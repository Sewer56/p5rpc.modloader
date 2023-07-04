using CriFs.V2.Hook.Interfaces;
using FileEmulationFramework.Lib.Utilities;
using Persona.Merger.Cache;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G;
using Persona.Merger.Patching.Tbl;
using PAK.Stream.Emulator.Interfaces;
using PAK.Stream.Emulator.Interfaces.Structures.IO;
using System.Diagnostics;

namespace p5rpc.modloader.Merging.Tbl
{
    internal class P4GTblMerger : IFileMerger
    {
        private readonly MergeUtils _utils;
        private readonly Logger _logger;
        private readonly MergedFileCache _mergedFileCache;
        private readonly ICriFsRedirectorApi _criFsApi;
        private readonly IPakEmulator _pakEmulator;

        internal P4GTblMerger(MergeUtils utils, Logger logger, MergedFileCache mergedFileCache, ICriFsRedirectorApi criFsApi, IPakEmulator pakEmulator)
        {
            //Debugger.Launch();
            _utils = utils;
            _logger = logger;
            _mergedFileCache = mergedFileCache;
            _criFsApi = criFsApi;
            _pakEmulator = pakEmulator;
        }

        public void Merge(string[] cpks, ICriFsRedirectorApi.BindContext context)
        {
            var pakFiles = _pakEmulator.GetEmulatorInput();
            var tasks = new List<ValueTask>
            {
                PatchTbl(pakFiles, @"init_free.bin\battle\AICALC.TBL", TblType.AiCalc, cpks),
                PatchTbl(pakFiles, @"init_free.bin\battle\EFFECT.TBL", TblType.Effect, cpks),
                PatchTbl(pakFiles, @"init_free.bin\battle\ENCOUNT.TBL", TblType.Encount, cpks),
                PatchTbl(pakFiles, @"init_free.bin\battle\MODEL.TBL", TblType.Model, cpks),
                PatchTbl(pakFiles, @"init_free.bin\battle\MSG.TBL", TblType.Message, cpks),
                PatchTbl(pakFiles, @"init_free.bin\battle\PERSONA.TBL", TblType.Persona, cpks),
                PatchTbl(pakFiles, @"init_free.bin\battle\SKILL.TBL", TblType.Skill, cpks),
                PatchTbl(pakFiles, @"init_free.bin\battle\UNIT.TBL", TblType.Unit, cpks),
                PatchTbl(pakFiles, @"init_free.bin\init\itemtbl.bin", TblType.Item, cpks),
            };

            Task.WhenAll(tasks.Select(x => x.AsTask())).Wait();
        }

        private async ValueTask PatchTbl(RouteGroupTuple[] pakFiles, string tblPath, TblType type, string[] cpks)
        {
            var route = tblPath.Substring(0, tblPath.LastIndexOf('\\'));
            var tblName = Path.GetFileName(tblPath);
            List<string> candidates = new();
            foreach (var group in pakFiles.Where(x => x.Route.Equals(route, StringComparison.OrdinalIgnoreCase)))
            {
                candidates.AddRange(group.Files.Files.Where(x => x.Equals(tblName, StringComparison.OrdinalIgnoreCase)).
                    Select(file => $@"{group.Files.Directory.FullPath}\{file}"));
            }

            if (candidates.Count == 0) return;

            var extIndex = tblPath.IndexOf('.');
            var dirIndex = tblPath.IndexOf('\\', extIndex);
            var pathInCpk = '\\' + tblPath.Substring(0, dirIndex);
            var pathInPak = tblPath.Substring(dirIndex + 1);

            if (!_utils.TryFindFileInAnyCpk(pathInCpk, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
            {
                _logger.Warning("Unable to find TBL in any CPK {0}", tblPath);
                return;
            }

            // Build cache key
            string[] modIds = { "p5rpc.modloader" };
            var cacheKey = MergedFileCache.CreateKey(tblPath, modIds);
            var sources = candidates.Select(x => new CachedFileSource { LastWrite = File.GetLastWriteTime(x) }).ToArray();

            if (_mergedFileCache.TryGet(cacheKey, sources, out var cachedFilePath))
            {
                _logger.Info("Loading Merged TBL {0} from Cache ({1})", tblPath, cachedFilePath);
                _pakEmulator.AddFile(cachedFilePath, route, pathInPak);
                return;
            }

            // Else Merge our Data
            // First we extract.
            await Task.Run(async () =>
            {
                _logger.Info("Merging {0} with key {1}.", tblPath, cacheKey);
                await using var cpkStream = new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                using var reader = _criFsApi.GetCriFsLib().CreateCpkReader(cpkStream, false);
                using var extractedPak = reader.ExtractFile(cpkEntry.Files[fileIndex].File);

                var extractedTbl = _pakEmulator.GetEntry(new MemoryStream(extractedPak.RawArray), pathInPak);
                if(extractedTbl == null)
                {
                    _logger.Error($"Unable to extract {pathInPak} from {pathInCpk}");
                    return;
                }

                // Then we merge
                byte[] patched = await PatchTable(type, extractedTbl.Value.ToArray(), candidates);

                // Then we store in cache.
                var item = await _mergedFileCache.AddAsync(cacheKey, sources, patched);
                _pakEmulator.AddFile(Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath), route, pathInPak);
                _logger.Info("Merge {0} Complete. Cached to {1}.", tblPath, item.RelativePath);
            });
        }

        private static async Task<byte[]> PatchTable(TblType type, byte[] extractedTable, List<string> candidates)
        {
            var patcher = new P4GTblPatcher(extractedTable, type);
            var patches = new List<TblPatch>(candidates.Count);
            for (var x = 0; x < candidates.Count; x++)
                patches.Add(patcher.GeneratePatch(await File.ReadAllBytesAsync(candidates[x])));

            var patched = patcher.Apply(patches, type);
            return patched;
        }
    }
}
