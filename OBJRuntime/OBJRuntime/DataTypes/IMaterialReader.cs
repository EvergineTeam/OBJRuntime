// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;

namespace OBJRuntime.DataTypes
{
    public interface IMaterialReader
    {
        bool Read(
            string matId,
            List<MaterialInfo> materials,
            Dictionary<string, int> matMap,
            out string warning,
            out string error);
    }
}
