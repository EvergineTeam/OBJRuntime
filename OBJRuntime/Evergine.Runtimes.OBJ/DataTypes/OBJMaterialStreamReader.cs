// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using OBJRuntime.Readers;
using System.Collections.Generic;
using System.IO;

namespace OBJRuntime.DataTypes
{
    /// <summary>
    /// Reads MTL data from a Stream (like a MemoryStream).
    /// </summary>
    public class OBJMaterialStreamReader
    {
        private readonly Stream inStream;

        public OBJMaterialStreamReader(Stream inStream)
        {
            this.inStream = inStream;
        }

        public bool Read(
            string matId,
            List<OBJMaterial> materials,
            Dictionary<string, int> matMap,
            out string warning,
            out string error)
        {
            warning = "";
            error = "";

            if (inStream == null)
            {
                warning += "Material stream in error state.\n";
                return false;
            }

            using (var sr = new StreamReader(inStream, leaveOpen: true))
            {
                MtlLoader.Load(sr, materials, matMap, ref warning, ref error);
            }

            return true;
        }
    }
}
