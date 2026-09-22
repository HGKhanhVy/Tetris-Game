using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BrickStacker.EditorTools
{
    // Keeps the AI/MCP editor tooling out of player builds.
    //
    // The Unity-MCP package re-adds its UNITY_MCP_READY define to every build target and re-enables
    // its NuGet DLLs for "Any Platform" on each domain reload, so editing Player Settings or the
    // plugin importers by hand does not stick. Its runtime assembly then drags SignalR, System.Text.Json,
    // Roslyn, R3 and friends into IL2CPP. Nothing in the game uses them, so they are dropped here,
    // at build time only; the editor keeps working with MCP as before.
    public class EditorToolingBuildFilter : IFilterBuildAssemblies
    {
        public int callbackOrder => 0;

        static readonly string[] StrippedScriptAssemblyPrefixes =
        {
            "com.IvanMurzak.Unity.MCP",
        };

        // Application-level libraries only. Low-level shims (System.Memory, System.Buffers,
        // Microsoft.Bcl.*...) stay: other plugins may depend on them and they are tiny.
        static readonly string[] StrippedPluginPrefixes =
        {
            "McpPlugin",
            "ReflectorNet",
            "R3",
            "Microsoft.AspNetCore.",
            "Microsoft.Extensions.",
            "Microsoft.CodeAnalysis",
            "System.Text.Json",
            "System.Text.Encodings.Web",
            "System.Threading.Channels",
            "System.IO.Pipelines",
            "System.Reflection.Metadata",
            "System.Collections.Immutable",
        };

        public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
        {
            var stripped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in assemblies)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (MatchesAny(name, StrippedScriptAssemblyPrefixes) || MatchesAny(name, StrippedPluginPrefixes))
                {
                    stripped.Add(name);
                }
            }

            if (stripped.Count == 0)
            {
                return assemblies;
            }

            // Stripping a library a kept assembly still references would break IL2CPP; bail out
            // and leave the build untouched rather than risk it.
            var conflict = FindKeptReferenceToStripped(assemblies, stripped);
            if (conflict != null)
            {
                Debug.LogWarning("[BuildFilter] Not stripping editor tooling: " + conflict);
                return assemblies;
            }

            Debug.Log("[BuildFilter] Stripped from player build: " + string.Join(", ", stripped.OrderBy(n => n)));
            return assemblies.Where(p => !stripped.Contains(Path.GetFileNameWithoutExtension(p))).ToArray();
        }

        // Uses the metadata references of the editor-loaded copies: the compiler only records
        // assemblies that are actually used, unlike the reference list handed to it. The editor
        // build may reference slightly more than the player build, which only errs on the safe side.
        static string FindKeptReferenceToStripped(string[] buildAssemblies, HashSet<string> stripped)
        {
            var kept = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in buildAssemblies)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (!stripped.Contains(name))
                {
                    kept.Add(name);
                }
            }

            foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = loaded.GetName().Name;
                if (!kept.Contains(name))
                {
                    continue;
                }

                foreach (var reference in loaded.GetReferencedAssemblies())
                {
                    if (stripped.Contains(reference.Name))
                    {
                        return name + " references " + reference.Name;
                    }
                }
            }
            return null;
        }

        static bool MatchesAny(string name, string[] prefixes)
        {
            foreach (var prefix in prefixes)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
