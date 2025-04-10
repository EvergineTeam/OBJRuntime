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
    }
}
