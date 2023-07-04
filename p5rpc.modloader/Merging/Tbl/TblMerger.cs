using CriFs.V2.Hook.Interfaces;
using CriFsV2Lib.Definitions.Utilities;
using Persona.Merger.Cache;
using Persona.Merger.Patching.Tbl;
using static p5rpc.modloader.Merging.MergeUtils;
using FileEmulationFramework.Lib.Utilities;
using Persona.Merger.Patching.Tbl.FieldResolvers.P5R;
using Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Name;
using PAK.Stream.Emulator.Interfaces;

namespace p5rpc.modloader.Merging.Tbl;

internal class TblMerger : IFileMerger
{
    private IFileMerger _tblMerger;

    internal TblMerger(MergeUtils utils, Logger logger, MergedFileCache mergedFileCache, ICriFsRedirectorApi criFsApi, IPakEmulator pakEmulator, Game game)
    {
        switch (game)
        {
            case Game.P5R:
                _tblMerger = new P5RTblMerger(utils, logger, mergedFileCache, criFsApi);
                break;
            case Game.P4G:
                _tblMerger = new P4GTblMerger(utils, logger, mergedFileCache, criFsApi, pakEmulator);
                break;
            default:
                logger.Warning($"{game} does not support tbl merging");
                break;
        }
    }

    public void Merge(string[] cpks, ICriFsRedirectorApi.BindContext context)
    {
        _tblMerger?.Merge(cpks, context);
    }
}