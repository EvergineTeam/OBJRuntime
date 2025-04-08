//// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

//using System;
//using System.Collections.Generic;
//using System.IO;
//using OBJRuntime.DataTypes;
//using OBJRuntime.Readers;

//namespace TinyObj
//{
//    public class ObjReader
//    {
//        private bool _valid;
//        private Attrib _attrib = new Attrib();
//        private List<Shape> _shapes = new List<Shape>();
//        private List<MaterialInfo> _materials = new List<MaterialInfo>();
//        private string _warning = "";
//        private string _error = "";

//        public bool Valid { get { return _valid; } }
//        public Attrib Attrib { get { return _attrib; } }
//        public List<Shape> Shapes { get { return _shapes; } }
//        public List<MaterialInfo> Materials { get { return _materials; } }
//        public string Warning { get { return _warning; } }
//        public string Error { get { return _error; } }

//        public bool ParseFromFile(string filename, ObjReaderConfig config)
//        {
//            // figure out base dir for searching .mtl
//            string baseDir = config.MtlSearchPath;
//            if (string.IsNullOrEmpty(baseDir))
//            {
//                var dir = Path.GetDirectoryName(filename);
//                if (!string.IsNullOrEmpty(dir))
//                {
//                    baseDir = dir;
//                }
//            }
//            MaterialFileReader mtlReader = new MaterialFileReader(baseDir);

//            try
//            {
//                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
//                using (var sr = new StreamReader(fs))
//                {
//                    _valid = ObjLoader.LoadObj(
//                        sr,
//                        ref _attrib,
//                        _shapes,
//                        _materials,
//                        ref _warning,
//                        ref _error,
//                        mtlReader,
//                        config.Triangulate,
//                        config.VertexColor);
//                }
//            }
//            catch (Exception ex)
//            {
//                _error += $"Cannot open file [{filename}]. Exception: {ex.Message}\n";
//                _valid = false;
//            }

//            return _valid;
//        }

//        public bool ParseFromString(string objText, string mtlText, ObjReaderConfig config)
//        {
//            _attrib = new Attrib();
//            _shapes = new List<Shape>();
//            _materials = new List<MaterialInfo>();
//            _warning = "";
//            _error = "";

//            using (var msObj = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(objText)))
//            using (var srObj = new StreamReader(msObj))
//            using (var msMtl = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(mtlText)))
//            using (var srMtl = new StreamReader(msMtl))
//            {
//                var mtlReader = new MaterialStreamReader(msMtl);
//                _valid = ObjLoader.LoadObj(
//                    srObj,
//                    ref _attrib,
//                    _shapes,
//                    _materials,
//                    ref _warning,
//                    ref _error,
//                    mtlReader,
//                    config.Triangulate,
//                    config.VertexColor);
//            }

//            return _valid;
//        }
//    }
//}
