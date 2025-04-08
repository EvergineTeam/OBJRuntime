using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace TinyObj
{
    #region Data Types (Enums, Structs)

    public enum TextureType
    {
        None,
        Sphere,
        CubeTop,
        CubeBottom,
        CubeFront,
        CubeBack,
        CubeLeft,
        CubeRight
    }

    public class TextureOption
    {
        public TextureType Type = TextureType.None;
        public float Sharpness = 1.0f;
        public float Brightness = 0.0f;
        public float Contrast = 1.0f;
        public float[] OriginOffset = new float[3] { 0, 0, 0 };
        public float[] Scale = new float[3] { 1, 1, 1 };
        public float[] Turbulence = new float[3] { 0, 0, 0 };
        public int TextureResolution = -1;
        public bool Clamp = false;
        public char Imfchan = 'm';  // default to 'm' (for decal)
        public bool Blendu = true;
        public bool Blendv = true;
        public float BumpMultiplier = 1.0f;
        public string Colorspace = ""; // e.g. "sRGB" or "linear"
    }

    /// <summary>
    /// TinyObj material definition
    /// </summary>
    public class Material
    {
        public string Name = "";

        public float[] Ambient = new float[3] { 0, 0, 0 };
        public float[] Diffuse = new float[3] { 0, 0, 0 };
        public float[] Specular = new float[3] { 0, 0, 0 };
        public float[] Transmittance = new float[3] { 0, 0, 0 };
        public float[] Emission = new float[3] { 0, 0, 0 };
        public float Shininess = 1.0f;
        public float Ior = 1.0f;        // Index of refraction
        public float Dissolve = 1.0f;   // 1=opaque; 0=fully transparent
        public int Illum = 0;          // Illumination model

        // Texture information
        public string AmbientTexname = "";    // map_Ka
        public string DiffuseTexname = "";    // map_Kd
        public string SpecularTexname = "";   // map_Ks
        public string SpecularHighlightTexname = "";  // map_Ns
        public string BumpTexname = "";               // map_bump
        public string DisplacementTexname = "";        // disp
        public string AlphaTexname = "";              // map_d
        public string ReflectionTexname = "";         // refl

        public TextureOption AmbientTexopt = new TextureOption();
        public TextureOption DiffuseTexopt = new TextureOption();
        public TextureOption SpecularTexopt = new TextureOption();
        public TextureOption SpecularHighlightTexopt = new TextureOption();
        public TextureOption BumpTexopt = new TextureOption();
        public TextureOption DisplacementTexopt = new TextureOption();
        public TextureOption AlphaTexopt = new TextureOption();
        public TextureOption ReflectionTexopt = new TextureOption();

        // PBR extension
        public float Roughness = 0.0f;           // map_Pr
        public float Metallic = 0.0f;            // map_Pm
        public float Sheen = 0.0f;               // map_Ps
        public float ClearcoatThickness = 0.0f;  // Pc
        public float ClearcoatRoughness = 0.0f;  // Pcr
        public float Anisotropy = 0.0f;          // aniso
        public float AnisotropyRotation = 0.0f;  // anisor

        public string RoughnessTexname = "";  // map_Pr
        public string MetallicTexname = "";   // map_Pm
        public string SheenTexname = "";      // map_Ps
        public string EmissiveTexname = "";   // map_Ke
        public string NormalTexname = "";     // norm

        public TextureOption roughness_texopt;
        public TextureOption metallic_texopt;
        public TextureOption sheen_texopt;
        public TextureOption emissive_texopt;
        public TextureOption normal_texopt;

        public int pad2;

        // Key-value pairs for unknown parameters.
        public Dictionary<string, string> UnknownParameter = new Dictionary<string, string>();
    }

    public class Tag
    {
        public string Name;
        public List<int> IntValues = new List<int>();
        public List<float> FloatValues = new List<float>();
        public List<string> StringValues = new List<string>();
    }

    public struct JointAndWeight
    {
        public int JointId;
        public float Weight;
    }

    public class SkinWeight
    {
        // Index in the "attrib_t.vertices" array
        public int VertexId;
        public List<JointAndWeight> WeightValues = new List<JointAndWeight>();
    }

    /// <summary>
    /// Index struct to support different indices for vtx/normal/texcoord.
    /// -1 means "not used".
    /// </summary>
    public struct Index
    {
        public int VertexIndex;
        public int NormalIndex;
        public int TexcoordIndex;
    }

    public class Mesh
    {
        public List<Index> Indices = new List<Index>();
        public List<uint> NumFaceVertices = new List<uint>();   // number of vertices per face
        public List<int> MaterialIds = new List<int>();         // per-face material ID
        public List<uint> SmoothingGroupIds = new List<uint>(); // per-face smoothing group ID
        public List<Tag> Tags = new List<Tag>();
    }

    public class Lines
    {
        public List<Index> Indices = new List<Index>();
        public List<int> NumLineVertices = new List<int>();
    }

    public class Points
    {
        public List<Index> Indices = new List<Index>();
    }

    public class Shape
    {
        public string Name;
        public Mesh Mesh = new Mesh();
        public Lines Lines = new Lines();
        public Points Points = new Points();
    }

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

    public class ObjReaderConfig
    {
        // Triangulate polygons?
        public bool Triangulate = true;
        // "simple" or "earcut" if you had more advanced usage. 
        // We'll keep only "simple" ear clipping approach in this code.
        public string TriangulationMethod = "simple";
        // Parse vertex colors? If no colors are present, they remain empty unless fallback is set.
        public bool VertexColor = true;
        // Where to search for .mtl
        public string MtlSearchPath = "";
    }

    public interface IMaterialReader
    {
        bool Read(
            string matId,
            List<Material> materials,
            Dictionary<string, int> matMap,
            out string warning,
            out string error);
    }

    /// <summary>
    /// Reads MTL from a file on disk.
    /// You can supply a base directory in the constructor.
    /// </summary>
    public class MaterialFileReader : IMaterialReader
    {
        private readonly string _mtlBaseDir;

        public MaterialFileReader(string mtlBasedir)
        {
            _mtlBaseDir = mtlBasedir ?? "";
        }

        private static string JoinPath(string dir, string filename)
        {
            if (string.IsNullOrEmpty(dir))
            {
                return filename;
            }
            char dirsep = Path.DirectorySeparatorChar;
            if (!dir.EndsWith(dirsep.ToString()))
            {
                return dir + dirsep + filename;
            }
            else
            {
                return dir + filename;
            }
        }

        public bool Read(
            string matId,
            List<Material> materials,
            Dictionary<string, int> matMap,
            out string warning,
            out string error)
        {
            warning = "";
            error = "";

            // support multiple possible base dirs separated by ':' or ';'
            // per OS differences. For simplicity, let's just try a single path:
            char[] separators = new char[] { ';', ':' };
            var baseDirs = _mtlBaseDir.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            if (baseDirs.Length == 0)
            {
                // fallback: single attempt with _mtlBaseDir
                baseDirs = new string[] { _mtlBaseDir };
            }

            bool found = false;
            foreach (var bd in baseDirs)
            {
                var filepath = JoinPath(bd, matId);
                if (File.Exists(filepath))
                {
                    found = true;
                    try
                    {
                        using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                        using (var sr = new StreamReader(fs))
                        {
                            MtlLoader.Load(sr, materials, matMap, ref warning, ref error);
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        error += "Exception while reading material file: " + ex.Message + "\n";
                        return false;
                    }
                }
            }

            if (!found)
            {
                warning += $"Material file [{matId}] not found in path: {_mtlBaseDir}\n";
            }
            return false;
        }
    }

    /// <summary>
    /// Reads MTL data from a Stream (like a MemoryStream).
    /// </summary>
    public class MaterialStreamReader : IMaterialReader
    {
        private readonly Stream _inStream;

        public MaterialStreamReader(Stream inStream)
        {
            _inStream = inStream;
        }

        public bool Read(
            string matId,
            List<Material> materials,
            Dictionary<string, int> matMap,
            out string warning,
            out string error)
        {
            warning = "";
            error = "";

            if (_inStream == null)
            {
                warning += "Material stream in error state.\n";
                return false;
            }

            using (var sr = new StreamReader(_inStream, leaveOpen: true))
            {
                MtlLoader.Load(sr, materials, matMap, ref warning, ref error);
            }

            return true;
        }
    }

    #endregion

    #region MtlLoader (parsing .mtl)

    /// <summary>
    /// Helper class for loading .mtl text into a List&lt;Material&gt;.
    /// </summary>
    public static class MtlLoader
    {
        // Specialized read that tries to parse float from a substring.
        private static bool TryParseFloat(string s, out float result)
        {
            // Try with CultureInfo.InvariantCulture
            return float.TryParse(
                s,
                NumberStyles.Float | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out result);
        }

        // Similar to std::getline but in C#. We can just read line by line.
        // Already have StreamReader.ReadLine() so it's simpler.
        // (We just keep a naming consistency.)
        public static string SafeGetLine(StreamReader sr)
        {
            return sr.ReadLine();
        }

        // The main function to load .mtl data:
        public static void Load(
            StreamReader sr,
            List<Material> materials,
            Dictionary<string, int> materialMap,
            ref string warning,
            ref string error)
        {
            // If there's no "newmtl" at all, we still push a default material at the end.
            Material material = new Material();
            bool firstMaterial = true;

            bool hasD = false;
            bool hasTr = false;
            bool hasKd = false; // to set a default Kd if we see map_Kd w/o Kd

            int lineNo = 0;

            while (!sr.EndOfStream)
            {
                lineNo++;
                string line = SafeGetLine(sr);
                if (line == null) break;

                line = line.Trim();
                if (line.Length < 1) continue;  // skip blank lines
                if (line.StartsWith("#")) continue; // skip comments

                var tokens = Tokenize(line);
                if (tokens.Count == 0) continue;

                string key = tokens[0];
                if (key == "newmtl" && tokens.Count > 1)
                {
                    // push old material if it has a name
                    if (!firstMaterial || !string.IsNullOrEmpty(material.Name))
                    {
                        // store
                        if (!materialMap.ContainsKey(material.Name))
                            materialMap.Add(material.Name, materials.Count);
                        materials.Add(material);
                    }
                    // reset
                    material = new Material();
                    hasD = false;
                    hasTr = false;
                    hasKd = false;
                    firstMaterial = false;

                    material.Name = line.Substring(6).Trim(); // or tokens[1..end]
                }
                else if ((key == "Ka" || key == "ka") && tokens.Count >= 4)
                {
                    ParseReal3(tokens, 1, material.Ambient);
                }
                else if ((key == "Kd" || key == "kd") && tokens.Count >= 4)
                {
                    ParseReal3(tokens, 1, material.Diffuse);
                    hasKd = true;
                }
                else if ((key == "Ks" || key == "ks") && tokens.Count >= 4)
                {
                    ParseReal3(tokens, 1, material.Specular);
                }
                else if (key == "Ke" && tokens.Count >= 4)
                {
                    ParseReal3(tokens, 1, material.Emission);
                }
                else if ((key == "Tf" || key == "Kt") && tokens.Count >= 4)
                {
                    ParseReal3(tokens, 1, material.Transmittance);
                }
                else if (key == "Ns" && tokens.Count >= 2)
                {
                    if (TryParseFloat(tokens[1], out float val))
                        material.Shininess = val;
                }
                else if (key == "Ni" && tokens.Count >= 2)
                {
                    if (TryParseFloat(tokens[1], out float val))
                        material.Ior = val;
                }
                else if (key == "illum" && tokens.Count >= 2)
                {
                    if (int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int ival))
                    {
                        material.Illum = ival;
                    }
                }
                else if (key == "d" && tokens.Count >= 2)
                {
                    if (TryParseFloat(tokens[1], out float val))
                        material.Dissolve = val;
                    if (hasTr)
                    {
                        warning += $"Both 'd' and 'Tr' found for material '{material.Name}'. Using 'd' (line {lineNo}).\n";
                    }
                    hasD = true;
                }
                else if (key == "Tr" && tokens.Count >= 2)
                {
                    if (hasD)
                    {
                        // ignore Tr
                        warning += $"Both 'd' and 'Tr' found for material '{material.Name}'. Using 'd' (line {lineNo}).\n";
                    }
                    else
                    {
                        // invert
                        if (TryParseFloat(tokens[1], out float val))
                            material.Dissolve = 1.0f - val;
                    }
                    hasTr = true;
                }
                else if (key == "map_Ka")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), material.AmbientTexname, material.AmbientTexopt, "Ka");
                }
                else if (key == "map_Kd")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), material.DiffuseTexname, material.DiffuseTexopt, "Kd");
                    if (!hasKd)
                    {
                        // set a default
                        material.Diffuse[0] = 0.6f;
                        material.Diffuse[1] = 0.6f;
                        material.Diffuse[2] = 0.6f;
                    }
                }
                else if (key == "map_Ks")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), material.SpecularTexname, material.SpecularTexopt, "Ks");
                }
                else if (key == "map_Ns")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), material.SpecularHighlightTexname, material.SpecularHighlightTexopt, "Ns");
                }
                else if (key == "map_d")
                {
                    ParseTextureAndOption(line.Substring(5).Trim(), material.AlphaTexname, material.AlphaTexopt, "d");
                }
                else if (key == "map_bump" || key == "map_Bump")
                {
                    ParseTextureAndOption(line.Substring(key.Length).Trim(), material.BumpTexname, material.BumpTexopt, "bump");
                }
                else if (key == "bump")
                {
                    ParseTextureAndOption(line.Substring(4).Trim(), material.BumpTexname, material.BumpTexopt, "bump");
                }
                else if (key == "map_disp" || key == "map_Disp" || key == "disp")
                {
                    int skipLen = (key == "disp") ? 4 : 8;
                    ParseTextureAndOption(line.Substring(skipLen).Trim(), material.DisplacementTexname, material.DisplacementTexopt, "disp");
                }
                else if (key == "refl")
                {
                    ParseTextureAndOption(line.Substring(4).Trim(), material.ReflectionTexname, material.ReflectionTexopt, "refl");
                }
                else if (key == "map_Pr")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), material.RoughnessTexname, material.roughness_texopt, "Pr");
                }
                else if (key == "map_Pm")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), material.MetallicTexname, material.metallic_texopt, "Pm");
                }
                else if (key == "map_Ps")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), material.SheenTexname, material.sheen_texopt, "Ps");
                }
                else if (key == "map_Ke")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), material.EmissiveTexname, material.emissive_texopt, "Ke");
                }
                else if (key == "norm")
                {
                    ParseTextureAndOption(line.Substring(4).Trim(), material.NormalTexname, material.normal_texopt, "norm");
                }
                else if (key == "Pr")
                {
                    if (TryParseFloat(tokens[1], out float val))
                        material.Roughness = val;
                }
                else if (key == "Pm")
                {
                    if (TryParseFloat(tokens[1], out float val))
                        material.Metallic = val;
                }
                else if (key == "Ps")
                {
                    if (TryParseFloat(tokens[1], out float val))
                        material.Sheen = val;
                }
                else if (key == "Pc")
                {
                    if (TryParseFloat(tokens[1], out float val))
                        material.ClearcoatThickness = val;
                }
                else if (key == "Pcr")
                {
                    if (TryParseFloat(tokens[1], out float val))
                        material.ClearcoatRoughness = val;
                }
                else if (key == "aniso")
                {
                    if (TryParseFloat(tokens[1], out float val))
                        material.Anisotropy = val;
                }
                else if (key == "anisor")
                {
                    if (TryParseFloat(tokens[1], out float val))
                        material.AnisotropyRotation = val;
                }
                else
                {
                    // unknown, store in map
                    if (tokens.Count >= 2)
                    {
                        string paramV = line.Substring(key.Length).Trim();
                        material.UnknownParameter[key] = paramV;
                    }
                }
            }

            // push last material
            if (!materialMap.ContainsKey(material.Name))
                materialMap.Add(material.Name, materials.Count);
            materials.Add(material);
        }

        private static void ParseTextureAndOption(string line, string texName, TextureOption texOpt, string prefix)
        {
            // We parse the line for texture name and possible sub‐options like -o, -s, etc.
            // This can be somewhat simplified, but let's keep a structure close to the original.
            // For a more robust approach, you can do additional tokenization of the entire line.

            // We do a naive split for demonstration, real code might do more refined parse.
            var tokens = Tokenize(line);

            // We keep a local pointer to the final texture name we actually set in 'texName'.
            // Because in C# strings are passed by value, we might set it after we find the main token.
            // We'll parse each piece in tokens:
            bool foundTexName = false;
            string foundName = "";

            int idx = 0;
            while (idx < tokens.Count)
            {
                var t = tokens[idx];
                if (t.StartsWith("-blendu"))
                {
                    // e.g. "-blendu on" or "off"
                    idx++;
                    if (idx < tokens.Count)
                    {
                        texOpt.Blendu = (tokens[idx] == "on");
                    }
                }
                else if (t.StartsWith("-blendv"))
                {
                    idx++;
                    if (idx < tokens.Count)
                    {
                        texOpt.Blendv = (tokens[idx] == "on");
                    }
                }
                else if (t.StartsWith("-clamp"))
                {
                    idx++;
                    if (idx < tokens.Count)
                    {
                        texOpt.Clamp = (tokens[idx] == "on");
                    }
                }
                else if (t.StartsWith("-boost"))
                {
                    idx++;
                    if (idx < tokens.Count && TryParseFloat(tokens[idx], out float val)) texOpt.Sharpness = val;
                }
                else if (t.StartsWith("-bm"))
                {
                    idx++;
                    if (idx < tokens.Count && TryParseFloat(tokens[idx], out float val)) texOpt.BumpMultiplier = val;
                }
                else if (t.StartsWith("-o"))
                {
                    // e.g. -o u [v [w]]
                    // parse up to 3 floats
                    int maxCoords = 3;
                    int coordCount = 0;
                    idx++;
                    while (coordCount < maxCoords && idx < tokens.Count && TryParseFloat(tokens[idx], out float oval))
                    {
                        texOpt.OriginOffset[coordCount] = oval;
                        coordCount++; idx++;
                    }
                    // no stepping back of idx
                    continue;
                }
                else if (t.StartsWith("-s"))
                {
                    // e.g. -s u [v [w]]
                    int maxCoords = 3;
                    int coordCount = 0;
                    idx++;
                    while (coordCount < maxCoords && idx < tokens.Count && TryParseFloat(tokens[idx], out float sval))
                    {
                        texOpt.Scale[coordCount] = sval;
                        coordCount++; idx++;
                    }
                    continue;
                }
                else if (t.StartsWith("-t"))
                {
                    // e.g. -t u [v [w]]
                    int maxCoords = 3;
                    int coordCount = 0;
                    idx++;
                    while (coordCount < maxCoords && idx < tokens.Count && TryParseFloat(tokens[idx], out float tval))
                    {
                        texOpt.Turbulence[coordCount] = tval;
                        coordCount++; idx++;
                    }
                    continue;
                }
                else if (t.StartsWith("-texres"))
                {
                    idx++;
                    if (idx < tokens.Count && int.TryParse(tokens[idx], out int texres))
                    {
                        texOpt.TextureResolution = texres;
                    }
                }
                else if (t.StartsWith("-imfchan"))
                {
                    idx++;
                    if (idx < tokens.Count && tokens[idx].Length >= 1)
                    {
                        texOpt.Imfchan = tokens[idx][0];
                    }
                }
                else if (t.StartsWith("-mm"))
                {
                    // e.g. -mm baseValue gainValue
                    // parse 2 floats
                    idx++;
                    if (idx < tokens.Count && TryParseFloat(tokens[idx], out float bval))
                    {
                        texOpt.Brightness = bval;
                        idx++;
                        if (idx < tokens.Count && TryParseFloat(tokens[idx], out float cval))
                        {
                            texOpt.Contrast = cval;
                            idx++;
                        }
                    }
                    continue;
                }
                else if (t.StartsWith("-colorspace"))
                {
                    idx++;
                    if (idx < tokens.Count)
                    {
                        texOpt.Colorspace = tokens[idx];
                    }
                }
                else
                {
                    // We treat this as the texture filename
                    foundName = t;
                    foundTexName = true;
                    idx++;
                    // We'll assume the rest of the tokens might be extraneous or for advanced usage.
                    // In practice, you might continue to parse for additional parameters.
                }
                idx++;
            }

            if (foundTexName && !string.IsNullOrEmpty(foundName))
            {
                texName = foundName;
            }
        }

        private static void ParseReal3(List<string> tokens, int startIndex, float[] arr)
        {
            // tokens: e.g. ["Kd", "0.1", "0.2", "0.3"]
            // parse from tokens[startIndex] up to 3.
            for (int i = 0; i < 3; i++)
            {
                if ((startIndex + i) < tokens.Count)
                {
                    if (TryParseFloat(tokens[startIndex + i], out float val))
                    {
                        arr[i] = val;
                    }
                }
            }
        }

        private static List<string> Tokenize(string line)
        {
            var tokens = new List<string>();
            var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                tokens.Add(p.Trim());
            }
            return tokens;
        }
    }

    #endregion

    #region Core OBJ Loader

    public class ObjReader
    {
        private bool _valid;
        private Attrib _attrib = new Attrib();
        private List<Shape> _shapes = new List<Shape>();
        private List<Material> _materials = new List<Material>();
        private string _warning = "";
        private string _error = "";

        public bool Valid { get { return _valid; } }
        public Attrib Attrib { get { return _attrib; } }
        public List<Shape> Shapes { get { return _shapes; } }
        public List<Material> Materials { get { return _materials; } }
        public string Warning { get { return _warning; } }
        public string Error { get { return _error; } }

        public bool ParseFromFile(string filename, ObjReaderConfig config)
        {
            // figure out base dir for searching .mtl
            string baseDir = config.MtlSearchPath;
            if (string.IsNullOrEmpty(baseDir))
            {
                var dir = Path.GetDirectoryName(filename);
                if (!string.IsNullOrEmpty(dir))
                {
                    baseDir = dir;
                }
            }
            MaterialFileReader mtlReader = new MaterialFileReader(baseDir);

            try
            {
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                using (var sr = new StreamReader(fs))
                {
                    _valid = ObjLoader.LoadObj(
                        sr,
                        ref _attrib,
                        _shapes,
                        _materials,
                        ref _warning,
                        ref _error,
                        mtlReader,
                        config.Triangulate,
                        config.VertexColor);
                }
            }
            catch (Exception ex)
            {
                _error += $"Cannot open file [{filename}]. Exception: {ex.Message}\n";
                _valid = false;
            }

            return _valid;
        }

        public bool ParseFromString(string objText, string mtlText, ObjReaderConfig config)
        {
            _attrib = new Attrib();
            _shapes = new List<Shape>();
            _materials = new List<Material>();
            _warning = "";
            _error = "";

            using (var msObj = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(objText)))
            using (var srObj = new StreamReader(msObj))
            using (var msMtl = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(mtlText)))
            using (var srMtl = new StreamReader(msMtl))
            {
                var mtlReader = new MaterialStreamReader(msMtl);
                _valid = ObjLoader.LoadObj(
                    srObj,
                    ref _attrib,
                    _shapes,
                    _materials,
                    ref _warning,
                    ref _error,
                    mtlReader,
                    config.Triangulate,
                    config.VertexColor);
            }

            return _valid;
        }
    }

    /// <summary>
    /// Main OBJ loading logic in a static class, adapted from your code snippet.
    /// </summary>
    public static class ObjLoader
    {
        // Helper method: Splits a string into tokens, but doesn't handle advanced escaping rules.
        private static List<string> Tokenize(string line)
        {
            var tokens = new List<string>();
            var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                tokens.Add(p.Trim());
            }
            return tokens;
        }

        // We do a line read. .NET has StreamReader.ReadLine already, so we won't replicate safeGetline exactly.
        private static string SafeGetLine(StreamReader sr)
        {
            return sr.ReadLine();
        }

        public static bool LoadObj(
            StreamReader inStream,
            ref Attrib attrib,
            List<Shape> shapes,
            List<Material> materials,
            ref string warning,
            ref string error,
            IMaterialReader readMatFn,
            bool triangulate,
            bool defaultVcolsFallback)
        {
            // We'll parse the entire .obj text from the provided stream.
            // This is roughly a direct line-by-line translation from the original.
            // We skip some advanced parts like TINYOBJLOADER_USE_MAPBOX_EARCUT for brevity.

            attrib.Vertices.Clear();
            attrib.VertexWeights.Clear();
            attrib.Normals.Clear();
            attrib.Texcoords.Clear();
            attrib.TexcoordWs.Clear();
            attrib.Colors.Clear();
            attrib.SkinWeights.Clear();
            shapes.Clear();

            var v = new List<float>();
            var vertexWeights = new List<float>();  // for the optional w in 'v' lines
            var vn = new List<float>();
            var vt = new List<float>();
            var vc = new List<float>();  // optional vertex colors
            var vw = new List<SkinWeight>(); // extension: vertex skin weights

            int materialId = -1;
            uint currentSmoothingId = 0;

            bool foundAllColors = true;

            // We'll accumulate face/lines/points in a "PrimGroup" then flush to shape when
            // we see group or object changes. 
            PrimGroup primGroup = new PrimGroup();
            string currentGroupName = "";

            // For storing materials that we've loaded from .mtl
            var materialMap = new Dictionary<string, int>();
            var materialFilenames = new HashSet<string>();

            int lineNo = 0;
            while (!inStream.EndOfStream)
            {
                lineNo++;
                string line = SafeGetLine(inStream);
                if (line == null) break;
                line = line.TrimEnd(); // remove trailing spaces
                if (line.Length < 1) continue;

                // skip leading spaces
                if (line.StartsWith("#")) continue; // comment

                // parse tokens
                var tokens = Tokenize(line);
                if (tokens.Count < 1) continue;
                var cmd = tokens[0];

                if (cmd == "v")
                {
                    // vertex
                    if (tokens.Count >= 4)
                    {
                        float x = 0, y = 0, z = 0;
                        float r = 1, g = 1, b = 1; // either color or 'w'

                        // parse x,y,z
                        TryParseFloat(tokens[1], out x);
                        TryParseFloat(tokens[2], out y);
                        TryParseFloat(tokens[3], out z);

                        // if we have 4 tokens => might be w or color
                        // if we have 7 tokens => x y z r g b
                        int count = tokens.Count - 1; // ignoring the "v"
                        if (count == 4)
                        {
                            // interpret the 4th as 'w'
                            TryParseFloat(tokens[4], out r);
                            // store w into vertexWeights
                            v.Add(x); v.Add(y); v.Add(z);
                            vertexWeights.Add(r);
                            foundAllColors = false;
                        }
                        else if (count >= 7)
                        {
                            // x y z r g b ...
                            TryParseFloat(tokens[4], out r);
                            TryParseFloat(tokens[5], out g);
                            TryParseFloat(tokens[6], out b);

                            v.Add(x); v.Add(y); v.Add(z);
                            vertexWeights.Add(1.0f); // default w=1
                            vc.Add(r); vc.Add(g); vc.Add(b);

                        }
                        else
                        {
                            // just x,y,z
                            v.Add(x); v.Add(y); v.Add(z);
                            vertexWeights.Add(1.0f);
                            foundAllColors = false;
                        }
                    }
                }
                else if (cmd == "vn")
                {
                    if (tokens.Count >= 4)
                    {
                        float x = 0, y = 0, z = 0;
                        TryParseFloat(tokens[1], out x);
                        TryParseFloat(tokens[2], out y);
                        TryParseFloat(tokens[3], out z);
                        vn.Add(x); vn.Add(y); vn.Add(z);
                    }
                }
                else if (cmd == "vt")
                {
                    // vt u v [w]
                    if (tokens.Count >= 2)
                    {
                        float u = 0, vv = 0, w = 0;
                        TryParseFloat(tokens[1], out u);
                        if (tokens.Count > 2) TryParseFloat(tokens[2], out vv);
                        if (tokens.Count > 3) TryParseFloat(tokens[3], out w);
                        vt.Add(u); vt.Add(vv);
                        // we won't store w in vt directly, but we can store it in TexcoordWs if needed.
                    }
                }
                else if (cmd == "vw")
                {
                    // extension for vertex weights
                    // e.g. "vw 0 0 0.25 1 0.25 2 0.5"
                    // first token is "vw", second is vertex id:
                    if (tokens.Count > 1)
                    {
                        SkinWeight sw = new SkinWeight();
                        int vid = 0;
                        if (int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out vid))
                        {
                            sw.VertexId = vid;
                            int idx = 2;
                            while (idx + 1 < tokens.Count)
                            {
                                int j = 0;
                                float w = 0.0f;
                                if (!int.TryParse(tokens[idx], out j)) break;
                                if (!TryParseFloat(tokens[idx + 1], out w)) break;
                                JointAndWeight jw = new JointAndWeight();
                                jw.JointId = j;
                                jw.Weight = w;
                                sw.WeightValues.Add(jw);
                                idx += 2;
                            }
                            vw.Add(sw);
                        }
                    }
                }
                else if (cmd == "f")
                {
                    // face
                    if (tokens.Count < 2) continue;

                    Face f = new Face() { SmoothingGroupId = currentSmoothingId };
                    // parse
                    for (int i = 1; i < tokens.Count; i++)
                    {
                        var vix = ParseRawTriple(tokens[i]);
                        f.VertexIndices.Add(vix);
                    }

                    primGroup.FaceGroup.Add(f);
                }
                else if (cmd == "l")
                {
                    // line
                    if (tokens.Count < 2) continue;
                    var lineGroup = new LineElm();
                    for (int i = 1; i < tokens.Count; i++)
                    {
                        var vix = ParseRawTriple(tokens[i]);
                        lineGroup.VertexIndices.Add(vix);
                    }
                    primGroup.LineGroup.Add(lineGroup);
                }
                else if (cmd == "p")
                {
                    // points
                    if (tokens.Count < 2) continue;
                    var pointGroup = new PointsElm();
                    for (int i = 1; i < tokens.Count; i++)
                    {
                        var vix = ParseRawTriple(tokens[i]);
                        pointGroup.VertexIndices.Add(vix);
                    }
                    primGroup.PointsGroup.Add(pointGroup);
                }
                else if (cmd == "usemtl")
                {
                    // flush the current primGroup into a shape
                    ExportGroupsToShape(
                        shapes,
                        ref primGroup,
                        currentGroupName,
                        materialId,
                        v,
                        triangulate,
                        ref warning);

                    primGroup.Clear();
                    // read the next material name
                    string matName = (tokens.Count > 1) ? tokens[1] : "";
                    if (!materialMap.TryGetValue(matName, out materialId))
                    {
                        // not found
                        materialId = -1;
                        warning += $"material [{matName}] not found.\n";
                    }
                }
                else if (cmd == "mtllib")
                {
                    if (readMatFn != null && tokens.Count > 1)
                    {
                        // We can have multiple filenames in the line: "mtllib file1 file2"
                        // We'll parse them all.
                        for (int i = 1; i < tokens.Count; i++)
                        {
                            string filename = tokens[i];
                            if (materialFilenames.Contains(filename)) continue; // skip repeated
                            if (!readMatFn.Read(filename, materials, materialMap, out string warnMtl, out string errMtl))
                            {
                                // failed
                                warning += warnMtl;
                                error += errMtl;
                            }
                            else
                            {
                                warning += warnMtl;
                            }
                            materialFilenames.Add(filename);
                        }
                    }
                }
                else if (cmd == "g")
                {
                    // flush current group -> shape
                    ExportGroupsToShape(
                        shapes,
                        ref primGroup,
                        currentGroupName,
                        materialId,
                        v,
                        triangulate,
                        ref warning);

                    if (primGroup.HasData())
                    {
                        // create a new shape if there's leftover
                        // but typically ExportGroupsToShape() empties it if it is valid
                    }

                    // parse the new group name
                    if (tokens.Count > 1)
                    {
                        // we combine them if multiple
                        currentGroupName = "";
                        for (int i = 1; i < tokens.Count; i++)
                        {
                            if (i > 1) currentGroupName += " ";
                            currentGroupName += tokens[i];
                        }
                    }
                    else
                    {
                        currentGroupName = "";
                    }
                }
                else if (cmd == "o")
                {
                    // flush
                    ExportGroupsToShape(
                        shapes,
                        ref primGroup,
                        currentGroupName,
                        materialId,
                        v,
                        triangulate,
                        ref warning);

                    // new shape
                    if (tokens.Count > 1)
                    {
                        currentGroupName = line.Substring(1).Trim();
                    }
                    else
                    {
                        currentGroupName = "";
                    }
                }
                else if (cmd == "s")
                {
                    // smoothing group
                    if (tokens.Count > 1)
                    {
                        if (tokens[1] == "off") currentSmoothingId = 0;
                        else
                        {
                            if (!uint.TryParse(tokens[1], out currentSmoothingId))
                            {
                                currentSmoothingId = 0;
                            }
                        }
                    }
                }
                // else unknown
            }

            // flush last primGroup
            ExportGroupsToShape(
                shapes,
                ref primGroup,
                currentGroupName,
                materialId,
                v,
                triangulate,
                ref warning);

            // Store them in the final Attrib
            if (!foundAllColors && !defaultVcolsFallback)
            {
                vc.Clear();
            }

            attrib.Vertices.AddRange(v);
            attrib.VertexWeights.AddRange(vertexWeights);
            attrib.Normals.AddRange(vn);
            attrib.Texcoords.AddRange(vt);
            attrib.TexcoordWs.AddRange(new float[vt.Count]); // not used currently
            attrib.Colors.AddRange(vc);
            attrib.SkinWeights.AddRange(vw);

            return true;
        }

        private static bool TryParseFloat(string s, out float val)
        {
            return float.TryParse(
                s,
                NumberStyles.Float | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out val);
        }

        // Raw triple parse: i, i/j, i/j/k, i//k
        private static Index ParseRawTriple(string token)
        {
            Index idx = new Index() { VertexIndex = 0, TexcoordIndex = 0, NormalIndex = 0 };
            // We just do naive splitting by '/'
            // If there's no '/', it's just the v index
            string[] parts = token.Split('/');
            int vIdx = 0, vtIdx = 0, vnIdx = 0;

            if (!string.IsNullOrEmpty(parts[0]))
                int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out vIdx);

            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out vtIdx);

            if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
                int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out vnIdx);

            // Convert from 1-based to 0-based. Negative means relative.
            idx.VertexIndex = FixIndex(vIdx);
            idx.TexcoordIndex = FixIndex(vtIdx);
            idx.NormalIndex = FixIndex(vnIdx);

            return idx;
        }

        private static int FixIndex(int idx)
        {
            if (idx > 0) return idx - 1;
            return idx - 1; // negative or zero
        }

        private static void ExportGroupsToShape(
            List<Shape> shapes,
            ref PrimGroup primGroup,
            string groupName,
            int materialId,
            List<float> v,
            bool triangulate,
            ref string warning)
        {
            if (!primGroup.HasData()) return;

            Shape shape = new Shape();
            shape.Name = groupName;

            // faceGroup => shape.Mesh
            foreach (var face in primGroup.FaceGroup)
            {
                int nVerts = face.VertexIndices.Count;
                if (nVerts < 3)
                {
                    warning += "Degenerate face found.\n";
                    continue;
                }
                if (triangulate && nVerts > 3)
                {
                    // naive ear clipping or fan approach. We'll do a fan approach for brevity:
                    // Face (v0, v1, v2, v3, ...) => (v0,v1,v2),(v0,v2,v3), ...
                    // Not robust for complex polygons, but typical for quads etc.

                    var baseIndex = face.VertexIndices[0];
                    for (int i = 1; i < (nVerts - 1); i++)
                    {
                        Index i1 = face.VertexIndices[i];
                        Index i2 = face.VertexIndices[i + 1];
                        shape.Mesh.Indices.Add(baseIndex);
                        shape.Mesh.Indices.Add(i1);
                        shape.Mesh.Indices.Add(i2);

                        shape.Mesh.NumFaceVertices.Add(3);
                        shape.Mesh.MaterialIds.Add(materialId);
                        shape.Mesh.SmoothingGroupIds.Add(face.SmoothingGroupId);
                    }
                }
                else
                {
                    // store as is
                    foreach (var idx in face.VertexIndices)
                    {
                        shape.Mesh.Indices.Add(idx);
                    }
                    shape.Mesh.NumFaceVertices.Add((uint)nVerts);
                    shape.Mesh.MaterialIds.Add(materialId);
                    shape.Mesh.SmoothingGroupIds.Add(face.SmoothingGroupId);
                }
            }

            // lines
            foreach (var line in primGroup.LineGroup)
            {
                foreach (var idx in line.VertexIndices)
                {
                    shape.Lines.Indices.Add(idx);
                }
                shape.Lines.NumLineVertices.Add(line.VertexIndices.Count);
            }
            // points
            foreach (var pts in primGroup.PointsGroup)
            {
                foreach (var idx in pts.VertexIndices)
                {
                    shape.Points.Indices.Add(idx);
                }
            }

            shapes.Add(shape);
            primGroup.Clear();
        }

        #region Helper Classes for "PrimGroup"

        private class Face
        {
            public uint SmoothingGroupId = 0;
            public List<Index> VertexIndices = new List<Index>();
        }

        private class LineElm
        {
            public List<Index> VertexIndices = new List<Index>();
        }

        private class PointsElm
        {
            public List<Index> VertexIndices = new List<Index>();
        }

        private class PrimGroup
        {
            public List<Face> FaceGroup = new List<Face>();
            public List<LineElm> LineGroup = new List<LineElm>();
            public List<PointsElm> PointsGroup = new List<PointsElm>();

            public void Clear()
            {
                FaceGroup.Clear();
                LineGroup.Clear();
                PointsGroup.Clear();
            }

            public bool HasData()
            {
                return (FaceGroup.Count > 0 || LineGroup.Count > 0 || PointsGroup.Count > 0);
            }
        }

        #endregion
    }

    #endregion
}
