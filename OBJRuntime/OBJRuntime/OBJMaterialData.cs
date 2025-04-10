// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Framework.Runtimes;
using System.Threading.Tasks;

namespace Evergine.Runtimes.OBJ
{
    public class OBJMaterialData : MaterialData
    {
        /// <inheritdoc/>
        public override string Name => "default";

        /// <inheritdoc/>
        public override Color BaseColor => Color.White;

        /// <inheritdoc/>
        public override float MetallicFactor => 0.0f;

        /// <inheritdoc/>
        public override float RoughnessFactor => 1.0f;

        /// <inheritdoc/>
        public override LinearColor EmissiveColor => default;

        /// <inheritdoc/>
        public override AlphaMode AlphaMode => AlphaMode.Opaque;

        /// <inheritdoc/>
        public override float AlphaCutoff => 0.5f;

        /// <inheritdoc/>
        public override bool HasVertexColor => false;

        /// <inheritdoc/>
        public override bool HasVertexNormal => false;

        /// <inheritdoc/>
        public override bool HasVertexTexcoord => false;

        /// <inheritdoc/>
        public override bool HasVertexTangent => false;

        /// <inheritdoc/>
        public override bool HasDoubleSided => false;

        /// <inheritdoc/>
        public override Task<(Texture Texture, SamplerState Sampler)> GetBaseColorTextureAndSampler()
        {
            return Task.FromResult<(Texture, SamplerState)>(default);
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
