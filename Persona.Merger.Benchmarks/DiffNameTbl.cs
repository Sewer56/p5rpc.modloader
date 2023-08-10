using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Persona.Merger.Patching.Tbl.FieldResolvers.P5R.Name;
using Persona.Merger.Tests;

namespace Persona.Merger.Benchmarks;

[MemoryDiagnoser]
public class DiffNameTbl
{
    private byte[] _origArr = null!;
    private byte[] _tgtArr = null!;
    private GCHandle _orig;
    private GCHandle _tgt;

    private ParsedNameTable _originalTable;
    private ParsedNameTable _originalTableForApplyDiff;
    private ParsedNameTable _targetTable;
    private NameTableDiff[] _diffsToApply = null!;

    [GlobalSetup]
    public void Setup()
    {
        _origArr = File.ReadAllBytes(P5RAssets.NameBefore);
        _tgtArr  = File.ReadAllBytes(P5RAssets.NameAfter);
        _orig    = GCHandle.Alloc(_origArr, GCHandleType.Pinned);
        _tgt     = GCHandle.Alloc(_tgtArr, GCHandleType.Pinned);
        _originalTable = ParsedNameTable.ParseTable(_origArr);
        _targetTable = ParsedNameTable.ParseTable(_tgtArr);
        _diffsToApply = NameTableMerger.CreateDiffs(_originalTable, new[] { _targetTable });
        _originalTableForApplyDiff = _originalTable;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _orig.Free();
        _tgt.Free();
    }


    [Benchmark]
    public ParsedNameTable Parse() => ParsedNameTable.ParseTable(_origArr);

    [Benchmark]
    public NameTableDiff[] CreateDiff() => NameTableMerger.CreateDiffs(_originalTable, new[] { _targetTable });
    
    // Due to nature of implementation, reusing instance is okay.
    [Benchmark]
    public ParsedNameTable Apply_Diff() => NameTableMerger.Merge(_originalTableForApplyDiff, _diffsToApply);
}