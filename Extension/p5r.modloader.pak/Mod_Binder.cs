using System.Diagnostics;
using CriFs.V2.Hook.Interfaces;
using CriFs.V2.Hook.Interfaces.Structs;
using CriFsV2Lib.Definitions.Utilities;
using FileEmulationFramework.Lib;
using p5r.modloader.pak.Template;
using PAK.Stream.Emulator.Interfaces.Structures.IO;
using Persona.Merger.Cache;
using Persona.Merger.Patching.Tbl;
using Persona.Merger.Patching.Tbl.Name;

namespace p5r.modloader.pak;

public partial class Mod
{
    private object _binderInputLock = new();

    private void OnBind(ICriFsRedirectorApi.BindContext context)
    {
        // Wait for cache to init first.
        _createMergedFileCacheTask.Wait();

        // Table merging
        // Note: Actual merging logic is optimised but code in mod could use some more work.
        var input = _pakEmulator.GetEmulatorInput();
        var cpks = _criFsApi.GetCpkFilesInGameDir();
        var criFsLib = _criFsApi.GetCriFsLib();
        var tasks = new List<ValueTask>();
        var watch = Stopwatch.StartNew();


        var pathToFileMap = context.RelativePathToFileMap;
        foreach(RouteGroupTuple group in input)
        {
            //var dir = group.Files.Directory.FullPath;
            foreach (var file in group.Files.Files)
                tasks.Add(CachePak(pathToFileMap,  file, cpks));
        }
    }

    private async ValueTask CachePak(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> pathToFileMap, string filePath, string[] cpks)
    {

        if (!pathToFileMap.TryGetValue(filePath, out var candidates))
            return;

        var pathInCpk = RemoveR2Prefix(filePath);
        if (!TryFindFileInAnyCpk(pathInCpk, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
        {
            _logger.Warning("Unable to find PAK in any CPK {0}", pathInCpk);
            return;
        }

        // Build cache key
        var cacheKey = GetCacheKeyAndSources(filePath, candidates, out var sources);
        if (_mergedFileCache.TryGet(cacheKey, sources, out var cachedFilePath))
        {
            _logger.Info("Loading Merged TBL {0} from Cache ({1})", filePath, cachedFilePath);
            ReplaceFileInBinderInput(pathToFileMap, filePath, cachedFilePath);
            return;
        }

        // Else Merge our Data
        // First we extract.
        await Task.Run(async () =>
        {
            _logger.Info("Cacheing {0} with key {1}.", filePath, cacheKey);
            await using var cpkStream = new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var reader = _criFsApi.GetCriFsLib().CreateCpkReader(cpkStream, false);
            using var extractedFile = reader.ExtractFile(cpkEntry.Files[fileIndex].File);

            
            _pakEmulator.TryCreateFromBytes(extractedFile.RawArray, filePath, cpkPath, out var stream);

            MemoryStream memoryStream= new MemoryStream();
            stream.CopyTo(memoryStream);

            // Then we store in cache.
            var item = await _mergedFileCache.AddAsync(cacheKey, sources, memoryStream.ToArray());
            ReplaceFileInBinderInput(pathToFileMap, filePath, Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath));
            _logger.Info("Cached to {0}.", item.RelativePath);
            
        });
    }
    private void ReplaceFileInBinderInput(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> binderInput, string filePath, string newFilePath)
    {
        lock (_binderInputLock)
        {
            binderInput[filePath] = new List<ICriFsRedirectorApi.BindFileInfo>()
        {
            new()
            {
                FullPath = newFilePath,
                ModId = "p5r.modloader",
                LastWriteTime = DateTime.UtcNow
            }
        };
        }
    }

    private static string GetCacheKeyAndSources(string filePath, List<ICriFsRedirectorApi.BindFileInfo> files, out CachedFileSource[] sources)
    {
        var modIds = new string[files.Count];
        sources = new CachedFileSource[files.Count];

        for (var x = 0; x < files.Count; x++)
        {
            modIds[x] = files[x].ModId;
            sources[x] = new CachedFileSource()
            {
                LastWrite = files[x].LastWriteTime
            };
        }

        return MergedFileCache.CreateKey(filePath, modIds);
    }

    private bool TryFindFileInAnyCpk(string filePath, string[] cpkFiles, out string cpkPath, out CpkCacheEntry cachedFile, out int fileIndex)
    {
        foreach (var cpk in cpkFiles)
        {
            cpkPath = cpk;
            cachedFile = _criFsApi.GetCpkFilesCached(cpk);

            if (cachedFile.FilesByPath.TryGetValue(filePath, out fileIndex))
                return true;
        }

        cpkPath = string.Empty;
        fileIndex = -1;
        cachedFile = default;
        return false;
    }
    private static string RemoveR2Prefix(string input)
    {
        return input.StartsWith(@"R2\")
            ? input.Substring(@"R2\".Length)
            : input;
    }
}
