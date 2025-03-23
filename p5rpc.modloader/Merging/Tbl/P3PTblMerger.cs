using CriFs.V2.Hook.Interfaces;
using CriFsV2Lib.Definitions.Utilities;
using FileEmulationFramework.Lib.Utilities;
using PAK.Stream.Emulator.Interfaces;
using PAK.Stream.Emulator.Interfaces.Structures.IO;
using Persona.Merger.Cache;
using Persona.Merger.Patching.Tbl;
using Persona.Merger.Patching.Tbl.FieldResolvers.Generic;
using Persona.Merger.Patching.Tbl.FieldResolvers.P3P;
using Reloaded.Universal.Localisation.Framework.Interfaces;
using static p5rpc.modloader.Merging.Tbl.TblMerger;

namespace p5rpc.modloader.Merging.Tbl;

internal class P3PTblMerger : IFileMerger
{
    private readonly ICriFsRedirectorApi _criFsApi;
    private readonly Logger _logger;
    private readonly MergedFileCache _mergedFileCache;
    private readonly IPakEmulator _pakEmulator;
    private readonly MergeUtils _utils;
    private readonly ILocalisationFramework _localisationFramework;

    internal P3PTblMerger(MergeUtils utils, Logger logger, MergedFileCache mergedFileCache,
        ICriFsRedirectorApi criFsApi, IPakEmulator pakEmulator, ILocalisationFramework localisationFramework)
    {
        _utils = utils;
        _logger = logger;
        _mergedFileCache = mergedFileCache;
        _criFsApi = criFsApi;
        _pakEmulator = pakEmulator;
        _localisationFramework = localisationFramework;
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
            PatchAnyFileInPak(pakFiles, @"data\facility\combine.bin\ps_model.bin", 4, cpks),
            PatchAnyFileInPak(pakFiles, @"data\facility\elvgirl.bin\ps_model.bin", 4, cpks),
            PatchAnyFileInPak(pakFiles, @"data\facility\elvgirl_m.bin\ps_model.bin", 4, cpks)
        };

