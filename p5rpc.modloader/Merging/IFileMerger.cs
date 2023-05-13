using CriFs.V2.Hook.Interfaces;
using FileEmulationFramework.Lib.Utilities;
using Persona.Merger.Cache;
using Reloaded.Mod.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace p5rpc.modloader.Merging
{
    internal interface IFileMerger
    {
        void Merge(string[] cpks, ICriFsRedirectorApi.BindContext context);
    }
}
