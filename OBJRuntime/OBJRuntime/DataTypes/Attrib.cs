// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;

namespace OBJRuntime.DataTypes
{
    public class Attrib
    {
        public List<float> Vertices = new List<float>();       // 'v' xyz
        public List<float> VertexWeights = new List<float>();  // 'v' w
        public List<float> Normals = new List<float>();        // 'vn'
        public List<float> Texcoords = new List<float>();      // 'vt' (u,v)
        public List<float> TexcoordWs = new List<float>();     // 'vt' w (if present, else unused)
        public List<float> Colors = new List<float>();         // vertex colors, if present
        // extension
        public List<SkinWeight> SkinWeights = new List<SkinWeight>();
    }
}
