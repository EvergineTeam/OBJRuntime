﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using OBJRuntime.DataTypes;
using Evergine.Mathematics;

namespace OBJRuntime.Readers
{
    /// <summary>
    /// Helper class for loading .mtl text into a List&lt;Material&gt;.
    /// </summary>
    public static class MtlLoader
    {
        // The main function to load .mtl data:
        public static void Load(
            StreamReader sr,
            List<OBJMaterial> materials,
            Dictionary<string, int> materialMap,
            ref string warning,
            ref string error)
        {
            // If there's no "newmtl" at all, we still push a default material at the end.
            OBJMaterial material = new OBJMaterial();
            bool firstMaterial = true;

            bool hasD = false;
            bool hasTr = false;
            bool hasKd = false; // to set a default Kd if we see map_Kd w/o Kd

            int lineNo = 0;

            while (!sr.EndOfStream)
            {
                lineNo++;
                string line = sr.ReadLine();
                if (line == null)
                    break;

                line = line.Trim();
                if (line.Length < 1)
                    continue;  // skip blank lines

                if (line.StartsWith("#"))
                    continue; // skip comments

                var tokens = Helpers.Tokenize(line);
                if (tokens.Count == 0)
                    continue;

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
                    material = new OBJMaterial();
                    hasD = false;
                    hasTr = false;
                    hasKd = false;
                    firstMaterial = false;

                    material.Name = line.Substring(6).Trim(); // or tokens[1..end]
                }
                else if ((key == "Ka" || key == "ka") && tokens.Count >= 4)
                {
                    Helpers.ParseVector3(tokens, 1, ref material.Ambient);
                }
                else if ((key == "Kd" || key == "kd") && tokens.Count >= 4)
                {
                    Helpers.ParseVector3(tokens, 1, ref material.Diffuse);
                    hasKd = true;
                }
                else if ((key == "Ks" || key == "ks") && tokens.Count >= 4)
                {
                    Helpers.ParseVector3(tokens, 1, ref material.Specular);
                }
                else if (key == "Ke" && tokens.Count >= 4)
                {
                    Helpers.ParseVector3(tokens, 1, ref material.Emission);
                }
                else if ((key == "Tf" || key == "Kt") && tokens.Count >= 4)
                {
                    Helpers.ParseVector3(tokens, 1, ref material.Transmittance);
                }
                else if (key == "Ns" && tokens.Count >= 2)
                {
                    if (Helpers.TryParseFloat(tokens[1], out float val))
                        material.Shininess = val;
                }
                else if (key == "Ni" && tokens.Count >= 2)
                {
                    if (Helpers.TryParseFloat(tokens[1], out float val))
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
                    if (Helpers.TryParseFloat(tokens[1], out float val))
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
                        if (Helpers.TryParseFloat(tokens[1], out float val))
                            material.Dissolve = 1.0f - val;
                    }
                    hasTr = true;
                }
                else if (key == "map_Ka")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), ref material.AmbientTexname, material.AmbientTexopt, "Ka");
                }
                else if (key == "map_Kd")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), ref material.DiffuseTexname, material.DiffuseTexopt, "Kd");
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
                    ParseTextureAndOption(line.Substring(6).Trim(), ref material.SpecularTexname, material.SpecularTexopt, "Ks");
                }
                else if (key == "map_Ns")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), ref material.SpecularHighlightTexname, material.SpecularHighlightTexopt, "Ns");
                }
                else if (key == "map_d")
                {
                    ParseTextureAndOption(line.Substring(5).Trim(), ref material.AlphaTexname, material.AlphaTexopt, "d");
                }
                else if (key == "map_bump" || key == "map_Bump")
                {
                    ParseTextureAndOption(line.Substring(key.Length).Trim(), ref material.BumpTexname, material.BumpTexopt, "bump");
                }
                else if (key == "bump")
                {
                    ParseTextureAndOption(line.Substring(4).Trim(), ref material.BumpTexname, material.BumpTexopt, "bump");
                }
                else if (key == "map_disp" || key == "map_Disp" || key == "disp")
                {
                    int skipLen = key == "disp" ? 4 : 8;
                    ParseTextureAndOption(line.Substring(skipLen).Trim(), ref material.DisplacementTexname, material.DisplacementTexopt, "disp");
                }
                else if (key == "refl")
                {
                    ParseTextureAndOption(line.Substring(4).Trim(), ref material.ReflectionTexname, material.ReflectionTexopt, "refl");
                }
                else if (key == "map_Pr")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), ref material.RoughnessTexname, material.roughness_texopt, "Pr");
                }
                else if (key == "map_Pm")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), ref material.MetallicTexname, material.metallic_texopt, "Pm");
                }
                else if (key == "map_Ps")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), ref material.SheenTexname, material.sheen_texopt, "Ps");
                }
                else if (key == "map_Ke")
                {
                    ParseTextureAndOption(line.Substring(6).Trim(), ref material.EmissiveTexname, material.emissive_texopt, "Ke");
                }
                else if (key == "norm")
                {
                    ParseTextureAndOption(line.Substring(4).Trim(), ref material.NormalTexname, material.normal_texopt, "norm");
                }
                else if (key == "Pr")
                {
                    if (Helpers.TryParseFloat(tokens[1], out float val))
                        material.Roughness = val;
                }
                else if (key == "Pm")
                {
                    if (Helpers.TryParseFloat(tokens[1], out float val))
                        material.Metallic = val;
                }
                else if (key == "Ps")
                {
                    if (Helpers.TryParseFloat(tokens[1], out float val))
                        material.Sheen = val;
                }
                else if (key == "Pc")
                {
                    if (Helpers.TryParseFloat(tokens[1], out float val))
                        material.ClearcoatThickness = val;
                }
                else if (key == "Pcr")
                {
                    if (Helpers.TryParseFloat(tokens[1], out float val))
                        material.ClearcoatRoughness = val;
                }
                else if (key == "aniso")
                {
                    if (Helpers.TryParseFloat(tokens[1], out float val))
                        material.Anisotropy = val;
                }
                else if (key == "anisor")
                {
                    if (Helpers.TryParseFloat(tokens[1], out float val))
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
            {
                materialMap.Add(material.Name, materials.Count);
            }

            materials.Add(material);
        }

        private static void ParseTextureAndOption(string line, ref string texName, OBJTextureOption texOpt, string prefix)
        {
            // We parse the line for texture name and possible sub‐options like -o, -s, etc.
            // This can be somewhat simplified, but let's keep a structure close to the original.
            // For a more robust approach, you can do additional tokenization of the entire line.

            // We do a naive split for demonstration, real code might do more refined parse.
            var tokens = Helpers.Tokenize(line);

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
                        texOpt.Blendu = tokens[idx] == "on";
                    }
                }
                else if (t.StartsWith("-blendv"))
                {
                    idx++;
                    if (idx < tokens.Count)
                    {
                        texOpt.Blendv = tokens[idx] == "on";
                    }
                }
                else if (t.StartsWith("-clamp"))
                {
                    idx++;
                    if (idx < tokens.Count)
                    {
                        texOpt.Clamp = tokens[idx] == "on";
                    }
                }
                else if (t.StartsWith("-boost"))
                {
                    idx++;
                    if (idx < tokens.Count && Helpers.TryParseFloat(tokens[idx], out float val)) texOpt.Sharpness = val;
                }
                else if (t.StartsWith("-bm"))
                {
                    idx++;
                    if (idx < tokens.Count && Helpers.TryParseFloat(tokens[idx], out float val)) texOpt.BumpMultiplier = val;
                }
                else if (t.StartsWith("-o"))
                {
                    // e.g. -o u [v [w]]
                    // parse up to 3 floats
                    int maxCoords = 3;
                    int coordCount = 0;
                    idx++;
                    while (coordCount < maxCoords && idx < tokens.Count && Helpers.TryParseFloat(tokens[idx], out float oval))
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
                    while (coordCount < maxCoords && idx < tokens.Count && Helpers.TryParseFloat(tokens[idx], out float sval))
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
                    while (coordCount < maxCoords && idx < tokens.Count && Helpers.TryParseFloat(tokens[idx], out float tval))
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
                    if (idx < tokens.Count && Helpers.TryParseFloat(tokens[idx], out float bval))
                    {
                        texOpt.Brightness = bval;
                        idx++;
                        if (idx < tokens.Count && Helpers.TryParseFloat(tokens[idx], out float cval))
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
    }
}
