using CriFs.V2.Hook.Interfaces;
using FileEmulationFramework.Lib.Utilities;
using Persona.Merger.Cache;
using BMD.File.Emulator.Interfaces;
using static p5rpc.modloader.Merging.MergeUtils;
using BMD.File.Emulator.Interfaces.Structures.IO;
using PAK.Stream.Emulator.Interfaces;
using Persona.Merger.Patching.Tbl.FieldResolvers.P4G;
using Persona.Merger.Patching.Tbl.FieldResolvers.P3P;
using Reloaded.Universal.Localisation.Framework.Interfaces;

namespace p5rpc.modloader.Merging;

internal class BmdMerger : IFileMerger
{
    private readonly MergeUtils _utils;
    private readonly Logger _logger;
    private readonly MergedFileCache _mergedFileCache;
    private readonly ICriFsRedirectorApi _criFsApi;
    private readonly IBmdEmulator _bmdEmulator;
    private readonly IPakEmulator _pakEmulator;
    private readonly Game _game;

    internal BmdMerger(MergeUtils utils, Logger logger, MergedFileCache mergedFileCache, ICriFsRedirectorApi criFsApi, IBmdEmulator bmdEmulator, IPakEmulator pakEmulator, Game game, Language language)
    {
        _utils = utils;
        _logger = logger;
        _mergedFileCache = mergedFileCache;
        _criFsApi = criFsApi;
        _bmdEmulator = bmdEmulator;
        _pakEmulator = pakEmulator;
        _game = game;
        
        var gameEncodings = BfMerger.Encodings[game];
        if (gameEncodings.ContainsKey(language))
        {
            var encoding = gameEncodings[language];
            _logger.Info("Set bf emulator encoding to {0}", encoding);
            _bmdEmulator.SetEncoding(encoding);
        }
        else
        {
            _logger.Error(
                "Encoding for {0} is not known, using default encoding. " +
                "If script tools does have an encoding for this language please report this so it can be supported.",
                language.Name);
        }

    }

    public void Merge(string[] cpks, ICriFsRedirectorApi.BindContext context)
    {
        var input = _bmdEmulator.GetEmulatorInput();
        var pathToFileMap = context.RelativePathToFileMap;
        var pakGroups = _pakEmulator.GetEmulatorInput();
        var tasks = new List<ValueTask>();
        Dictionary<string, List<string>> looseBmds = new();
        Dictionary<string, BmdMsgTuple> pakedBmds = new();
        CachedFileSource[] cpkSources = cpks.Select(cpk => new CachedFileSource { LastWrite = File.GetLastWriteTime(cpk) }).ToArray();

        foreach (RouteFileTuple group in input)
        {
            var route = Path.ChangeExtension(group.Route, ".bmd");

            // Loose bmds
            var bmdRoutes = pathToFileMap.Keys.Where(x => x.Contains(route, StringComparison.OrdinalIgnoreCase));
            foreach (var bmdRoute in bmdRoutes)
            {
                if (!looseBmds.ContainsKey(bmdRoute))
                    looseBmds[bmdRoute] = new List<string> { group.File };
                else
                    looseBmds[bmdRoute].Add(group.File);
            }

            // bmds in pak files
            foreach (var pakGroup in pakGroups)
            {
                var looseBmdRoutes = pakGroup.Files.Files.Where(x => $@"{pakGroup.Route}\{x}".Contains(route, StringComparison.OrdinalIgnoreCase));
                foreach (var bmdRoute in looseBmdRoutes)
                {
                    var fullBmdRoute = $@"{pakGroup.Route}\{bmdRoute}";
                    if (!pakedBmds.ContainsKey(fullBmdRoute))
                        pakedBmds[fullBmdRoute] = new BmdMsgTuple($@"{pakGroup.Files.Directory.FullPath}\{bmdRoute}", [group.File]);
                    else
                    {
                        pakedBmds[fullBmdRoute].MsgPaths.Add(group.File);
                        pakedBmds[fullBmdRoute].BmdPaths.Add($@"{pakGroup.Files.Directory.FullPath}\{bmdRoute}"); // Ensure highest priority bmd is used
                    }
                }

            }
            _logger.Info("Route: {0}", route);
        }

        foreach (var routePair in looseBmds)
            tasks.Add(CacheBmd(pathToFileMap, routePair.Key, cpks, routePair.Value, context.BindDirectory));

        foreach (var routePair in pakedBmds)
            tasks.Add(CachePakedBmd(routePair.Value.MsgPaths, routePair.Value.BmdPaths, routePair.Key, cpks, cpkSources));

        Task.WhenAll(tasks.Select(x => x.AsTask())).Wait();
        _logger.Info($"Finished merging bmd files");
    }

