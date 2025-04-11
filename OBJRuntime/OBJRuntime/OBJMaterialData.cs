// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Framework.Runtimes;
using OBJRuntime.DataTypes;
using System.Threading.Tasks;

namespace Evergine.Runtimes.OBJ
{
    public class OBJMaterialData : MaterialData
    {
        public OBJMaterial OBJMaterial;

        private int materialId;

        public OBJRuntime OBJ;

        /// <inheritdoc/>
        public override string Name => this.OBJMaterial.Name ?? $"material{materialId}";

        /// <inheritdoc/>
        public override Color BaseColor => Color.FromVector3(ref this.OBJMaterial.Diffuse);

        /// <inheritdoc/>
        public override float MetallicFactor => this.OBJMaterial.Metallic;

        /// <inheritdoc/>
        public override float RoughnessFactor => this.OBJMaterial.Roughness;

        /// <inheritdoc/>
        public override LinearColor EmissiveColor => new LinearColor(this.OBJMaterial.Emission);

        /// <inheritdoc/>
        public override AlphaMode AlphaMode => AlphaMode.Opaque;

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
                diffuseSampler = this.OBJ.LinearWrapSampler;
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
