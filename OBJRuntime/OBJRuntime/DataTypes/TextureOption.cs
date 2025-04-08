// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace OBJRuntime.DataTypes
{
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
}
