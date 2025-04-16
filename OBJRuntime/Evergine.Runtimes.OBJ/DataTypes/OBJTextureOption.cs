// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace OBJRuntime.DataTypes
{
    // https://en.wikipedia.org/wiki/Wavefront_.obj_file says ...
    //
    //  -blendu on | off                       # set horizontal texture blending
    //  (default on)
    //  -blendv on | off                       # set vertical texture blending
    //  (default on)
    //  -boost real_value                      # boost mip-map sharpness
    //  -mm base_value gain_value              # modify texture map values (default
    //  0 1)
    //                                         #     base_value = brightness,
    //                                         gain_value = contrast
    //  -o u [v [w]]                           # Origin offset             (default
    //  0 0 0)
    //  -s u [v [w]]                           # Scale                     (default
    //  1 1 1)
    //  -t u [v [w]]                           # Turbulence                (default
    //  0 0 0)
    //  -texres resolution                     # texture resolution to create
    //  -clamp on | off                        # only render texels in the clamped
    //  0-1 range (default off)
    //                                         #   When unclamped, textures are
    //                                         repeated across a surface,
    //                                         #   when clamped, only texels which
    //                                         fall within the 0-1
    //                                         #   range are rendered.
    //  -bm mult_value                         # bump multiplier (for bump maps
    //  only)
    //
    //  -imfchan r | g | b | m | l | z         # specifies which channel of the file
    //  is used to
    //                                         # create a scalar or bump texture.
    //                                         r:red, g:green,
    //                                         # b:blue, m:matte, l:luminance,
    //                                         z:z-depth..
    //                                         # (the default for bump is 'l' and
    //                                         for decal is 'm')
    //  bump -imfchan r bumpmap.tga            # says to use the red channel of
    //  bumpmap.tga as the bumpmap
    //
    // For reflection maps...
    //
    //   -type sphere                           # specifies a sphere for a "refl"
    //   reflection map
    //   -type cube_top    | cube_bottom |      # when using a cube map, the texture
    //   file for each
    //         cube_front  | cube_back   |      # side of the cube is specified
    //         separately
    //         cube_left   | cube_right
    public class OBJTextureOption
    {
        public OBJTextureType Type = OBJTextureType.None;
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
