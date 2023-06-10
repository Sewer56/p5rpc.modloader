using CriFs.V2.Hook.Interfaces;
using FileEmulationFramework.Lib.Utilities;
using PAK.Stream.Emulator.Interfaces.Structures.IO;
using Persona.Merger.Cache;
using BF.File.Emulator.Interfaces;
using static p5rpc.modloader.Merging.MergeUtils;
using BF.File.Emulator.Interfaces.Structures.IO;
using PAK.Stream.Emulator.Interfaces;
using System.Diagnostics;

namespace p5rpc.modloader.Merging
{
    internal class BfMerger : IFileMerger
    {
        private readonly MergeUtils _utils;
        private readonly Logger _logger;
        private readonly MergedFileCache _mergedFileCache;
        private readonly ICriFsRedirectorApi _criFsApi;
        private readonly IBfEmulator _bfEmulator;
        private readonly IPakEmulator _pakEmulator;

        internal BfMerger(MergeUtils utils, Logger logger, MergedFileCache mergedFileCache, ICriFsRedirectorApi criFsApi, IBfEmulator bfEmulator, IPakEmulator pakEmulator)
        {
            _utils = utils;
            _logger = logger;
            _mergedFileCache = mergedFileCache;
            _criFsApi = criFsApi;
            _bfEmulator = bfEmulator;
            _pakEmulator = pakEmulator;
        }

        public void Merge(string[] cpks, ICriFsRedirectorApi.BindContext context)
        {
            var input = _bfEmulator.GetEmulatorInput();
            var pathToFileMap = context.RelativePathToFileMap;
            var pakGroups = _pakEmulator.GetEmulatorInput();
            var tasks = new List<ValueTask>();
            HashSet<string> doneRoutes = new();

            foreach (RouteFileTuple group in input)
            {
                var route = Path.ChangeExtension(group.Route, ".bf");

                if (doneRoutes.Contains(route))
                    continue;

                // Loose bfs
                var bfRoutes = pathToFileMap.Keys.Where(x => x.Contains(route, StringComparison.OrdinalIgnoreCase));
                foreach (var bfRoute in bfRoutes)
                    tasks.Add(CacheBf(pathToFileMap, bfRoute, cpks, context.BindDirectory));

                // bfs in pak files
                foreach (var pakGroup in pakGroups)
                {
                    bfRoutes = pakGroup.Files.Files.Where(x => $@"{pakGroup.Route}\{x}".Contains(route, StringComparison.OrdinalIgnoreCase));
                    foreach (var bfRoute in bfRoutes)
                        tasks.Add(CachePakedBf(pathToFileMap, $@"{pakGroup.Files.Directory.FullPath}\{bfRoute}", $@"{pakGroup.Route}\{bfRoute}", cpks, context.BindDirectory));

                }

                doneRoutes.Add(route);
                _logger.Info("Route: {0}", route);

            }

            Task.WhenAll(tasks.Select(x => x.AsTask())).Wait();
            _logger.Info($"Finished merging bf files");
        }

        private async ValueTask CachePakedBf(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> pathToFileMap, string bfPath, string bfRoute, string[] cpks, string bindDirectory)
        {
            // Get pak file
            var extensionIndex = bfRoute.IndexOf(".", StringComparison.OrdinalIgnoreCase);
            var index = bfRoute.IndexOf(Path.DirectorySeparatorChar, extensionIndex);
            var pakPath = bfRoute.Substring(0, index);

            string cpkFinderPath = string.IsNullOrEmpty(Path.GetDirectoryName(pakPath)) ? "\\" + pakPath : pakPath;

            if (!_utils.TryFindFileInAnyCpk(cpkFinderPath, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
            {
                _logger.Warning("Unable to find PAK in any CPK {0}", pakPath);
                return;
            }

            // Then we store in cache.
            string[] modids = { "p5rpc.modloader" };
            var sources = new[] { new CachedFileSource() };
            var cacheKey = MergedFileCache.CreateKey(bfRoute, modids);

            var bfPathInPak = bfRoute.Substring(index + 1);

            if (!_mergedFileCache.TryGet(cacheKey, sources, out var cachedPath))
            {
                // Extract bf from pak
                await Task.Run(async () =>
                {
                    await using var cpkStream = new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    using var reader = _criFsApi.GetCriFsLib().CreateCpkReader(cpkStream, false);
                    using var extractedFile = reader.ExtractFile(cpkEntry.Files[fileIndex].File);
                    var bfFile = _pakEmulator.GetEntry(new MemoryStream(extractedFile.RawArray), bfPathInPak);
                    if (bfFile == null)
                    {
                        _logger.Error($"Failed to extract {bfPathInPak} from {pakPath}");
                        return;
                    }
                    _logger.Info($"Extracted {bfPathInPak} from {pakPath}");
                    var item = await _mergedFileCache.AddAsync(cacheKey, sources, (ReadOnlyMemory<byte>)bfFile);
                    cachedPath = Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath);
                });
            }

            if (cachedPath == null) return;

            //string bfPath = Path.Combine(bindDirectory, bfRoute);
            string? dir = Path.GetDirectoryName(bfPath);
            if (dir != null)
                Directory.CreateDirectory(dir);

            if (!_bfEmulator.TryCreateFromBf(cachedPath, bfRoute, bfPath))
            {
                _logger.Error($"Cannot Create File From {bfPath}!");
                return;
            }

            //_utils.ReplaceFileInBinderInput(pathToFileMap, bfRoute, bfPath);
            _logger.Info("File emulated at {0}.", bfPath);
        }

        private async ValueTask CacheBf(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> pathToFileMap, string route, string[] cpks, string bindDirectory)
        {
            string pathInCpk = RemoveR2Prefix(route);
            string cpkFinderPath = string.IsNullOrEmpty(Path.GetDirectoryName(pathInCpk)) ? "\\" + pathInCpk : pathInCpk;

            if (!_utils.TryFindFileInAnyCpk(cpkFinderPath, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
            {
                _logger.Warning("Unable to find BF in any CPK {0}", pathInCpk);
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

            string bfPath = Path.Combine(bindDirectory, route);
            string? dir = Path.GetDirectoryName(bfPath);
            if (dir != null)
                Directory.CreateDirectory(dir);

            if (!_bfEmulator.TryCreateFromBf(cachedPath, pathInCpk, bfPath))
            {
                _logger.Error($"Cannot Create File From {bfPath}!");
                return;
            }

            _utils.ReplaceFileInBinderInput(pathToFileMap, route, bfPath);
            _logger.Info("File emulated at {0}.", bfPath);
        }
    }
}
