using System.Diagnostics;
using CriFs.V2.Hook.Interfaces;
using p5rpc.modloader.Merging;
using p5rpc.modloader.Merging.Tbl;
using Reloaded.Universal.Localisation.Framework.Interfaces;

namespace p5rpc.modloader;

public partial class Mod
{    
    // The names of the cpk file for each language
    private readonly Dictionary<Language, string[]> _cpkNames = new()
    {
        { Language.Japanese, ["BASE.CPK", "DATA.CPK", "data\\umd0.cpk"] },
        { Language.English, ["EN.CPK", "_E.CPK", "data_EN"] },
        { Language.German, ["DE.CPK", "_DE.CPK", "data_DE"] },
        { Language.French, ["FR.CPK", "_FR.CPK", "data_FR"] },
        { Language.Italian, ["IT.CPK", "_IT.CPK", "data_IT"] },
        { Language.Korean, ["KR.CPK", "_K.CPK", "data_KR"] },
        { Language.Spanish, ["ES.CPK", "_ES.CPK", "data_ES"] },
        { Language.SimplifiedChinese, ["SC.CPK", "_CK.CPK", "data_CK"] },
        { Language.TraditionalChinese, ["TC.CPK", "_CH.CPK", "data_CH"] },
    };
    
    private void OnBind(ICriFsRedirectorApi.BindContext context)
    {
        // Wait for cache to init first.
        _createMergedFileCacheTask.Wait();
        
        // File merging
        var watch = Stopwatch.StartNew();
        var cpks = _criFsApi.GetCpkFilesInGameDir();

        ForceBaseCpkSecond(cpks);
        ForceCpkFirst(cpks, _language);

        var mergeUtils = new MergeUtils(_criFsApi);
        List<IFileMerger> fileMergers = new()
        {
            new BfMerger(mergeUtils, _logger, _mergedFileCache, _criFsApi, _bfEmulator, _pakEmulator, Game, _language),
            new BmdMerger(mergeUtils, _logger, _mergedFileCache, _criFsApi, _bmdEmulator, _pakEmulator, Game, _language),
            new TblMerger(mergeUtils, _logger, _mergedFileCache, _criFsApi, _pakEmulator,_localisationFramework,  Game),
            new SpdMerger(mergeUtils, _logger, _mergedFileCache, _criFsApi, _spdEmulator, _pakEmulator),
            new PakMerger(mergeUtils, _logger, _mergedFileCache, _criFsApi, _pakEmulator),
        };
        
        foreach (var fileMerger in fileMergers)
            fileMerger.Merge(cpks, context);

        _logger.Info("Merging Completed in {0}ms", watch.ElapsedMilliseconds);
        _mergedFileCache.RemoveExpiredItems();
        _ = _mergedFileCache.ToPathAsync();
    }

    private void ForceCpkFirst(string[] cpkFiles, Language language)
    {
        // Reorder array to force a specific cpk to be first
        var names = _cpkNames[language];
        var cpkIndex = Array.FindIndex(cpkFiles, s => s.Contains(names[0], StringComparison.OrdinalIgnoreCase) || s.Contains(names[1], StringComparison.OrdinalIgnoreCase) || s.Contains(names[2], StringComparison.OrdinalIgnoreCase));
        if (cpkIndex != -1)
            (cpkFiles[0], cpkFiles[cpkIndex]) = (cpkFiles[cpkIndex], cpkFiles[0]);
    }

    private void ForceBaseCpkSecond(string[] cpkFiles)
    {
        // Reorder array to force a specific cpk to be first
        var names = _cpkNames[Language.Japanese];
        var cpkIndex = Array.FindIndex(cpkFiles, s => s.Contains(names[0], StringComparison.OrdinalIgnoreCase) || s.Contains(names[1], StringComparison.OrdinalIgnoreCase) || s.Contains(names[2], StringComparison.OrdinalIgnoreCase));
        if (cpkIndex != -1)
            (cpkFiles[1], cpkFiles[cpkIndex]) = (cpkFiles[cpkIndex], cpkFiles[1]);
    }
}