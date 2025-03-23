using CriFs.V2.Hook.Interfaces;
using Persona.Merger.Cache;
using FileEmulationFramework.Lib.Utilities;
using PAK.Stream.Emulator.Interfaces;
using PAK.Stream.Emulator.Interfaces.Structures.IO;
using Reloaded.Universal.Localisation.Framework.Interfaces;

namespace p5rpc.modloader.Merging.Tbl;

internal class TblMerger : IFileMerger
{
    private IFileMerger? _tblMerger;

    internal TblMerger(MergeUtils utils, Logger logger, MergedFileCache mergedFileCache, ICriFsRedirectorApi criFsApi,
        IPakEmulator pakEmulator, ILocalisationFramework localisationFramework, Game game)
    {
        switch (game)
        {
            case Game.P5R:
                _tblMerger = new P5RTblMerger(utils, logger, mergedFileCache, criFsApi, localisationFramework);
                break;
            case Game.P4G:
                _tblMerger = new P4GTblMerger(utils, logger, mergedFileCache, criFsApi, pakEmulator, localisationFramework);
                break;
            case Game.P3P:
                _tblMerger = new P3PTblMerger(utils, logger, mergedFileCache, criFsApi, pakEmulator, localisationFramework);
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

    /// <summary>
    /// Finds files that mods have edited in paks that match a route
    /// </summary>
    /// <param name="pakFiles">A list of pak files from pak emulator</param>
    /// <param name="route">The route for the file</param>
    /// <returns>A list of files from modded paks that match the file</returns>
    internal static List<string> FindInPaks(RouteGroupTuple[] pakFiles, string route, string fileName)
    {
        List<string> candidates = new();
        foreach (var group in pakFiles.Where(x => x.Route.Equals(route, StringComparison.OrdinalIgnoreCase)))
        {
            candidates.AddRange(group.Files.Files
                .Where(x => x.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                .Select(file => $@"{group.Files.Directory.FullPath}\{file}"));
        }

        return candidates;
    }
}