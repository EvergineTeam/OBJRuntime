// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace OBJRuntime.DataTypes
{
    public class OBJShape
    {
        public string Name;
        public OBJMesh Mesh = new OBJMesh();
        public OBJLines Lines = new OBJLines();
        public OBJPoints Points = new OBJPoints();
    }
}