        Task.WhenAll(tasks.Select(x => x.AsTask())).Wait();
    }

    private async ValueTask PatchTbl(RouteGroupTuple[] pakFiles, string tblPath, TblType type, string[] cpks)
    {
        var route = tblPath.Substring(0, tblPath.LastIndexOf('\\'));
        var tblName = Path.GetFileName(tblPath);
        var candidates = FindInPaks(pakFiles, route, tblName);
        if (type is TblType.Message)
        {
            candidates.AddRange(FindInPaks(pakFiles, @"init_free.bin\battle", "MSGTBL.bmd"));
        }
        else if (type is TblType.AiCalc)
        {
            candidates.AddRange(FindInPaks(pakFiles, @"init_free.bin\battle", "enemy.bf"));
            candidates.AddRange(FindInPaks(pakFiles, @"init_free.bin\battle", "friend.bf"));
        }

        if (candidates.Count == 0) return;

        var extIndex = tblPath.IndexOf('.');
        var dirIndex = tblPath.IndexOf('\\', extIndex);
        var pathInCpk = '\\' + tblPath.Substring(0, dirIndex);
        var pathInPak = tblPath.Substring(dirIndex + 1);

        if (!_utils.TryFindFileInAnyCpk(pathInCpk, cpks, out var cpkPath, out var cpkEntry, out var fileIndex))
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
            await using var cpkStream =
                new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var reader = _criFsApi.GetCriFsLib().CreateCpkReader(cpkStream, false);
            using var extractedPak = reader.ExtractFile(cpkEntry.Files[fileIndex].File);

            var extractedTbl = _pakEmulator.GetEntry(new MemoryStream(extractedPak.RawArray), pathInPak);
            if (extractedTbl == null)
            {
                _logger.Error($"Unable to extract {pathInPak} from {pathInCpk}");
                return;
            }

            // Then we merge
            // Note: https://github.com/Sewer56/p5rpc.modloader/pull/41#issue-2939748981
            // Async reads don't work here.
            byte[] patched;
            switch (type)
            {
                case TblType.Message:
                    patched = PatchMsgTable(extractedTbl.Value.ToArray(), candidates);
                    break;
                case TblType.AiCalc:
                    patched = PatchAiCalc(extractedTbl.Value.ToArray(), candidates);
                    break;
                default:
                    patched = PatchTable(type, extractedTbl.Value.ToArray(), candidates);
                    break;
            }

            // Then we store in cache.
            var item = await _mergedFileCache.AddAsync(cacheKey, sources, patched);
            _pakEmulator.AddFile(Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath), route, pathInPak);
            _logger.Info("Merge {0} Complete. Cached to {1}.", tblPath, item.RelativePath);
        });
    }

    private async ValueTask PatchAnyFileInPak(RouteGroupTuple[] pakFiles, string tblPath, int ResolverSize, string[] cpks)
    {
        var route = tblPath.Substring(0, tblPath.LastIndexOf('\\'));
        var tblName = Path.GetFileName(tblPath);
        var candidates = FindInPaks(pakFiles, route, tblName);

        if (candidates.Count == 0) return;

        var extIndex = tblPath.IndexOf('.');
        var dirIndex = tblPath.IndexOf('\\', extIndex);
        var pathInCpk = '\\' + tblPath.Substring(0, dirIndex);
        var pathInPak = tblPath.Substring(dirIndex + 1);

        if (!_utils.TryFindFileInAnyCpk(pathInCpk, cpks, out var cpkPath, out var cpkEntry, out var fileIndex))
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
            await using var cpkStream =
                new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var reader = _criFsApi.GetCriFsLib().CreateCpkReader(cpkStream, false);
            using var extractedPak = reader.ExtractFile(cpkEntry.Files[fileIndex].File);

            var extractedTbl = _pakEmulator.GetEntry(new MemoryStream(extractedPak.RawArray), pathInPak);
            if (extractedTbl == null)
            {
                _logger.Error($"Unable to extract {pathInPak} from {pathInCpk}");
                return;
            }

            // Then we merge
            var patched = await PatchAny(extractedTbl.Value.ToArray(), candidates, ResolverSize);

            // Then we store in cache.
            var item = await _mergedFileCache.AddAsync(cacheKey, sources, patched);
            _pakEmulator.AddFile(Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath), route, pathInPak);
            _logger.Info("Merge {0} Complete. Cached to {1}.", tblPath, item.RelativePath);
        });
    }

    private static byte[] PatchTable(TblType type, byte[] extractedTable, List<string> candidates)
    {
        var patcher = new P3PTblPatcher(extractedTable, type);
        var patches = new List<TblPatch>(candidates.Count);
        for (var x = 0; x < candidates.Count; x++)
            patches.Add(patcher.GeneratePatch(File.ReadAllBytes(candidates[x])));

        return patcher.Apply(patches, type);
    }

    private byte[] PatchMsgTable(byte[] extractedTable, List<string> candidates)
    {
        // Msg tbls of different languages cannot be merged, ensure only those of the same language are used
        if (_localisationFramework.TryGetLanguage(out var language) && language != Language.English)
        {
            candidates = candidates.Where(x => _localisationFramework.IsFileLocalised(x)).ToList();
        }

        var bmds = new byte[5][];
        var bmdFiles = candidates.Where(x => x.EndsWith("MSGTBL.bmd", StringComparison.OrdinalIgnoreCase)).ToArray();
        foreach (var bmdFile in bmdFiles)
        {
            _logger.Info($"Embedding {bmdFile} into MSG.TBL");
            bmds[4] = File.ReadAllBytes(bmdFile);
            candidates.Remove(bmdFile);
        }

        var patcher = new P3PTblPatcher(extractedTable, TblType.Message);
        var patches = new List<TblPatch>(candidates.Count);
        for (var x = 0; x < candidates.Count; x++)
            patches.Add(patcher.GeneratePatch(File.ReadAllBytes(candidates[x]))); // TODO find out why ReadAllBytesAsync doesn't work

        return patcher.Apply(patches, TblType.Message, bmds);
    }

    private byte[] PatchAiCalc(byte[] extractedTable, List<string> candidates)
    {
        var bfs = new byte[18][];
        // ToArray so we can remove items from the collection in the foreach
        var bfFiles = candidates.Where(x =>
            x.EndsWith("enemy.bf", StringComparison.OrdinalIgnoreCase) ||
            x.EndsWith("friend.bf", StringComparison.OrdinalIgnoreCase)).ToArray();
        foreach (var bfFile in bfFiles)
        {
            _logger.Info($"Embedding {bfFile} into AICALC.TBL");
            var index = bfFile.EndsWith("friend.bf", StringComparison.OrdinalIgnoreCase) ? 16 : 17;
            bfs[index] = File.ReadAllBytes(bfFile);
            candidates.Remove(bfFile);
        }

        var patcher = new P3PTblPatcher(extractedTable, TblType.AiCalc);
        var patches = new List<TblPatch>(candidates.Count);
        for (var x = 0; x < candidates.Count; x++)
            patches.Add(patcher.GeneratePatch(File.ReadAllBytes(candidates[x])));

        return patcher.Apply(patches, TblType.AiCalc, bfs);
    }

    private static async Task<byte[]> PatchAny(byte[] extractedTable,
        List<string> candidates, int ResolverSize)
    {
        var patcher = new GenericPatcher(extractedTable);
        var patches = new List<TblPatch>(candidates.Count);
        for (var x = 0; x < candidates.Count; x++)
            patches.Add(patcher.GeneratePatchGeneric(await File.ReadAllBytesAsync(candidates[x]), ResolverSize));

        return patcher.ApplyGeneric(patches);
    }
}