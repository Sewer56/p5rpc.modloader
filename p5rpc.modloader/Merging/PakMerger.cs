using CriFs.V2.Hook.Interfaces;
using FileEmulationFramework.Lib.Utilities;
using PAK.Stream.Emulator.Interfaces;
using PAK.Stream.Emulator.Interfaces.Structures.IO;
using Persona.Merger.Cache;
using static p5rpc.modloader.Merging.MergeUtils;

namespace p5rpc.modloader.Merging;

internal class PakMerger : IFileMerger
{
    private readonly MergeUtils _utils;
    private readonly Logger _logger;
    private readonly MergedFileCache _mergedFileCache;
    private readonly ICriFsRedirectorApi _criFsApi;
    private readonly IPakEmulator _pakEmulator;

    internal PakMerger(MergeUtils utils, Logger logger, MergedFileCache mergedFileCache, ICriFsRedirectorApi criFsApi, IPakEmulator pakEmulator)
    {
        _utils = utils;
        _logger = logger;
        _mergedFileCache = mergedFileCache;
        _criFsApi = criFsApi;
        _pakEmulator = pakEmulator;
    }

    public void Merge(string[] cpks, ICriFsRedirectorApi.BindContext context)
    {
        var input = _pakEmulator.GetEmulatorInput();
        var pathToFileMap = context.RelativePathToFileMap;
        var tasks = new List<ValueTask>();
        Dictionary<string, List<string>> doneRoutes = new();
        CachedFileSource[] cpkSources = cpks.Select(cpk => new CachedFileSource { LastWrite = new FileInfo(cpk).LastWriteTime }).ToArray();

        foreach (RouteGroupTuple group in input)
        {
            var route = group.Route;
            string routeDir = Path.GetDirectoryName(route) ?? "";
            if (routeDir.Contains('.'))
            {
                var extensionIndex = route.IndexOf(".", StringComparison.OrdinalIgnoreCase);
                var index = route.IndexOf(Path.DirectorySeparatorChar, extensionIndex);
                route = route.Substring(0, index); // extract route CPK name
            }

            if (!doneRoutes.ContainsKey(route))
                doneRoutes[route] = new List<string>(group.Files.Files.Select(file => $@"{group.Files.Directory.FullPath}\{file}"));
            else
                doneRoutes[route].AddRange(group.Files.Files.Select(file => $@"{group.Files.Directory.FullPath}\{file}"));
        }

        foreach (var routePair in doneRoutes)
        {
            _logger.Info("Route: {0}", routePair.Key);
            tasks.Add(CachePak(pathToFileMap, @"R2\" + routePair.Key, cpks, cpkSources, routePair.Value, context.BindDirectory));
        }

        Task.WhenAll(tasks.Select(x => x.AsTask())).Wait();
    }

    private async ValueTask CachePak(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> pathToFileMap, string route, string[] cpks, CachedFileSource[] cpkSources, List<string> innerFiles, string bindDirectory)
    {
        string pathInCpk = RemoveR2Prefix(route);
        string cpkFinderPath = string.IsNullOrEmpty(Path.GetDirectoryName(pathInCpk)) ? "\\" + pathInCpk : pathInCpk;

        if (!_utils.TryFindFileInAnyCpk(cpkFinderPath, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
        {
            _logger.Warning("Unable to find PAK in any CPK {0}", pathInCpk);
            return;
        }

        // Try and get cached merged file
        string[] modIds = { "p5rpc.modloader" };
        var mergedKey = MergedFileCache.CreateKey(route, modIds);
        CachedFileSource[] innerSources = innerFiles.Select(file => new CachedFileSource { LastWrite = new FileInfo(file).LastWriteTime }).ToArray();
        if(_mergedFileCache.TryGet(mergedKey, innerSources, out var mergedCachePath))
        {
            _logger.Info("Loading Merged PAK {0} from Cache ({1})", route, mergedCachePath);
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

        string pakPath = Path.Combine(bindDirectory, route);
        string? dir = Path.GetDirectoryName(pakPath);
        if (dir != null)
            Directory.CreateDirectory(dir);

        if (!_pakEmulator.TryCreateFromFileSlice(cachedPath!, 0, pathInCpk, pakPath))
        {
            _logger.Error($"Cannot Create File From Slice!");
            return;
        }

        // Cache merged
        var item = await _mergedFileCache.AddAsync(mergedKey, innerSources, File.ReadAllBytes(pakPath));
        _utils.ReplaceFileInBinderInput(pathToFileMap, route, pakPath);
        _logger.Info("Merge {0} Complete. Cached to {1}.", route, item.RelativePath);
    }
}