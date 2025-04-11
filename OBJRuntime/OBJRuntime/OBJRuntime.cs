// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Common.Graphics.VertexFormats;
using Evergine.Common.IO;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Graphics.Materials;
using Evergine.Framework.Runtimes;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using OBJRuntime;
using OBJRuntime.DataTypes;
using OBJRuntime.Readers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Buffer = Evergine.Common.Graphics.Buffer;

namespace Evergine.Runtimes.OBJ
{
    public class OBJRuntime : ModelRuntime
    {
        /// <summary>
        /// Gets the a default instance of the class resolving the required services using the default Evergine container.
        /// </summary>
        public readonly static OBJRuntime Instance = new OBJRuntime();

        private GraphicsContext graphicsContext;
        private AssetsService assetsService;
        private AssetsDirectory assetsDirectory;

        public SamplerState LinearWrapSampler = null;

        private Dictionary<int, (string name, Material material)> materials = new Dictionary<int, (string, Material)>();
        private Func<MaterialData, Task<Material>> materialAssigner = null;

        public string WorkingDirectory { get; set; }

        private OBJRuntime()
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="OBJRuntime"/> class.
        /// </summary>
        /// <param name="graphicsContext">Graphics Context instance.</param>
        /// <param name="assetsService">Assets Service instance.</param>
        /// <param name="assetsDirectory">Assets Directory instance.</param>
        public OBJRuntime(GraphicsContext graphicsContext, AssetsService assetsService, AssetsDirectory assetsDirectory)
        {
            this.graphicsContext = graphicsContext;
            this.assetsService = assetsService;
            this.assetsDirectory = assetsDirectory;
        }

        public override string Extentsion => ".obj";

        public async Task<Model> Read(string filePath, Func<MaterialData, Task<Material>> materialAssigner = null)
        {
            Model model = null;

            if (assetsDirectory == null)
            {
                assetsDirectory = Application.Current.Container.Resolve<AssetsDirectory>();
            }

            this.WorkingDirectory = Path.GetDirectoryName(filePath);

            using (var stream = assetsDirectory.Open(filePath))
            {
                if (stream == null || !stream.CanRead)
                {
                    throw new ArgumentException("Stream must be readable");
                }

                if (!stream.CanSeek)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        model = await Read(memoryStream, materialAssigner);
                    }
                }
                else
                {
                    model = await Read(stream, materialAssigner);
                }
            }

