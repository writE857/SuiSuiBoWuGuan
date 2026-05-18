using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;

namespace Fix.Editor
{
    public static class FixEditorDef
    {
        private static readonly ISet<Def> Defs = new HashSet<Def>()
        {
        };

        private static List<string> defines = new List<string>();

        [InitializeOnLoadMethod]
        private static void InitEditor()
        {
            KeepSymbol();
            CompilationPipeline.compilationFinished += Check;
        }

        private static void Check(object obj) => KeepSymbol();

        private static void KeepSymbol()
        {
            if (BuildPipeline.isBuildingPlayer) return;
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            defines.Clear();
            defines.AddRange(PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';'));
            bool update = CheckDefs();


            if (update)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines));
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }

        private static bool CheckDefs()
        {
            bool update = false;

            foreach (var def in Defs)
            {
                int indexOf;
                if ((Type.GetType(def.assemblyQualifiedName) != null)
                    ^ ((indexOf = defines.IndexOf(def.symbol)) >= 0))
                {
                    if (indexOf >= 0) defines.RemoveAt(indexOf);
                    else defines.Add(def.symbol);
                    update = true;
                }
            }

            return update;
        }

        public static void RegisterDefs(params Def[] defs)
        {
            if (defs == null) return;
            foreach (var def in defs)
            {
                if (def == null) continue;
                Defs.Add(def);
            }
            KeepSymbol();
        }
        public static void RegisterDef(Def def)
        {
            if (def == null) return;
            Defs.Add(def);
            KeepSymbol();
        }

        public sealed class Def
        {
            public string assemblyQualifiedName;
            public string symbol;

            public Def(string assemblyQualifiedName, string symbol)
            {
                this.assemblyQualifiedName = assemblyQualifiedName;
                this.symbol = symbol;
            }

            private bool Equals(Def other)
            {
                return assemblyQualifiedName == other.assemblyQualifiedName && symbol == other.symbol;
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is Def other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((assemblyQualifiedName != null ? assemblyQualifiedName.GetHashCode() : 0) * 397) ^ (symbol != null ? symbol.GetHashCode() : 0);
                }
            }
        }
    }
}