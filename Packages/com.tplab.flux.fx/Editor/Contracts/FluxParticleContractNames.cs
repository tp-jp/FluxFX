using System;
using System.Collections.Generic;

namespace TpLab.Flux.FX.Editor.Contracts
{
    static class FluxParticleContractNames
    {
        static readonly HashSet<string> Keywords = new HashSet<string>(
            ("abstract as base bool break byte case catch char checked class const continue decimal default delegate do " +
             "double else enum event explicit extern false finally fixed float for foreach goto if implicit in int " +
             "interface internal is lock long namespace new null object operator out override params private protected " +
             "public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw " +
             "true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while " +
             "__arglist __makeref __reftype __refvalue").Split(' '), StringComparer.Ordinal);

        internal static void RequireId(string id, string context)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Trim() != id)
            {
                throw new InvalidOperationException($"{context}: a non-empty ID without surrounding whitespace is required.");
            }
        }

        internal static void RequireIdentifier(string name, string context)
        {
            if (string.IsNullOrEmpty(name) || Keywords.Contains(name))
            {
                throw new InvalidOperationException($"{context}: invalid C# identifier '{name}'.");
            }
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (c == '_' || c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || i > 0 && c >= '0' && c <= '9') continue;
                throw new InvalidOperationException($"{context}: identifiers must use ASCII letters, digits or underscores: '{name}'.");
            }
        }
    }
}
