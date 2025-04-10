// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Mathematics;
using System.Collections.Generic;

namespace OBJRuntime.DataTypes
{
    /// <summary>
    /// TinyObj material definition
    /// </summary>
    public class OBJMaterial
    {
        public string Name = "";

        public Vector3 Ambient = Vector3.Zero;
        public Vector3 Diffuse = Vector3.One;
        public Vector3 Specular = Vector3.Zero;
        public Vector3 Transmittance = Vector3.Zero;
        public Vector3 Emission = Vector3.Zero;
        public float Shininess = 1.0f;
        public float Ior = 1.0f;        // Index of refraction
        public float Dissolve = 1.0f;   // 1=opaque; 0=fully transparent
        public int Illum = 0;          // Illumination model

        // Texture information
        public string AmbientTexname = string.Empty;            // map_Ka
        public string DiffuseTexname = string.Empty;            // map_Kd
        public string SpecularTexname = string.Empty;           // map_Ks
        public string SpecularHighlightTexname = string.Empty;  // map_Ns
        public string BumpTexname = string.Empty;               // map_bump
        public string DisplacementTexname = string.Empty;       // disp
        public string AlphaTexname = string.Empty;              // map_d
        public string ReflectionTexname = string.Empty;         // refl

        public OBJTextureOption AmbientTexopt = new OBJTextureOption();
        public OBJTextureOption DiffuseTexopt = new OBJTextureOption();
        public OBJTextureOption SpecularTexopt = new OBJTextureOption();
        public OBJTextureOption SpecularHighlightTexopt = new OBJTextureOption();
        public OBJTextureOption BumpTexopt = new OBJTextureOption();
        public OBJTextureOption DisplacementTexopt = new OBJTextureOption();
        public OBJTextureOption AlphaTexopt = new OBJTextureOption();
        public OBJTextureOption ReflectionTexopt = new OBJTextureOption();

        // PBR extension
        public float Roughness = 0.0f;           // map_Pr
        public float Metallic = 0.0f;            // map_Pm
        public float Sheen = 0.0f;               // map_Ps
        public float ClearcoatThickness = 0.0f;  // Pc
        public float ClearcoatRoughness = 0.0f;  // Pcr
        public float Anisotropy = 0.0f;          // aniso
        public float AnisotropyRotation = 0.0f;  // anisor

        public string RoughnessTexname = string.Empty;  // map_Pr
        public string MetallicTexname = string.Empty;   // map_Pm
        public string SheenTexname = string.Empty;      // map_Ps
        public string EmissiveTexname = string.Empty;   // map_Ke
        public string NormalTexname = string.Empty;     // norm

        public OBJTextureOption roughness_texopt;
        public OBJTextureOption metallic_texopt;
        public OBJTextureOption sheen_texopt;
        public OBJTextureOption emissive_texopt;
        public OBJTextureOption normal_texopt;

        public int pad2;

        // Key-value pairs for unknown parameters.
        public Dictionary<string, string> UnknownParameter = new Dictionary<string, string>();
    }
}
