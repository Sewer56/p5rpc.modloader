using CriFs.V2.Hook.Interfaces.Structs;
using CriFs.V2.Hook.Interfaces;
using Persona.Merger.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace p5rpc.modloader.Merging
{
    public class MergeUtils
    {
        private object _binderInputLock = new();
        private ICriFsRedirectorApi _criFsApi;

        public MergeUtils(ICriFsRedirectorApi criFsApi)
        {
            _criFsApi = criFsApi;
        }

        internal void ReplaceFileInBinderInput(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> binderInput, string filePath, string newFilePath)
        {
            lock (_binderInputLock)
            {
                binderInput[filePath] = new List<ICriFsRedirectorApi.BindFileInfo>()
            {
                new()
                {
                    FullPath = newFilePath,
                    ModId = "p5rpc.modloader",
                    LastWriteTime = DateTime.UtcNow
                }
            };
            }
        }

        internal static string GetCacheKeyAndSources(string filePath, List<ICriFsRedirectorApi.BindFileInfo> files, out CachedFileSource[] sources)
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

        internal bool TryFindFileInAnyCpk(string filePath, string[] cpkFiles, out string cpkPath, out CpkCacheEntry cachedFile, out int fileIndex)
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

        internal static string RemoveR2Prefix(string input)
        {
            return input.StartsWith(@"R2\")
                ? input.Substring(@"R2\".Length)
                : input;
        }
    }
}
