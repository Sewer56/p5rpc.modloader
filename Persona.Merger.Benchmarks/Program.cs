// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using Persona.Merger.Benchmarks;

Console.WriteLine("Hello, World!");

BenchmarkRunner.Run<DiffNameTbl>();