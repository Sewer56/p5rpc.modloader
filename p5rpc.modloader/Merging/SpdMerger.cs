using CriFs.V2.Hook.Interfaces;
using FileEmulationFramework.Lib.Utilities;
using Persona.Merger.Cache;
using SPD.File.Emulator.Interfaces;
using static p5rpc.modloader.Merging.MergeUtils;
using SPD.File.Emulator.Interfaces.Structures.IO;
using PAK.Stream.Emulator.Interfaces;
using System.Reflection.Metadata.Ecma335;
using FileEmulationFramework.Lib;
using BF.File.Emulator.Interfaces;
using Reloaded.Memory.Streams;

namespace p5rpc.modloader.Merging;

internal class SpdMerger : IFileMerger
{
    private readonly MergeUtils _utils;
    private readonly Logger _logger;
    private readonly MergedFileCache _mergedFileCache;
    private readonly ICriFsRedirectorApi _criFsApi;
    private readonly ISpdEmulator _spdEmulator;
    private readonly IPakEmulator _pakEmulator;

    internal SpdMerger(MergeUtils utils, Logger logger, MergedFileCache mergedFileCache, ICriFsRedirectorApi criFsApi, ISpdEmulator spdEmulator, IPakEmulator pakEmulator, Game game)
    {
        _utils = utils;
        _logger = logger;
        _mergedFileCache = mergedFileCache;
        _criFsApi = criFsApi;
        _spdEmulator = spdEmulator;
        _pakEmulator = pakEmulator;
    }