    private async ValueTask CachePakedBmd(List<string> msgPaths, List<string> bmdPaths, string route, string[] cpks, CachedFileSource[] cpkSources)
    {
        // Try and get cached merged bmd
        string[] modIds = { "p5rpc.modloader" };
        var mergedKey = MergedFileCache.CreateKey(route, modIds);
        CachedFileSource[] msgSources = msgPaths.Select(file => new CachedFileSource { LastWrite = File.GetLastWriteTime(file) }).ToArray();
        
        if (_mergedFileCache.TryGet(mergedKey, msgSources, out var mergedCachePath))
        {
            _logger.Info("Loading Merged BMD {0} from Cache ({1})", route, mergedCachePath);
            foreach (var path in bmdPaths)
            {
                _bmdEmulator.RegisterBmd(mergedCachePath, path);
            }
            return;
        }

        // Get pak file
        var extensionIndex = route.IndexOf(".", StringComparison.OrdinalIgnoreCase);
        var index = route.IndexOf(Path.DirectorySeparatorChar, extensionIndex);
        var pakPath = route.Substring(0, index);

        string cpkFinderPath = string.IsNullOrEmpty(Path.GetDirectoryName(pakPath)) ? "\\" + pakPath : pakPath;

        if (!_utils.TryFindFileInAnyCpk(cpkFinderPath, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
        {
            _logger.Warning("Unable to find PAK in any CPK {0}", pakPath);
            return;
        }

        // Then we store in cache.
        var originalKey = MergedFileCache.CreateKey($"OG/{route}", modIds);

        var bmdPathInPak = route.Substring(index + 1);

        if (!_mergedFileCache.TryGet(originalKey, cpkSources, out var cachedPath))
        {
            // Extract bmd from pak
            await Task.Run(async () =>
            {
                await using var cpkStream = new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                using var reader = _criFsApi.GetCriFsLib().CreateCpkReader(cpkStream, false);
                using var extractedFile = reader.ExtractFile(cpkEntry.Files[fileIndex].File);
                var bmdFile = ExtractBmd(extractedFile.RawArray, bmdPathInPak);
                if (bmdFile == null)
                {
                    _logger.Error($"Failed to extract {bmdPathInPak} from {pakPath}");
                    return;
                }
                _logger.Info($"Extracted {bmdPathInPak} from {pakPath}");
                var item = await _mergedFileCache.AddAsync(originalKey, cpkSources, (ReadOnlyMemory<byte>)bmdFile);
                cachedPath = Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath);
            });
        }

        if (cachedPath == null) return;

        var bmdPath = bmdPaths[bmdPaths.Count - 1];
        string? dir = Path.GetDirectoryName(bmdPath);
        if (dir != null)
            Directory.CreateDirectory(dir);

        if (!_bmdEmulator.TryCreateFromBmd(cachedPath, route, bmdPath))
        {
            _logger.Error($"Cannot Create File From {bmdPath}!");
            return;
        }

        // Cache merged
        var item = await _mergedFileCache.AddAsync(mergedKey, msgSources, File.ReadAllBytes(bmdPath));
        _logger.Info("Merge {0} Complete. Cached to {1}.", route, item.RelativePath);

        // Register all the bmds to the one emulated one (only the highest priority should ever actually be used though)
        for (int i = 0; i < bmdPaths.Count - 1; i++)
            _bmdEmulator.RegisterBmd($"{_mergedFileCache.CacheFolder}\\{item.RelativePath}", bmdPaths[i]);
    }

    private ReadOnlyMemory<byte>? ExtractBmd(byte[] pak, string bmdPathInPak)
    {
        if (!bmdPathInPak.Equals(@"battle\msgtbl.bmd", StringComparison.OrdinalIgnoreCase))
            return _pakEmulator.GetEntry(new MemoryStream(pak), bmdPathInPak);

        var msgTbl = _pakEmulator.GetEntry(new MemoryStream(pak), "battle/MSG.TBL");
        if (msgTbl == null)
            return null;

        return _game == Game.P4G ? 
            P4GTblPatcher.GetSegment(msgTbl.Value.ToArray(), Persona.Merger.Patching.Tbl.FieldResolvers.P4G.TblType.Message, 4) : 
            P3PTblPatcher.GetSegment(msgTbl.Value.ToArray(), Persona.Merger.Patching.Tbl.FieldResolvers.P3P.TblType.Message, 4);
    }

    private async ValueTask CacheBmd(Dictionary<string, List<ICriFsRedirectorApi.BindFileInfo>> pathToFileMap, string route, string[] cpks, List<string> msgPaths, string bindDirectory)
    {
        // Try and get cached merged bmd
        string bmdPath = Path.Combine(bindDirectory, route);
        string[] modIds = { "p5rpc.modloader" };
        var mergedKey = MergedFileCache.CreateKey(route, modIds);
        CachedFileSource[] msgSources = msgPaths.Select(file => new CachedFileSource { LastWrite = File.GetLastWriteTime(file) }).ToArray();
        if (_mergedFileCache.TryGet(mergedKey, msgSources, out var mergedCachePath))
        {
            _logger.Info("Loading Merged BMD {0} from Cache ({1})", route, mergedCachePath);
            _bmdEmulator.RegisterBmd(mergedCachePath, bmdPath);
            _utils.ReplaceFileInBinderInput(pathToFileMap, route, mergedCachePath);
            return;
        }

        string pathInCpk = RemoveR2Prefix(route);
        string cpkFinderPath = string.IsNullOrEmpty(Path.GetDirectoryName(pathInCpk)) ? "\\" + pathInCpk : pathInCpk;

        if (!_utils.TryFindFileInAnyCpk(cpkFinderPath, cpks, out var cpkPath, out var cpkEntry, out int fileIndex))
        {
            _logger.Warning("Unable to find BMD in any CPK {0}", pathInCpk);
            return;
        }

        // Then we store in cache.
        string[] modids = { "p5rpc.modloader" };
        var sources = new[] { new CachedFileSource() };
        var cacheKey = MergedFileCache.CreateKey(route, modids);

        if (!_mergedFileCache.TryGet(cacheKey, sources, out var cachedPath))
        {
            // Else Merge our Data
            // First we extract.
            await Task.Run(async () =>
            {
                await using var cpkStream = new FileStream(cpkPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                using var reader = _criFsApi.GetCriFsLib().CreateCpkReader(cpkStream, false);
                using var extractedFile = reader.ExtractFile(cpkEntry.Files[fileIndex].File);
                var item = await _mergedFileCache.AddAsync(cacheKey, sources, extractedFile.RawArray.AsMemory(0, extractedFile.Count));
                cachedPath = Path.Combine(_mergedFileCache.CacheFolder, item.RelativePath);
            });
        }

        string? dir = Path.GetDirectoryName(bmdPath);
        if (dir != null)
            Directory.CreateDirectory(dir);

        if (!_bmdEmulator.TryCreateFromBmd(cachedPath!, pathInCpk, bmdPath))
        {
            _logger.Error($"Cannot Create File From {bmdPath}!");
            return;
        }

        // Cache merged
        var item = await _mergedFileCache.AddAsync(mergedKey, msgSources, File.ReadAllBytes(bmdPath));
        _utils.ReplaceFileInBinderInput(pathToFileMap, route, bmdPath);
        _logger.Info("Merge {0} Complete. Cached to {1}.", route, item.RelativePath);
    }
}

internal class BmdMsgTuple
{
    internal List<string> BmdPaths { get; set; }
    internal List<string> MsgPaths { get; set; }

    internal BmdMsgTuple(string bmdPath, List<string> msgPaths)
    {
        BmdPaths = new List<string> { bmdPath };
        MsgPaths = msgPaths;
    }
}