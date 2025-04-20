// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Framework.Runtimes;
using Evergine.Mathematics;
using OBJRuntime.DataTypes;
using System.IO;
using System.Threading.Tasks;

namespace Evergine.Runtimes.OBJ
{
    /// <summary>
    /// Represents the material data for an OBJ file.
    /// </summary>
    public class OBJMaterialData : MaterialData
    {
        private int materialId;

        /// <summary>
        /// The material data parsed from the OBJ file.
        /// </summary>
        public OBJMaterial OBJMaterial;

        /// <summary>
        /// The unique identifier for the material.
        /// </summary>
        public OBJRuntime OBJ;

        /// <inheritdoc/>
        public override string Name => this.OBJMaterial.Name ?? $"material{this.materialId}";

        /// <inheritdoc/>
        public override Color BaseColor
        {
            get
            {
                // Fix mtl issue when mtl use a texture but the diffuse color is black
                if (!string.IsNullOrEmpty(this.OBJMaterial.DiffuseTexname) && this.OBJMaterial.Diffuse == Vector3.Zero)
                {
                    Vector4 diffuse = new Vector4(1, 1, 1, this.OBJMaterial.Dissolve);
                    return Color.FromVector4(ref diffuse);
                }
                else
                {
                    Vector4 diffuse = this.OBJMaterial.Diffuse.ToVector4(this.OBJMaterial.Dissolve);
                    return Color.FromVector4(ref diffuse);
                }
            }
        }

        /// <inheritdoc/>
        public override float MetallicFactor => this.OBJMaterial.Metallic;

        /// <inheritdoc/>
        public override float RoughnessFactor => this.OBJMaterial.Roughness;

        /// <inheritdoc/>
        public override LinearColor EmissiveColor => new LinearColor(this.OBJMaterial.Emission);

        /// <inheritdoc/>
        public override AlphaMode AlphaMode
        {
            get
            {
                var result = AlphaMode.Opaque;
                if (!string.IsNullOrEmpty(this.OBJMaterial.AlphaTexname) ||
                    Path.GetExtension(this.OBJMaterial.DiffuseTexname) == ".png")
                {
                    result = AlphaMode.Mask;
                }
                else if (this.BaseColor.A < 255f)
                {
                    result = AlphaMode.Blend;
                }

                return result;
            }
        }

        /// <inheritdoc/>
        public override float AlphaCutoff => 0.5f;

        /// <inheritdoc/>
        public override bool HasVertexColor => false;

        /// <inheritdoc/>
        public override bool HasVertexNormal => true;

        /// <inheritdoc/>
        public override bool HasVertexTexcoord => false;

        /// <inheritdoc/>
        public override bool HasVertexTangent => false;

        /// <inheritdoc/>
        public override bool HasDoubleSided => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="OBJMaterialData"/> class.
        /// </summary>
        /// <param name="objMaterial">The material data parsed from the OBJ file.</param>
        /// <param name="materialId">The unique identifier for the material.</param>
        /// <param name="objRuntime">The runtime instance responsible for handling OBJ assets.</param>
        public OBJMaterialData(OBJMaterial objMaterial, int materialId, OBJRuntime objRuntime)
        {
            this.OBJMaterial = objMaterial;
            this.materialId = materialId;
            this.OBJ = objRuntime;
        }

        /// <inheritdoc/>
        public override async Task<(Texture Texture, SamplerState Sampler)> GetBaseColorTextureAndSampler()
        {
            Texture diffuseTexture = null;
            SamplerState diffuseSampler = null;
            if (this.OBJMaterial != null && !string.IsNullOrEmpty(this.OBJMaterial.DiffuseTexname))
            {
                diffuseTexture = await this.OBJ.ReadTexture(this.OBJMaterial.DiffuseTexname);
                diffuseSampler = this.OBJMaterial.DiffuseTexopt.Clamp ? this.OBJ.LinearClampSampler : this.OBJ.LinearWrapSampler;
            }

            return (diffuseTexture, diffuseSampler);
        }

        /// <inheritdoc/>
        public override Task<(Texture Texture, SamplerState Sampler)> GetEmissiveTextureAndSampler()
        {
            return Task.FromResult<(Texture, SamplerState)>(default);
        }

        /// <inheritdoc/>
        public override Task<(Texture Texture, SamplerState Sampler)> GetMetallicRoughnessTextureAndSampler()
        {
            return Task.FromResult<(Texture, SamplerState)>(default);
        }

        /// <inheritdoc/>
        public override Task<(Texture Texture, SamplerState Sampler)> GetNormalTextureAndSampler()
        {
            return Task.FromResult<(Texture, SamplerState)>(default);
        }

        /// <inheritdoc/>
        public override Task<(Texture Texture, SamplerState Sampler)> GetOcclusionTextureAndSampler()
        {
            return Task.FromResult<(Texture, SamplerState)>(default);
        }
    }
}
