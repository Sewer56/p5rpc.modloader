using CriFs.V2.Hook.Interfaces;
using FileEmulationFramework.Lib.Utilities;
using PAK.Stream.Emulator.Interfaces;
using PAK.Stream.Emulator.Interfaces.Structures.IO;
using Persona.Merger.Cache;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using static p5rpc.modloader.Merging.MergeUtils;

namespace p5rpc.modloader.Merging
{
    internal class PakMerger : IFileMerger
    {
        private MergeUtils _utils;
        private Logger _logger;
        private MergedFileCache _mergedFileCache;
        private ICriFsRedirectorApi _criFsApi;
        private IPakEmulator _pakEmulator;

        internal PakMerger(MergeUtils utils, Logger logger, MergedFileCache mergedFileCache, ICriFsRedirectorApi criFsApi, IPakEmulator pakEmulator)
        {
            _utils = utils;
            _logger = logger;
            _mergedFileCache = mergedFileCache;
            _criFsApi = criFsApi;
            _pakEmulator = pakEmulator;
        }

        public void Merge(string[] cpks, ICriFsRedirectorApi.BindContext context)
        {
            //Debugger.Launch();
            var input = _pakEmulator.GetEmulatorInput();
            var pathToFileMap = context.RelativePathToFileMap;
            var tasks = new List<ValueTask>();
            foreach (RouteGroupTuple group in input)
            {
                var dir = group.Route;
                string dirdir = Path.GetDirectoryName(dir) != null ? Path.GetDirectoryName(dir) : "";
                if (dirdir.Contains('.'))
                {
                    dir = dir.Substring(0, dir.IndexOf(Path.DirectorySeparatorChar, dir.IndexOf(".")));
                }
                _logger.Info("Route: {0}", dir);
                tasks.Add(CachePak(pathToFileMap, @"R2\" + dir, cpks, context.BindDirectory));
            }
        }

        private async ValueTask CachePak(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> pathToFileMap, string route, string[] cpks, string bindDirectory)
        {

            string pathInCpk = RemoveR2Prefix(route);
            string cpkFinderPath = Path.GetDirectoryName(pathInCpk) == "" || Path.GetDirectoryName(pathInCpk) == null ? "\\" + pathInCpk : pathInCpk;
            if (!_utils.TryFindFileInAnyCpk(cpkFinderPath, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
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
                string[] modids = new string[1] { "p5rpc.modloader" };
                var sources = new CachedFileSource[1] { new CachedFileSource() };
                var cacheKey = MergedFileCache.CreateKey(route, modids);

                var item = await _mergedFileCache.AddAsync(cacheKey, sources, extractedFile.RawArray);

                string pakPath = Path.Combine(bindDirectory, route);
                string dirs = Path.GetDirectoryName(pakPath);
                if (dirs != null)
                    Directory.CreateDirectory(Path.GetDirectoryName(pakPath));
                if (!_pakEmulator.TryCreateFromFileSlice(Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath), 0, pathInCpk, pakPath))
                {
                    _logger.Error("Oops!");
                    return;
                }


                _utils.ReplaceFileInBinderInput(pathToFileMap, route, pakPath);
                _logger.Info("File emulated at {0}.", pakPath);

            });
        }
    }
}
