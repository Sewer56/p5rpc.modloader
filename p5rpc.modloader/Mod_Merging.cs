using System.Diagnostics;
using CriFs.V2.Hook.Interfaces;
using p5rpc.modloader.Merging;
using p5rpc.modloader.Merging.Tbl;
using static p5rpc.modloader.Config;

namespace p5rpc.modloader;

public partial class Mod
{    
    // The names of the cpk file for each language
    private readonly Dictionary<Language, string[]> _cpkNames = new()
    {
        { Language.Japanese, new[] { "BASE.CPK", "DATA.CPK", "data/" }},
        { Language.English, new[] { "EN.CPK", "_E.CPK", "data_EN" }},
        { Language.German, new[] { "DE.CPK", "_DE.CPK", "data_DE" }},
        { Language.French, new[] { "FR.CPK", "_FR.CPK", "data_FR" }},
        { Language.Italian, new[] { "IT.CPK", "_IT.CPK", "data_IT" }},
        { Language.Korean, new[] { "KR.CPK", "_K.CPK", "data_KR" }},
        { Language.Spanish, new[] { "ES.CPK", "_ES.CPK", "data_ES" }},
        { Language.SimplifiedChinese, new[] { "SC.CPK", "_CH.CPK", "data_CH" }},
        { Language.TraditionalChinese, new[] { "TC.CPK", "_CK.CPK", "data_CK" }},
    };
    
    private void OnBind(ICriFsRedirectorApi.BindContext context)
    {
        // Wait for cache to init first.
        _createMergedFileCacheTask.Wait();
        
        // File merging
        var watch = Stopwatch.StartNew();
        var cpks = _criFsApi.GetCpkFilesInGameDir();

        ForceBaseCpkSecond(cpks);
        ForceCpkFirst(cpks, Configuration.CPKLanguage);

        var mergeUtils = new MergeUtils(_criFsApi);
        List<IFileMerger> fileMergers = new()
        {
            new BfMerger(mergeUtils, _logger, _mergedFileCache, _criFsApi, _bfEmulator, _pakEmulator, Game),
            new TblMerger(mergeUtils, _logger, _mergedFileCache, _criFsApi, _pakEmulator, Game),
            new SpdMerger(mergeUtils, _logger, _mergedFileCache, _criFsApi, _spdEmulator, _pakEmulator, Game),
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