using Evergine.Mathematics;
using OBJRuntime.DataTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBJRuntime.Readers
{
    public static class Helpers
    {
        // Helper method: Splits a string into tokens, but doesn't handle advanced escaping rules.
        public static List<string> Tokenize(string line)
        {
            var tokens = new List<string>();
            var parts = line.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                tokens.Add(p.Trim());
            }
            return tokens;
        }

        public static bool TryParseFloat(string s, out float val)
        {
            return float.TryParse(
                s,
                NumberStyles.Float | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out val);
        }

        public static void ParseVector3(List<string> tokens, int startIndex, ref Vector3 arr)
        {
            // tokens: e.g. ["Kd", "0.1", "0.2", "0.3"]
            // parse from tokens[startIndex] up to 3.
            int count = Math.Min(3, tokens.Count - startIndex);
            for (int i = 0; i < count; i++)
            {
                if (Helpers.TryParseFloat(tokens[startIndex + i], out float val))
                {
                    arr[i] = val;
                }
            }
        }

        // Raw triple parse: i, i/j, i/j/k, i//k
        public static OBJIndex ParseRawTriple(string token)
        {
            OBJIndex idx = new OBJIndex() { VertexIndex = 0, TexcoordIndex = 0, NormalIndex = 0 };
            // We just do naive splitting by '/'
            // If there's no '/', it's just the v index
            string[] parts = token.Split('/');
            int vIdx = 0, vtIdx = 0, vnIdx = 0;

            if (!string.IsNullOrEmpty(parts[0]))
                int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out vIdx);

            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out vtIdx);

            if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
                int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out vnIdx);

            // Convert from 1-based to 0-based. Negative means relative.
            idx.VertexIndex = vIdx - 1;
            idx.TexcoordIndex = vtIdx - 1;
            idx.NormalIndex = vnIdx - 1;

            return idx;
        }
    }
}