            return model;
        }

        public override async Task<Model> Read(Stream stream, Func<MaterialData, Task<Material>> materialAssigner = null)
        {
            if (stream == null || !stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException("Stream must be readable and seekable");
            }

            this.materialAssigner = materialAssigner;

            this.LoadStaticResources();

            // Read OBJ data
            var attrib = new OBJAttrib();
            var shapes = new List<OBJShape>();
            var materials = new List<OBJMaterial>();
            var warning = string.Empty;
            var error = string.Empty;
            using (var srObj = new StreamReader(stream))
            {
                bool success = OBJLoader.Load(srObj, ref attrib, shapes, materials, ref warning, ref error, this.assetsDirectory, this.WorkingDirectory, true, true);
                if (!success)
                {
                    throw new Exception($"OBJ Load failed. Error:{error}");
                }
            }

            // Create meshes
            Vector3 min = new Vector3();
            Vector3 max = new Vector3();
            List<Mesh> meshes = await CreateMeshes(attrib, shapes, materials);

            var meshContainer = new MeshContainer()
            {
                Name = "OBJ MeshContainer",
                Meshes = meshes,
                BoundingBox = new BoundingBox(min, max),
            };

            // Generate root node
            var rootNode = new NodeContent()
            {
                Name = "OBJ file",
                Mesh = meshContainer,
                Children = Array.Empty<NodeContent>(),
                ChildIndices = Array.Empty<int>(),
            };

            // Collect materials
            var materialCollection = new List<(string, Guid)>();
            foreach (var materialInfo in this.materials.Values)
            {
                this.assetsService.RegisterInstance<Material>(materialInfo.material);
                materialCollection.Add((materialInfo.name, materialInfo.material.Id));
            }

            if (materialCollection.Count == 0)
            {
                materialCollection.Add(("default", DefaultResourcesIDs.DefaultMaterialID));
            }

            // Create model
            var model = new Model()
            {
                MeshContainers = new[] { meshContainer },
                Materials = materialCollection,
                AllNodes = new[] { rootNode },
                RootNodes = new[] { 0 },
            };

            model.RefreshBoundingBox();

            return model;
        }

        private void LoadStaticResources()
        {
            if (this.graphicsContext == null)
            {
                this.graphicsContext = Application.Current.Container.Resolve<GraphicsContext>();
                this.assetsService = Application.Current.Container.Resolve<AssetsService>();

                this.LinearWrapSampler = this.assetsService?.Load<SamplerState>(DefaultResourcesIDs.LinearWrapSamplerID);
            }
        }

        private async Task<List<Mesh>> CreateMeshes(OBJAttrib attrib, List<OBJShape> shapes, List<OBJMaterial> materials)
        {
            List<Mesh> meshes = new List<Mesh>(shapes.Count);

            await EvergineForegroundTask.Run(async () =>
            {
                for (int s = 0; s < shapes.Count; s++)
                {
                    var shape = shapes[s];
                    var meshIndices = shape.Mesh.Indices;
                    VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[meshIndices.Count];
                    var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                    var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                    // Create Vertex array
                    for (int i = 0; i < meshIndices.Count; i++)
                    {
                        int positionId = meshIndices[i].VertexIndex;
                        int normalId = meshIndices[i].NormalIndex;
                        int texcoordId = meshIndices[i].TexcoordIndex;

                        var vertex = new VertexPositionNormalTexture();

                        vertex.Position = positionId != -1 ? attrib.Vertices[positionId] : Vector3.Zero;
                        vertex.Normal = normalId != -1 ? attrib.Vertices[normalId] : Vector3.Zero;
                        vertex.TexCoord = texcoordId != -1 ? attrib.Texcoords[texcoordId] : Vector2.Zero;
                        vertex.TexCoord.Y = 1 - vertex.TexCoord.Y;

                        vertices[i] = vertex;

                        Vector3.Max(ref vertices[i].Position, ref max, out max);
                        Vector3.Min(ref vertices[i].Position, ref min, out min);
                    }

                    // Compute normals
                    if (attrib.Normals.Count == 0)
                    {
                        for (int i = 0; i < meshIndices.Count; i += 3)
                        {
                            Vector3 pos0 = vertices[i].Position;
                            Vector3 pos1 = vertices[i + 1].Position;
                            Vector3 pos2 = vertices[i + 2].Position;

                            Vector3 edge1 = pos1 - pos0;
                            Vector3 edge2 = pos2 - pos0;

                            Vector3 faceNormal = Vector3.Cross(edge1, edge2);
                            faceNormal = Vector3.Normalize(faceNormal);

                            vertices[i].Normal = faceNormal;
                            vertices[i + 1].Normal = faceNormal;
                            vertices[i + 2].Normal = faceNormal;
                        }
                    }

                    // Create vertex buffer
                    var pBufferDescription = new BufferDescription((uint)(Unsafe.SizeOf<VertexPositionNormalTexture>() * vertices.Length),
                                                 BufferFlags.ShaderResource | BufferFlags.VertexBuffer,
                                                 ResourceUsage.Default);
                    Buffer pBuffer = this.graphicsContext.Factory.CreateBuffer(vertices, ref pBufferDescription);
                    VertexBuffer vertexBuffer = new VertexBuffer(pBuffer, VertexPositionNormalTexture.VertexFormat);

                    // Get Material
                    int materialIndex = 0;
                    var ids = shape.Mesh.MaterialIds;
                    if (ids.Count > 0)
                    {
                        var materialId = ids[0];
                        materialIndex = await this.ReadMaterial(materialId, materials);
                    }

                    // Create Mesh
                    var Mesh = new Mesh([vertexBuffer], PrimitiveTopology.TriangleList, vertices.Length / 3, 0)
                    {
                        BoundingBox = new BoundingBox(min, max),
                        MaterialIndex = materialIndex,
                    };

                    meshes.Add(Mesh);
                }
            });

            return meshes;
        }

        private async Task<int> ReadMaterial(int materialId, List<OBJMaterial> materials)
        {
            var objMaterial = materials[materialId];
            MaterialData materialData = new OBJMaterialData(objMaterial, materialId, this);
            if (!this.materials.ContainsKey(materialId))
            {
                Material material = null;
                if (this.materialAssigner == null)
                {
                    material = await this.CreateEvergineMaterial(materialData);
                }
                else
                {
                    material = await this.materialAssigner(materialData);
                }

                this.materials.Add(materialId, (objMaterial.Name ?? $"material{materialId}", material));

                return this.materials.Count - 1;
            }

            return this.materials.Keys.ToList().IndexOf(materialId);
        }

        private async Task<Material> CreateEvergineMaterial(MaterialData data)
        {
            var baseColor = await data.GetBaseColorTextureAndSampler();

            var effect = this.assetsService.Load<Effect>(DefaultResourcesIDs.StandardEffectID);
            var layer = this.assetsService.Load<RenderLayerDescription>(EvergineContent.RenderLayers.CullFront);
            StandardMaterial material = new StandardMaterial(effect)
            {
                LightingEnabled = data.HasVertexNormal,
                IBLEnabled = data.HasVertexNormal,
                BaseColor = data.BaseColor,
                BaseColorTexture = baseColor.Texture,
                BaseColorSampler = baseColor.Sampler,
                Alpha = data.AlphaCutoff,
                LayerDescription = layer,
            };

            return material.Material;
        }

        public async Task<Texture> ReadTexture(string diffuseTexname)
        {
            Texture result = null;

            var textureFilePath = Path.Combine(this.WorkingDirectory, diffuseTexname);
            if (this.assetsDirectory.Exists(textureFilePath))
            {
                using (var fileStream = this.assetsDirectory.Open(textureFilePath))
                {
                    var codec = SKCodec.Create(fileStream);
                    var bitmap = new SKBitmap(codec.Info);
                    var imageInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                    var decodeResult = codec.GetPixels(imageInfo, bitmap.GetPixels());
                    await EvergineForegroundTask.Run(() =>
                    {
                        TextureDescription desc = new TextureDescription()
                        {
                            Type = TextureType.Texture2D,
                            Width = (uint)bitmap.Width,
                            Height = (uint)bitmap.Height,
                            Depth = 1,
                            ArraySize = 1,
                            Faces = 1,
                            Usage = ResourceUsage.Default,
                            CpuAccess = ResourceCpuAccess.None,
                            Flags = TextureFlags.ShaderResource,
                            Format = PixelFormat.R8G8B8A8_UNorm,
                            MipLevels = 1,
                            SampleCount = TextureSampleCount.None,
                        };
                        result = this.graphicsContext.Factory.CreateTexture(ref desc);

                        this.graphicsContext.UpdateTextureData(result, bitmap.GetPixels(), (uint)bitmap.ByteCount, 0);
                    });

                    // Read
                    fileStream.Flush();
                }
            }
            return result;
        }
    }
}
