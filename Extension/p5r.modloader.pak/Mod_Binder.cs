using System.Diagnostics;
using System.Threading.Tasks;
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
    private ICriFsRedirectorApi.BindContext bind;

    private void OnBind(ICriFsRedirectorApi.BindContext context)
    {
        // Wait for cache to init first.
        _createMergedFileCacheTask.Wait();

        bind = context;

        var input = _pakEmulator.GetEmulatorInput();
        var cpks = _criFsApi.GetCpkFilesInGameDir();
        var criFsLib = _criFsApi.GetCriFsLib();
        var tasks = new List<ValueTask>();
        var watch = Stopwatch.StartNew();

        _logger.Info("PLEASE GET HERE");
        var pathToFileMap = context.RelativePathToFileMap;
        
        foreach(RouteGroupTuple group in input)
        {
            var dir = group.Route;
            var file = group.Files.Directory.FullPath;
            _logger.Info("Route: {0}", dir);
            tasks.Add(CachePak(pathToFileMap, file, @"R2\" + dir, cpks));
        }
    }

    private async ValueTask CachePak(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> pathToFileMap, string filePath, string route, string[] cpks)
    {

        bool exists = pathToFileMap.TryGetValue(filePath, out var candidates);
        
        string pathInCpk= RemoveR2Prefix(route);
        if (!TryFindFileInAnyCpk(pathInCpk, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
        {
            _logger.Warning("Unable to find PAK in any CPK {0}", pathInCpk);
            return;
        }
        /*
        if (exists)
        {
            // Build cache key
            var cacheKey = GetCacheKeyAndSources(filePath, candidates, out var sources);
            if (_mergedFileCache.TryGet(cacheKey, sources, out var cachedFilePath))
            {
                _logger.Info("Loading Merged TBL {0} from Cache ({1})", filePath, cachedFilePath);
                ReplaceFileInBinderInput(pathToFileMap, filePath, cachedFilePath);
                return;
            }
        }
        */
        // Else Merge our Data
        // First we extract.
        await Task.Run(async () =>
        {
            await using var cpkStream = new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var reader = _criFsApi.GetCriFsLib().CreateCpkReader(cpkStream, false);
            using var extractedFile = reader.ExtractFile(cpkEntry.Files[fileIndex].File);


            // Then we store in cache.
            string[] modids = new string[1] { "p5rpc.modloader.pak" };
            var sources = new CachedFileSource[1] { new CachedFileSource() };
            var cacheKey = MergedFileCache.CreateKey(route, modids);

            var item = await _mergedFileCache.AddAsync(cacheKey, sources, extractedFile.RawArray);

            string pakPath = Path.Combine(bind.BindDirectory, route);
            Directory.CreateDirectory(Path.GetDirectoryName(pakPath));
            if (!_pakEmulator.TryCreateFromFileSlice(Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath), 0, pathInCpk, pakPath))
            {
                _logger.Error("Oops!");
                return;
            }


            ReplaceFileInBinderInput(pathToFileMap, route, pakPath);
            //_logger.Info("Cached to {0}.", item.RelativePath);

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
                ModId = "p5r.modloader.pak",
                LastWriteTime = DateTime.UtcNow
            }
        };
        }
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
