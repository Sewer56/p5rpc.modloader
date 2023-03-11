using System.Diagnostics;
using CriFs.V2.Hook.Interfaces;
using CriFs.V2.Hook.Interfaces.Structs;
using PAK.Stream.Emulator.Interfaces.Structures.IO;
using Persona.Merger.Cache;

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
        var tasks = new List<ValueTask>();
        var watch = Stopwatch.StartNew();

        var pathToFileMap = context.RelativePathToFileMap;
        foreach (RouteGroupTuple group in input)
        {
            var dir = group.Route;
            string dirdir = Path.GetDirectoryName(dir) != null ? Path.GetDirectoryName(dir) : "" ;
            if (dirdir.Contains('.'))
            {
                dir = dir.Substring(0, dir.IndexOf(Path.DirectorySeparatorChar, dir.IndexOf(".")));
            }
            _logger.Info("Route: {0}", dir);
            tasks.Add(CachePak(pathToFileMap, @"R2\" + dir, cpks));
        }
    }

    private async ValueTask CachePak(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> pathToFileMap, string route, string[] cpks)
    {

        string pathInCpk = RemoveR2Prefix(route);
        if (!TryFindFileInAnyCpk(pathInCpk, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
        {
            _logger.Warning("Unable to find PAK in any CPK {0}", pathInCpk);
            return;
        }
        
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
            string dirs = Path.GetDirectoryName(pakPath);
            if (dirs != null)
                Directory.CreateDirectory(Path.GetDirectoryName(pakPath));
            if (!_pakEmulator.TryCreateFromFileSlice(Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath), 0, pathInCpk, pakPath))
            {
                _logger.Error("Oops!");
                return;
            }


            ReplaceFileInBinderInput(pathToFileMap, route, pakPath);
            _logger.Info("File emulated at {0}.", pakPath);

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
                ModId = "p5rpc.modloader.pak",
                LastWriteTime = DateTime.UtcNow
            }
        };
        }
    }

    
    private bool TryFindFileInAnyCpk(string filePath, string[] cpkFiles, out string cpkPath, out CpkCacheEntry cachedFile, out int fileIndex)
    {
        _logger.Info($"Found {filePath}");
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
