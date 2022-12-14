using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Persona.Merger.Patching.Tbl.Name;
using Persona.Merger.Tests;
using Sewer56.StructuredDiff;

namespace Persona.Merger.Benchmarks;

[MemoryDiagnoser]
public class DiffNameTbl
{
    
    private byte[] _origArr = null!;
    private byte[] _tgtArr = null!;
    private GCHandle _orig;
    private GCHandle _tgt;

    private ParsedNameTable _originalTable;
    private ParsedNameTable _originalTable_ForApplyDiff;
    private ParsedNameTable _targetTable;
    private NameTableDiff[] _diffsToApply;

    [GlobalSetup]
    public void Setup()
    {
        _origArr = File.ReadAllBytes(Assets.NameBefore);
        _tgtArr  = File.ReadAllBytes(Assets.NameAfter);
        _orig    = GCHandle.Alloc(_origArr, GCHandleType.Pinned);
        _tgt     = GCHandle.Alloc(_tgtArr, GCHandleType.Pinned);
        _originalTable = ParsedNameTable.ParseTable(_origArr);
        _targetTable = ParsedNameTable.ParseTable(_tgtArr);
        _diffsToApply = NameTableMerger.CreateDiffs(_originalTable, new[] { _targetTable });
        _originalTable_ForApplyDiff = _originalTable;
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
    public ParsedNameTable Apply_Diff() => NameTableMerger.Merge(_originalTable_ForApplyDiff, _diffsToApply);
}