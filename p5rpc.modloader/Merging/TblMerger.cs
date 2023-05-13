﻿using CriFs.V2.Hook.Interfaces;
using CriFsV2Lib.Definitions.Utilities;
using Persona.Merger.Cache;
using Persona.Merger.Patching.Tbl.Name;
using Persona.Merger.Patching.Tbl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static p5rpc.modloader.Merging.MergeUtils;
using FileEmulationFramework.Lib.Utilities;

namespace p5rpc.modloader.Merging
{
    internal class TblMerger : IFileMerger
    {
        private MergeUtils _utils;
        private Logger _logger;
        private MergedFileCache _mergedFileCache;
        private ICriFsRedirectorApi _criFsApi;
        private Game _game;

        internal TblMerger(MergeUtils utils, Logger logger, MergedFileCache mergedFileCache, ICriFsRedirectorApi criFsApi, Game game)
        {
            _utils = utils;
            _logger = logger;
            _mergedFileCache = mergedFileCache;
            _criFsApi = criFsApi;
            _game = game;
        }

        public void Merge(string[] cpks, ICriFsRedirectorApi.BindContext context)
        {
            if (_game != Game.P5R) return; // There's only tbl merging stuff for P5R
            // Note: Actual merging logic is optimised but code in mod could use some more work.
            var pathToFileMap = context.RelativePathToFileMap;
            var tasks = new List<ValueTask>
            {
                PatchTbl(pathToFileMap, @"R2\BATTLE\TABLE\SKILL.TBL", TblType.Skill, cpks),
                PatchTbl(pathToFileMap, @"R2\BATTLE\TABLE\ELSAI.TBL", TblType.Elsai, cpks),
                PatchTbl(pathToFileMap, @"R2\BATTLE\TABLE\ITEM.TBL", TblType.Item, cpks),
                PatchTbl(pathToFileMap, @"R2\BATTLE\TABLE\EXIST.TBL", TblType.Exist, cpks),
                PatchTbl(pathToFileMap, @"R2\BATTLE\TABLE\PLAYER.TBL", TblType.Player, cpks),
                PatchTbl(pathToFileMap, @"R2\BATTLE\TABLE\ENCOUNT.TBL", TblType.Encount, cpks),
                PatchTbl(pathToFileMap, @"R2\BATTLE\TABLE\PERSONA.TBL", TblType.Persona, cpks),
                PatchTbl(pathToFileMap, @"R2\BATTLE\TABLE\AICALC.TBL", TblType.AiCalc, cpks),
                PatchTbl(pathToFileMap, @"R2\BATTLE\TABLE\VISUAL.TBL", TblType.Visual, cpks),
                PatchTbl(pathToFileMap, @"R2\BATTLE\TABLE\NAME.TBL", TblType.Name, cpks),
                PatchTbl(pathToFileMap, @"R2\BATTLE\TABLE\UNIT.TBL", TblType.Unit, cpks)
            };

            Task.WhenAll(tasks.Select(x => x.AsTask())).Wait();
        }

        private async ValueTask PatchTbl(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> pathToFileMap, string tblPath, TblType type, string[] cpks)
        {
            if (!pathToFileMap.TryGetValue(tblPath, out var candidates))
                return;

            var pathInCpk = RemoveR2Prefix(tblPath);
            if (!_utils.TryFindFileInAnyCpk(pathInCpk, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
            {
                _logger.Warning("Unable to find TBL in any CPK {0}", pathInCpk);
                return;
            }

            // Build cache key
            var cacheKey = GetCacheKeyAndSources(tblPath, candidates, out var sources);
            if (_mergedFileCache.TryGet(cacheKey, sources, out var cachedFilePath))
            {
                _logger.Info("Loading Merged TBL {0} from Cache ({1})", tblPath, cachedFilePath);
                _utils.ReplaceFileInBinderInput(pathToFileMap, tblPath, cachedFilePath);
                return;
            }

            // Else Merge our Data
            // First we extract.
            await Task.Run(async () =>
            {
                _logger.Info("Merging {0} with key {1}.", tblPath, cacheKey);
                await using var cpkStream = new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                using var reader = _criFsApi.GetCriFsLib().CreateCpkReader(cpkStream, false);
                using var extractedTable = reader.ExtractFile(cpkEntry.Files[fileIndex].File);

                // Then we merge
                byte[] patched;
                if (type != TblType.Name)
                    patched = await PatchTable(type, extractedTable, candidates);
                else
                    patched = await PatchNameTable(extractedTable, candidates);

                // Then we store in cache.
                var item = await _mergedFileCache.AddAsync(cacheKey, sources, patched);
                _utils.ReplaceFileInBinderInput(pathToFileMap, tblPath, Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath));
                _logger.Info("Merge {0} Complete. Cached to {1}.", tblPath, item.RelativePath);
            });
        }

        private async Task<byte[]> PatchNameTable(ArrayRental extractedTable, List<ICriFsRedirectorApi.BindFileInfo> candidates)
        {
            var table = ParsedNameTable.ParseTable(extractedTable.RawArray);
            var otherTables = new ParsedNameTable[candidates.Count];
            for (int x = 0; x < otherTables.Length; x++)
                otherTables[x] = ParsedNameTable.ParseTable(await File.ReadAllBytesAsync(candidates[x].FullPath));

            var diff = NameTableMerger.CreateDiffs(table, otherTables);
            return NameTableMerger.Merge(table, diff).ToArray();
        }

        private static async Task<byte[]> PatchTable(TblType type, ArrayRental extractedTable, List<ICriFsRedirectorApi.BindFileInfo> candidates)
        {
            var patcher = new TblPatcher(extractedTable.Span.ToArray(), type);
            var patches = new List<TblPatch>(candidates.Count);
            for (var x = 0; x < candidates.Count; x++)
                patches.Add(patcher.GeneratePatch(await File.ReadAllBytesAsync(candidates[x].FullPath)));

            var patched = patcher.Apply(patches);
            return patched;
        }
    }
}
