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
        List<string> doneRoutes = new();
        
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

            if (doneRoutes.Contains(route))
                continue;
            doneRoutes.Add(route);
            _logger.Info("Route: {0}", route);
            tasks.Add(CachePak(pathToFileMap, @"R2\" + route, cpks, context.BindDirectory));
        }
        
        Task.WhenAll(tasks.Select(x => x.AsTask())).Wait();
    }

    private async ValueTask CachePak(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> pathToFileMap, string route, string[] cpks, string bindDirectory)
    {
        string pathInCpk = RemoveR2Prefix(route);
        string cpkFinderPath = string.IsNullOrEmpty(Path.GetDirectoryName(pathInCpk)) ? "\\" + pathInCpk : pathInCpk;
        
        if (!_utils.TryFindFileInAnyCpk(cpkFinderPath, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
        {
            _logger.Warning("Unable to find PAK in any CPK {0}", pathInCpk);
            return;
        }
        
        // Then we store in cache.
        string[] modids = { "p5rpc.modloader" };
        var sources = new[] { new CachedFileSource() };
        var cacheKey = MergedFileCache.CreateKey(route, modids);

        if (!_mergedFileCache.TryGet(cacheKey, sources, out var cachedPath))
        {
            // Else Merge our Data
            // First we extract.
            await Task.Run(async () =>
            {
                await using var cpkStream = new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                using var reader = _criFsApi.GetCriFsLib().CreateCpkReader(cpkStream, false);
                using var extractedFile = reader.ExtractFile(cpkEntry.Files[fileIndex].File);
                var item = await _mergedFileCache.AddAsync(cacheKey, sources, extractedFile.RawArray.AsMemory(0, extractedFile.Count));
                cachedPath = Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath);
            });
        }
            
        string pakPath = Path.Combine(bindDirectory, route);
        string? dir = Path.GetDirectoryName(pakPath);
        if (dir != null)
            Directory.CreateDirectory(dir);
            
        if (!_pakEmulator.TryCreateFromFileSlice(cachedPath, 0, pathInCpk, pakPath))
        {
            _logger.Error($"Cannot Create File From Slice!");
            return;
        }

        _utils.ReplaceFileInBinderInput(pathToFileMap, route, pakPath);
        _logger.Info("File emulated at {0}.", pakPath);
    }
}