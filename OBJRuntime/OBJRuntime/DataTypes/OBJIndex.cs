// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace OBJRuntime.DataTypes
{
    /// <summary>
    /// Index struct to support different indices for vtx/normal/texcoord.
    /// -1 means "not used".
    /// </summary>
    public struct OBJIndex
    {
        public int VertexIndex;
        public int NormalIndex;
        public int TexcoordIndex;
    }
}
