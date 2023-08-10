using CriFs.V2.Hook.Interfaces;
using FileEmulationFramework.Lib.Utilities;
using Persona.Merger.Cache;
using BF.File.Emulator.Interfaces;
using static p5rpc.modloader.Merging.MergeUtils;
using BF.File.Emulator.Interfaces.Structures.IO;
using PAK.Stream.Emulator.Interfaces;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G;
using Persona.Merger.Patching.Tbl.FieldResolvers.P3P;

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
        private readonly Game _game;

        internal BfMerger(MergeUtils utils, Logger logger, MergedFileCache mergedFileCache, ICriFsRedirectorApi criFsApi, IBfEmulator bfEmulator, IPakEmulator pakEmulator, Game game)
        {
            _utils = utils;
            _logger = logger;
            _mergedFileCache = mergedFileCache;
            _criFsApi = criFsApi;
            _bfEmulator = bfEmulator;
            _pakEmulator = pakEmulator;
            _game = game;
        }

        public void Merge(string[] cpks, ICriFsRedirectorApi.BindContext context)
        {
            var input = _bfEmulator.GetEmulatorInput();
            var pathToFileMap = context.RelativePathToFileMap;
            var pakGroups = _pakEmulator.GetEmulatorInput();
            var tasks = new List<ValueTask>();
            Dictionary<string, List<string>> looseBfs = new();
            Dictionary<string, BfFlowTuple> pakedBfs = new();
            CachedFileSource[] cpkSources = cpks.Select(cpk => new CachedFileSource { LastWrite = File.GetLastWriteTime(cpk) }).ToArray();

            foreach (RouteFileTuple group in input)
            {
                var route = Path.ChangeExtension(group.Route, ".bf");

                // Loose bfs
                var bfRoutes = pathToFileMap.Keys.Where(x => x.Contains(route, StringComparison.OrdinalIgnoreCase));
                foreach (var bfRoute in bfRoutes)
                {
                    if (!looseBfs.ContainsKey(bfRoute))
                        looseBfs[bfRoute] = new List<string> { group.File };
                    else
                        looseBfs[bfRoute].Add(group.File);
                }

                // bfs in pak files
                foreach (var pakGroup in pakGroups)
                {
                    var looseBfRoutes = pakGroup.Files.Files.Where(x => $@"{pakGroup.Route}\{x}".Contains(route, StringComparison.OrdinalIgnoreCase));
                    foreach (var bfRoute in looseBfRoutes)
                    {
                        var fullBfRoute = $@"{pakGroup.Route}\{bfRoute}";
                        if (!pakedBfs.ContainsKey(fullBfRoute))
                            pakedBfs[fullBfRoute] = new BfFlowTuple($@"{pakGroup.Files.Directory.FullPath}\{bfRoute}", new List<string> { group.File });
                        else
                        {
                            pakedBfs[fullBfRoute].FlowPaths.Add(group.File);
                            pakedBfs[fullBfRoute].BfPaths.Add($@"{pakGroup.Files.Directory.FullPath}\{bfRoute}"); // Ensure highest priority bf is used
                        }
                    }

                }
                _logger.Info("Route: {0}", route);
            }

            foreach (var routePair in looseBfs)
                tasks.Add(CacheBf(pathToFileMap, routePair.Key, cpks, routePair.Value, context.BindDirectory));

            foreach (var routePair in pakedBfs)
                tasks.Add(CachePakedBf(routePair.Value.FlowPaths, routePair.Value.BfPaths, routePair.Key, cpks, cpkSources));

            Task.WhenAll(tasks.Select(x => x.AsTask())).Wait();
            _logger.Info($"Finished merging bf files");
        }

        private async ValueTask CachePakedBf(List<string> flowPaths, List<string> bfPaths, string route, string[] cpks, CachedFileSource[] cpkSources)
        {
            // Try and get cached merged bf
            string[] modIds = { "p5rpc.modloader" };
            var mergedKey = MergedFileCache.CreateKey(route, modIds);
            CachedFileSource[] flowSources = flowPaths.Select(file => new CachedFileSource { LastWrite = File.GetLastWriteTime(file) }).ToArray();

            DateTime lastWrite = DateTime.MinValue;
            foreach (var source in flowSources)
                if (source.LastWrite > lastWrite) lastWrite = source.LastWrite;

            if (_mergedFileCache.TryGet(mergedKey, flowSources, out var mergedCachePath))
            {
                _logger.Info("Loading Merged BF {0} from Cache ({1})", route, mergedCachePath);
                foreach (var path in bfPaths)
                {
                    _bfEmulator.RegisterBf(mergedCachePath, path);
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

            var bfPathInPak = route.Substring(index + 1);

            if (!_mergedFileCache.TryGet(originalKey, cpkSources, out var cachedPath))
            {
                // Extract bf from pak
                await Task.Run(async () =>
                {
                    await using var cpkStream = new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    using var reader = _criFsApi.GetCriFsLib().CreateCpkReader(cpkStream, false);
                    using var extractedFile = reader.ExtractFile(cpkEntry.Files[fileIndex].File);
                    var bfFile = ExtractBf(extractedFile.RawArray, bfPathInPak);
                    if (bfFile == null)
                    {
                        _logger.Error($"Failed to extract {bfPathInPak} from {pakPath}");
                        return;
                    }
                    _logger.Info($"Extracted {bfPathInPak} from {pakPath}");
                    var item = await _mergedFileCache.AddAsync(originalKey, cpkSources, (ReadOnlyMemory<byte>)bfFile);
                    cachedPath = Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath);
                });
            }

            if (cachedPath == null) return;

            var bfPath = bfPaths[bfPaths.Count - 1];
            string? dir = Path.GetDirectoryName(bfPath);
            if (dir != null)
                Directory.CreateDirectory(dir);

            if (!_bfEmulator.TryCreateFromBf(cachedPath, route, bfPath))
            {
                _logger.Error($"Cannot Create File From {bfPath}!");
                return;
            }

            // Cache merged
            var item = await _mergedFileCache.AddAsync(mergedKey, flowSources, File.ReadAllBytes(bfPath));
            _logger.Info("Merge {0} Complete. Cached to {1}.", route, item.RelativePath);

            // Register all the bfs to the one emulated one (only the highest priority should ever actually be used though)
            for (int i = 0; i < bfPaths.Count - 1; i++)
                _bfEmulator.RegisterBf($"{_mergedFileCache.CacheFolder}\\{item.RelativePath}", bfPaths[i]);

            // Reset last write
            foreach (var bf in bfPaths)
                File.SetLastWriteTime(bf, lastWrite);
        }

        private ReadOnlyMemory<byte>? ExtractBf(byte[] pak, string bfPathInPak)
        {
            int index = 0;
            if (bfPathInPak.Equals(@"battle\friend.bf", StringComparison.OrdinalIgnoreCase))
                index = _game == Game.P4G ? 9 : 16;
            else if (bfPathInPak.Equals(@"battle\enemy.bf", StringComparison.OrdinalIgnoreCase))
                index = _game == Game.P4G ? 10 : 17;

            if(index == 0)    
                return _pakEmulator.GetEntry(new MemoryStream(pak), bfPathInPak);

            var aiCalc = _pakEmulator.GetEntry(new MemoryStream(pak), "battle/AICALC.TBL");
            if (aiCalc == null)
                return null;

            if(_game == Game.P4G)
                return P4GTblPatcher.GetSegment(aiCalc.Value.ToArray(), Persona.Merger.Patching.Tbl.FieldResolvers.P4G.TblType.AiCalc, index);
            else
                return P3PTblPatcher.GetSegment(aiCalc.Value.ToArray(), Persona.Merger.Patching.Tbl.FieldResolvers.P3P.TblType.AiCalc, index);
        }

        private async ValueTask CacheBf(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> pathToFileMap, string route, string[] cpks, List<string> flowPaths, string bindDirectory)
        {
            // Try and get cached merged bf
            string bfPath = Path.Combine(bindDirectory, route);
            string[] modIds = { "p5rpc.modloader" };
            var mergedKey = MergedFileCache.CreateKey(route, modIds);
            CachedFileSource[] flowSources = flowPaths.Select(file => new CachedFileSource { LastWrite = File.GetLastWriteTime(file) }).ToArray();
            if (_mergedFileCache.TryGet(mergedKey, flowSources, out var mergedCachePath))
            {
                _logger.Info("Loading Merged BF {0} from Cache ({1})", route, mergedCachePath);
                _bfEmulator.RegisterBf(mergedCachePath, bfPath);
                return;
            }

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

            string? dir = Path.GetDirectoryName(bfPath);
            if (dir != null)
                Directory.CreateDirectory(dir);

            if (!_bfEmulator.TryCreateFromBf(cachedPath!, pathInCpk, bfPath))
            {
                _logger.Error($"Cannot Create File From {bfPath}!");
                return;
            }

            // Cache merged
            var item = await _mergedFileCache.AddAsync(mergedKey, flowSources, File.ReadAllBytes(bfPath));
            _utils.ReplaceFileInBinderInput(pathToFileMap, route, bfPath);
            _logger.Info("Merge {0} Complete. Cached to {1}.", route, item.RelativePath);

            // Reset last write
            DateTime lastWrite = DateTime.MinValue;
            foreach (var source in flowSources)
                if (source.LastWrite > lastWrite) lastWrite = source.LastWrite;
            File.SetLastWriteTime(bfPath, lastWrite);
        }
    }

    internal class BfFlowTuple
    {
        internal List<string> BfPaths { get; set; }
        internal List<string> FlowPaths { get; set; }

        internal BfFlowTuple(string bfPath, List<string> flowPaths)
        {
            BfPaths = new List<string> { bfPath };
            FlowPaths = flowPaths;
        }
    }
}
