// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;

namespace OBJRuntime.DataTypes
{
    public class OBJTag
    {
        public string Name;
        public List<int> IntValues = new List<int>();
        public List<float> FloatValues = new List<float>();
        public List<string> StringValues = new List<string>();
    }
}
