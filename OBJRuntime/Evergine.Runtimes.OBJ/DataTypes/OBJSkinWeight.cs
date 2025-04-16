// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;

namespace OBJRuntime.DataTypes
{
    public class OBJSkinWeight
    {
        // Index in the "attrib_t.vertices" array
        public int VertexId;
        public List<OBJJointAndWeight> WeightValues = new List<OBJJointAndWeight>();
    }
}
