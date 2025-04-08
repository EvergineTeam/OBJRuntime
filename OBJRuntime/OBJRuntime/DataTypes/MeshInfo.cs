// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;

namespace OBJRuntime.DataTypes
{
    public class MeshInfo
    {
        public List<Index> Indices = new List<Index>();
        public List<uint> NumFaceVertices = new List<uint>();   // number of vertices per face
        public List<int> MaterialIds = new List<int>();         // per-face material ID
        public List<uint> SmoothingGroupIds = new List<uint>(); // per-face smoothing group ID
        public List<Tag> Tags = new List<Tag>();
    }
}
