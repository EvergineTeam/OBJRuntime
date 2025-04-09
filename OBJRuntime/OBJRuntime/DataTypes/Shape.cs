// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace OBJRuntime.DataTypes
{
    public class Shape
    {
        public string Name;
        public Mesh Mesh = new Mesh();
        public Lines Lines = new Lines();
        public Points Points = new Points();
    }
}