    public void Merge(string[] cpks, ICriFsRedirectorApi.BindContext context)
    {
        // Variable setup
        var input = _spdEmulator.GetEmulatorInput();
        var pakGroups = _pakEmulator.GetEmulatorInput();
        var pathToFileMap = context.RelativePathToFileMap;
        var tasks = new List<ValueTask>();
        Dictionary<string, List<string>> looseSpds = new();
        Dictionary<string, PakSpdRoutes> pakSpdRoutes = new();
        CachedFileSource[] cpkSources = cpks.Select(cpk => new CachedFileSource { LastWrite = File.GetLastWriteTime(cpk) }).ToArray();

        foreach (RouteGroupTuple group in input)
        {
            var route = group.Route;
            string routeDir = Path.GetDirectoryName(route) ?? "";

            if (routeDir.Contains('.')) // Check for spds in a pak
            {
                foreach (var pakGroup in pakGroups)
                {
                    var looseSpdRoutes = pakGroup.Files.Files.Where(x => $@"{pakGroup.Route}\{x}".Contains(route, StringComparison.OrdinalIgnoreCase));
                    foreach (var spdRoute in looseSpdRoutes)
                    {
                        var fullSpdRoute = $@"{pakGroup.Route}\{spdRoute}";
                        if (!pakSpdRoutes.ContainsKey(fullSpdRoute))
                            pakSpdRoutes[fullSpdRoute] = new PakSpdRoutes($@"{pakGroup.Files.Directory.FullPath}\{spdRoute}", new List<string>(group.Files.Files));
                        else
                        {
                            pakSpdRoutes[fullSpdRoute].spdRoutes.AddRange(group.Files.Files);
                            pakSpdRoutes[fullSpdRoute].pakRoutes.Add($@"{pakGroup.Files.Directory.FullPath}\{spdRoute}"); // Ensure highest priority bf is used
                        }
                    }

                }
            }
            else
            {
                if (!looseSpds.ContainsKey(route))
                    looseSpds[route] = new List<string>(group.Files.Files.Select(file => $@"{group.Files.Directory.FullPath}\{file}"));
                else
                    looseSpds[route].AddRange(group.Files.Files.Select(file => $@"{group.Files.Directory.FullPath}\{file}"));
            }
        }

        foreach (var routePair in looseSpds)
        {
            _logger.Info("Route: {0}", routePair.Key);
            tasks.Add(CacheSpd(pathToFileMap, @"R2\" + routePair.Key, cpks, cpkSources, routePair.Value, context.BindDirectory));
        }

        foreach(var routePair in pakSpdRoutes)
        {
            _logger.Info("Route: {0}", routePair.Key);
            tasks.Add(CachePakedSpd(pathToFileMap, routePair.Key, cpks, cpkSources, routePair.Value, context.BindDirectory));
        }

        Task.WhenAll(tasks.Select(x => x.AsTask())).Wait();
        _logger.Info($"Finished merging spd files");
    }

    private async ValueTask CacheSpd(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> pathToFileMap, string route, string[] cpks, CachedFileSource[] cpkSources, List<string> innerFiles, string bindDirectory)
    {
        string pathInCpk = RemoveR2Prefix(route);
        string cpkFinderPath = string.IsNullOrEmpty(Path.GetDirectoryName(pathInCpk)) ? "\\" + pathInCpk : pathInCpk;

        if (!_utils.TryFindFileInAnyCpk(cpkFinderPath, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
        {
            _logger.Warning("Unable to find SPD in any CPK {0}", pathInCpk);
            return;
        }

        // Try and get cached merged file
        string[] modIds = { "p5rpc.modloader" };
        var mergedKey = MergedFileCache.CreateKey(route, modIds);
        CachedFileSource[] innerSources = innerFiles.Select(file => new CachedFileSource { LastWrite = new FileInfo(file).LastWriteTime }).ToArray();
        if (_mergedFileCache.TryGet(mergedKey, innerSources, out var mergedCachePath))
        {
            _logger.Info("Loading Merged SPD {0} from Cache ({1})", route, mergedCachePath);
            _utils.ReplaceFileInBinderInput(pathToFileMap, route, mergedCachePath);
            return;
        }

        // Try and get cached original file
        var originalKey = MergedFileCache.CreateKey(pathInCpk, modIds);

        if (!_mergedFileCache.TryGet(originalKey, cpkSources, out var cachedPath))
        {
            // Else Merge our Data
            // First we extract.
            await Task.Run(async () =>
            {
                await using var cpkStream = new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                using var reader = _criFsApi.GetCriFsLib().CreateCpkReader(cpkStream, false);
                using var extractedFile = reader.ExtractFile(cpkEntry.Files[fileIndex].File);
                var item = await _mergedFileCache.AddAsync(originalKey, cpkSources, extractedFile.RawArray.AsMemory(0, extractedFile.Count));
                cachedPath = Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath);
            });
        }

        string spdPath = Path.Combine(bindDirectory, route);
        string? dir = Path.GetDirectoryName(spdPath);
        if (dir != null)
            Directory.CreateDirectory(dir);

        if (!_spdEmulator.TryCreateFromFileSlice(cachedPath!, 0, pathInCpk, spdPath))
        {
            _logger.Error($"Cannot Create File From Slice!");
            return;
        }

        // Cache merged
        var item = await _mergedFileCache.AddAsync(mergedKey, innerSources, File.ReadAllBytes(spdPath));
        _utils.ReplaceFileInBinderInput(pathToFileMap, route, spdPath);
        _logger.Info("Merge {0} Complete. Cached to {1}.", route, item.RelativePath);
    }

    private async ValueTask CachePakedSpd(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> pathToFileMap, string route, string[] cpks, CachedFileSource[] cpkSources, PakSpdRoutes innerFiles, string bindDirectory)
    {
        // Try and get cached merged bf
        string[] modIds = { "p5rpc.modloader" };
        var mergedKey = MergedFileCache.CreateKey(route, modIds);

        CachedFileSource[] spdSources = innerFiles.spdRoutes.Select(file => new CachedFileSource { LastWrite = File.GetLastWriteTime(file) }).ToArray();

        DateTime lastWrite = DateTime.MinValue;
        foreach (var source in spdSources)
            if (source.LastWrite > lastWrite) lastWrite = source.LastWrite;

        if (_mergedFileCache.TryGet(mergedKey, spdSources, out var mergedCachePath))
        {
            _logger.Info("Loading Merged BF {0} from Cache ({1})", route, mergedCachePath);
            foreach (var path in innerFiles.pakRoutes)
            {
                _spdEmulator.RegisterSpd(mergedCachePath, path);
                File.SetLastWriteTime(path, lastWrite);
            }
            return;
        }

        // Get pak file
        var extensionIndex = route.IndexOf(".", StringComparison.OrdinalIgnoreCase);
        var index = route.IndexOf(Path.DirectorySeparatorChar, extensionIndex);
        var pakPath = route.Substring(0, index);

        string cpkFinderPath = string.IsNullOrEmpty(Path.GetDirectoryName(pakPath)) ? "\\" + pakPath : pakPath;

        if (!_utils.TryFindFileInAnyCpk(cpkFinderPath, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
        {
            _logger.Warning("Unable to find PAK in any CPK {0}", pakPath);
            return;
        }

        // Then we store in cache.
        var originalKey = MergedFileCache.CreateKey($"OG/{route}", modIds);

        var spdPathInPak = route.Substring(index + 1);

        if (!_mergedFileCache.TryGet(originalKey, cpkSources, out var cachedPath))
        {
            // Extract spd from pak
            await Task.Run(async () =>
            {
                await using var cpkStream = new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                using var reader = _criFsApi.GetCriFsLib().CreateCpkReader(cpkStream, false);
                using var extractedFile = reader.ExtractFile(cpkEntry.Files[fileIndex].File);
                var spdFile = _pakEmulator.GetEntry(new MemoryStream(extractedFile.RawArray), spdPathInPak);
                if (spdFile == null)
                {
                    _logger.Error($"Failed to extract {spdPathInPak} from {pakPath}");
                    return;
                }
                _logger.Info($"Extracted {spdPathInPak} from {pakPath}");
                var item = await _mergedFileCache.AddAsync(originalKey, cpkSources, (ReadOnlyMemory<byte>)spdFile);
                cachedPath = Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath);
            });
        }

        if (cachedPath == null) return;

        var spdPath = innerFiles.pakRoutes[innerFiles.pakRoutes.Count - 1];
        string? dir = Path.GetDirectoryName(spdPath);
        if (dir != null)
            Directory.CreateDirectory(dir);

        if (!_spdEmulator.TryCreateFromSpd(cachedPath, route, spdPath))
        {
            _logger.Error($"Cannot Create File From {spdPath}!");
            return;
        }

        // Cache merged
        var item = await _mergedFileCache.AddAsync(mergedKey, spdSources, File.ReadAllBytes(spdPath));
        _logger.Info("Merge {0} Complete. Cached to {1}.", route, item.RelativePath);

        // Register all the bfs to the one emulated one (only the highest priority should ever actually be used though)
        for (int i = 0; i < innerFiles.pakRoutes.Count - 1; i++)
            _spdEmulator.RegisterSpd($"{_mergedFileCache.CacheFolder}\\{item.RelativePath}", innerFiles.pakRoutes[i]);

        // Reset last write
        foreach (var bf in innerFiles.pakRoutes)
            File.SetLastWriteTime(bf, lastWrite);
    }
}

internal struct PakSpdRoutes
{
    internal List<string> pakRoutes;
    internal List<string> spdRoutes;

    internal PakSpdRoutes(string pakRoute, List<string> spdRoutes)
    {
        pakRoutes = new List<string>() { pakRoute };
        this.spdRoutes = spdRoutes;
    }
}