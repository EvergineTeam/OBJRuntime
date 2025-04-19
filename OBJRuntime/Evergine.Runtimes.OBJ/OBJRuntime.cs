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
    /// <summary>
    /// Runtime for OBJ files.
    /// </summary>
    public class OBJRuntime : ModelRuntime
    {
        /// <summary>
        /// Gets the a default instance of the class resolving the required services using the default Evergine container.
        /// </summary>
        public readonly static OBJRuntime Instance = new OBJRuntime();

        private GraphicsContext graphicsContext;
        private AssetsService assetsService;
        private AssetsDirectory assetsDirectory;

        private Dictionary<int, (string name, Material material)> materials = new Dictionary<int, (string, Material)>();
        private Func<MaterialData, Task<Material>> materialAssigner = null;

        /// <summary>
        /// Default sampler state for linear filtering with wrap mode.
        /// </summary>
        public SamplerState LinearWrapSampler = null;

        /// <summary>
        /// Default sampler state for linear filtering with clamp mode.
        /// </summary>
        public SamplerState LinearClampSampler = null;

        /// <summary>
        /// Gets or sets the working directory for the OBJ file.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use smooth normals.
        /// </summary>
        public bool UseSmoothNormals { get; set; }

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

        /// <summary>
        /// Gets the file extension for the OBJ runtime.
        /// </summary>
        public override string Extentsion => ".obj";

        /// <summary>
        /// Reads a 3D format file from the specified file path and returns a model asset.
        /// </summary>
        /// <param name="filePath">The path to the OBJ file to be read.</param>
        /// <param name="materialAssigner">A function to assign materials to the model. If null, default materials will be used.</param>
        /// <param name="useSmoothNormals">A boolean indicating whether to compute and use smooth normals for the model.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the loaded <see cref="Model"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the file stream is not readable or if the file cannot be opened.</exception>
        public async Task<Model> Read(string filePath, Func<MaterialData, Task<Material>> materialAssigner = null, bool useSmoothNormals = false)
        {
            Model model = null;
            if (this.assetsDirectory == null)
            {
                this.assetsDirectory = Application.Current.Container.Resolve<AssetsDirectory>();
            }

            this.WorkingDirectory = Path.GetDirectoryName(filePath);
            this.UseSmoothNormals = useSmoothNormals;

            using (var stream = this.assetsDirectory.Open(filePath))
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
                        model = await this.Read(memoryStream, materialAssigner);
                    }
                }
                else
                {
                    model = await this.Read(stream, materialAssigner);
                }
            }

            return model;
        }

        /// <summary>
        /// Reads a 3D format file from the specified stream and returns a model asset.
        /// </summary>
        /// <param name="stream">The stream containing the OBJ file data.</param>
        /// <param name="materialAssigner">A function to assign materials to the model. If null, default materials will be used.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the loaded <see cref="Model"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the stream is not readable or seekable.</exception>
        /// <exception cref="Exception">Thrown if the OBJ file fails to load.</exception>
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
            List<Mesh> meshes = await this.CreateMeshes(attrib, shapes, materials);

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
                this.LinearClampSampler = this.assetsService?.Load<SamplerState>(DefaultResourcesIDs.LinearClampSamplerID);
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
                    var meshIndices = shape.Mesh.Indices.ToArray();

                    VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[meshIndices.Length];
                    var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                    var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                    // Create Vertex array
                    for (int i = 0; i < meshIndices.Length; i += 3)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            int index = i + j;

                            int positionId = meshIndices[index].VertexIndex;
                            int normalId = meshIndices[index].NormalIndex;
                            int texcoordId = meshIndices[index].TexcoordIndex;

                            var vertex = new VertexPositionNormalTexture();

                            vertex.Position = positionId != OBJLoader.UndefinedIndex ? attrib.Vertices[positionId] : Vector3.Zero;
                            vertex.Normal = normalId != OBJLoader.UndefinedIndex ? attrib.Normals[normalId] : Vector3.Zero;
                            vertex.TexCoord = texcoordId != OBJLoader.UndefinedIndex ? attrib.Texcoords[texcoordId] : Vector2.Zero;
                            vertex.TexCoord.Y = 1 - vertex.TexCoord.Y;

                            vertices[index] = vertex;

                            Vector3.Max(ref vertex.Position, ref max, out max);
                            Vector3.Min(ref vertex.Position, ref min, out min);
                        }
                    }

                    // Compute smooth normals if none were provided in the OBJ (attrib.Normals.Count == 0)
                    if (attrib.Normals.Count == 0)
                    {
                        for (int i = 0; i < meshIndices.Length; i += 3)
                        {
                            Vector3 pos0 = vertices[i + 0].Position;
                            Vector3 pos1 = vertices[i + 2].Position;
                            Vector3 pos2 = vertices[i + 1].Position;

                            Vector3 edge1 = pos1 - pos0;
                            Vector3 edge2 = pos2 - pos0;

                            Vector3 faceNormal = Vector3.Cross(edge1, edge2);
                            faceNormal = Vector3.Normalize(faceNormal);

                            vertices[i].Normal = faceNormal;
                            vertices[i + 1].Normal = faceNormal;
                            vertices[i + 2].Normal = faceNormal;
                        }
                    }

                    if (this.UseSmoothNormals)
                    {
                        // Create a dictionary to accumulate normals per unique vertex position.
                        // Pre-size the dictionary to minimize rehashing.
                        var smoothDict = new Dictionary<Vector3, (Vector3 sum, int count)>(vertices.Length);

                        // First pass: accumulate normals and counts.
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            Vector3 pos = vertices[i].Position;

                            if (smoothDict.TryGetValue(pos, out var data))
                            {
                                data.sum += vertices[i].Normal;
                                data.count++;
                                smoothDict[pos] = data;
                            }
                            else
                            {
                                smoothDict.Add(pos, (vertices[i].Normal, 1));
                            }
                        }

                        // Second pass: assign averaged normals to each vertex.
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            Vector3 pos = vertices[i].Position;
                            var data = smoothDict[pos];

                            vertices[i].Normal = Vector3.Normalize(data.sum / data.count);
                        }
                    }

                    // Create vertex buffer
                    var pBufferDescription = new BufferDescription(
                                                (uint)(Unsafe.SizeOf<VertexPositionNormalTexture>() * vertices.Length),
                                                BufferFlags.ShaderResource | BufferFlags.VertexBuffer,
                                                ResourceUsage.Default);

                    Buffer pBuffer = this.graphicsContext.Factory.CreateBuffer(vertices, ref pBufferDescription);
                    VertexBuffer vertexBuffer = new VertexBuffer(pBuffer, VertexPositionNormalTexture.VertexFormat);

                    // Get Material
                    int materialIndex = 0;
                    var ids = shape.Mesh.MaterialIds;
                    if (ids.Count > 0 && ids[0] != -1)
                    {
                        var materialId = ids[0];
                        materialIndex = await this.ReadMaterial(materialId, materials);
                    }

                    // Create Mesh
                    var vertexBuffers = new VertexBuffer[] { vertexBuffer };
                    var mesh = new Mesh(vertexBuffers, PrimitiveTopology.TriangleList, vertices.Length / 3, 0)
                    {
                        BoundingBox = new BoundingBox(min, max),
                        MaterialIndex = materialIndex,
                        AllowBatching = false,
                    };

                    meshes.Add(mesh);
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

            // Get Layer
            RenderLayerDescription layer;
            float alpha = data.BaseColor.A / 255.0f;
            switch (data.AlphaMode)
            {
                case AlphaMode.Mask:
                    layer = this.assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.OpaqueRenderLayerID);
                    var renderstate = layer.RenderState;
                    renderstate.RasterizerState.CullMode = CullMode.None;
                    layer.RenderState = renderstate;
                    break;
                default:
                case AlphaMode.Opaque:
                    layer = this.assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.OpaqueRenderLayerID);
                    break;
                case AlphaMode.Blend:
                    layer = this.assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.AlphaRenderLayerID);
                    break;
            }

            StandardMaterial material = new StandardMaterial(effect)
            {
                LightingEnabled = true,
                IBLEnabled = true,
                BaseColor = data.BaseColor,
                Alpha = alpha,
                BaseColorTexture = baseColor.Texture,
                BaseColorSampler = baseColor.Sampler,
                Roughness = data.RoughnessFactor,
                Metallic = data.MetallicFactor,
                EmissiveColor = data.EmissiveColor.ToColor(),
                LayerDescription = layer,
                AlphaCutout = data.AlphaMode == AlphaMode.Mask ? 0.5f : 0,
            };

            return material.Material;
        }

        /// <summary>
        /// Reads a texture from the specified file name.
        /// </summary>
        /// <param name="diffuseTexname">The name of the texture file to be read.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the loaded <see cref="Texture"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if the texture file name is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified texture file does not exist in the assets directory.</exception>
        /// <exception cref="Exception">Thrown if there is an error during the texture loading process.</exception>
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
