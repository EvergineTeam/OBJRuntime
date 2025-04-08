//// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

//using OBJRuntime.Readers;
//using System;
//using System.Collections.Generic;
//using System.IO;

//namespace OBJRuntime.DataTypes
//{
//    /// <summary>
//    /// Reads MTL from a file on disk.
//    /// You can supply a base directory in the constructor.
//    /// </summary>
//    public class MaterialFileReader : IMaterialReader
//    {
//        private readonly string _mtlBaseDir;

//        public MaterialFileReader(string mtlBasedir)
//        {
//            _mtlBaseDir = mtlBasedir ?? "";
//        }

//        private static string JoinPath(string dir, string filename)
//        {
//            if (string.IsNullOrEmpty(dir))
//            {
//                return filename;
//            }
//            char dirsep = Path.DirectorySeparatorChar;
//            if (!dir.EndsWith(dirsep.ToString()))
//            {
//                return dir + dirsep + filename;
//            }
//            else
//            {
//                return dir + filename;
//            }
//        }

//        public bool Read(
//            string matId,
//            List<MaterialInfo> materials,
//            Dictionary<string, int> matMap,
//            out string warning,
//            out string error)
//        {
//            warning = "";
//            error = "";

//            // support multiple possible base dirs separated by ':' or ';'
//            // per OS differences. For simplicity, let's just try a single path:
//            char[] separators = new char[] { ';', ':' };
//            var baseDirs = _mtlBaseDir.Split(separators, StringSplitOptions.RemoveEmptyEntries);

//            if (baseDirs.Length == 0)
//            {
//                // fallback: single attempt with _mtlBaseDir
//                baseDirs = new string[] { _mtlBaseDir };
//            }

//            bool found = false;
//            foreach (var bd in baseDirs)
//            {
//                var filepath = JoinPath(bd, matId);
//                if (File.Exists(filepath))
//                {
//                    found = true;
//                    try
//                    {
//                        using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
//                        using (var sr = new StreamReader(fs))
//                        {
//                            MtlLoader.Load(sr, materials, matMap, ref warning, ref error);
//                            return true;
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        error += "Exception while reading material file: " + ex.Message + "\n";
//                        return false;
//                    }
//                }
//            }

//            if (!found)
//            {
//                warning += $"Material file [{matId}] not found in path: {_mtlBaseDir}\n";
//            }
//            return false;
//        }
//    }
//}
