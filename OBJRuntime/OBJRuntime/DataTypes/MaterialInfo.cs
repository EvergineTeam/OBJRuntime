// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;

namespace OBJRuntime.DataTypes
{
    /// <summary>
    /// TinyObj material definition
    /// </summary>
    public class MaterialInfo
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
}
