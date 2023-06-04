using CriFs.V2.Hook.Interfaces;

namespace p5rpc.modloader.Merging;

internal interface IFileMerger
{
    void Merge(string[] cpks, ICriFsRedirectorApi.BindContext context);
}