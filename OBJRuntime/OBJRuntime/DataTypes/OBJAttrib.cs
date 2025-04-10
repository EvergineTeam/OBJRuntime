// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Mathematics;
using System.Collections.Generic;

namespace OBJRuntime.DataTypes
{
    public class OBJAttrib
    {
        public List<Vector3> Vertices = new List<Vector3>();       // 'v' xyz
        public List<float> VertexWeights = new List<float>();  // 'v' w
        public List<Vector3> Normals = new List<Vector3>();        // 'vn'
        public List<Vector2> Texcoords = new List<Vector2>();      // 'vt' (u,v)
        public List<float> TexcoordWs = new List<float>();     // 'vt' w (if present, else unused)
        public List<Vector3> Colors = new List<Vector3>();         // vertex colors, if present
        // extension
        public List<OBJSkinWeight> SkinWeights = new List<OBJSkinWeight>();
    }
}
