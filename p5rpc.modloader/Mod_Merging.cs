using System;
using System.Diagnostics;
using CriFs.V2.Hook.Interfaces;
using CriFs.V2.Hook.Interfaces.Structs;
using CriFsV2Lib.Definitions.Utilities;
using p5rpc.modloader.Merging;
using Persona.Merger.Cache;
using Persona.Merger.Patching.Tbl;
using Persona.Merger.Patching.Tbl.Name;
using static p5rpc.modloader.Config;

namespace p5rpc.modloader;

public partial class Mod
{    
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
            new TblMerger(mergeUtils, _logger, _mergedFileCache, _criFsApi)
        };
        
        foreach (var fileMerger in fileMergers)
            fileMerger.Merge(cpks, context);

        _logger.Info("Merging Completed in {0}ms", watch.ElapsedMilliseconds);
        _mergedFileCache.RemoveExpiredItems();
        _ = _mergedFileCache.ToPathAsync();
    }

    // The names of the cpk file for each language
    private readonly Dictionary<Language, string[]> CpkNames = new()
    {
        { Language.Japanese, new string[] { "BASE.CPK", "DATA.CPK", "data/" }},
        { Language.English, new string[] { "EN.CPK", "_E.CPK", "data_EN" }},
        { Language.German, new string[] { "DE.CPK", "_DE.CPK", "data_DE" }},
        { Language.French, new string[] { "FR.CPK", "_FR.CPK", "data_FR" }},
        { Language.Italian, new string[] { "IT.CPK", "_IT.CPK", "data_IT" }},
        { Language.Korean, new string[] { "KR.CPK", "_K.CPK", "data_KR" }},
        { Language.Spanish, new string[] { "ES.CPK", "_ES.CPK", "data_ES" }},
        { Language.Simplified_Chinese, new string[] { "SC.CPK", "_CH.CPK", "data_CH" }},
        { Language.Traditional_Chinese, new string[] { "TC.CPK", "_CK.CPK", "data_CK" }},
    };
    
    private void ForceCpkFirst(string[] cpkFiles, Language language)
    {
        // Reorder array to force a specific cpk to be first
        var names = CpkNames[language];
        var cpkIndex = Array.FindIndex(cpkFiles, s => s.Contains(names[0], StringComparison.OrdinalIgnoreCase) || s.Contains(names[1], StringComparison.OrdinalIgnoreCase) || s.Contains(names[2], StringComparison.OrdinalIgnoreCase));
        if (cpkIndex != -1)
            (cpkFiles[0], cpkFiles[cpkIndex]) = (cpkFiles[cpkIndex], cpkFiles[0]);
    }

    private void ForceBaseCpkSecond(string[] cpkFiles)
    {
        // Reorder array to force a specific cpk to be first
        var names = CpkNames[Language.Japanese];
        var cpkIndex = Array.FindIndex(cpkFiles, s => s.Contains(names[0], StringComparison.OrdinalIgnoreCase) || s.Contains(names[1], StringComparison.OrdinalIgnoreCase) || s.Contains(names[2], StringComparison.OrdinalIgnoreCase));
        if (cpkIndex != -1)
            (cpkFiles[1], cpkFiles[cpkIndex]) = (cpkFiles[cpkIndex], cpkFiles[1]);
    }
}