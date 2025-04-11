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
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

            if (this.graphicsContext == null || this.assetsService == null)
            {
                this.graphicsContext = Application.Current.Container.Resolve<GraphicsContext>();
                this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            }

            // Read OBJ data
            var attrib = new OBJAttrib();
            var shapes = new List<OBJShape>();
            var materials = new List<OBJMaterial>();
            var warning = string.Empty;
            var error = string.Empty;
            using (var srObj = new StreamReader(stream))
            {
                bool success = OBJLoader.Load(srObj, ref attrib, shapes, materials, ref warning, ref error, null, true, true);
                if (!success)
                {
                    throw new Exception($"OBJ Load failed. Error:{error}");
                }
            }

            // Create meshes
            Vector3 min = new Vector3();
            Vector3 max = new Vector3();
            List<Mesh> meshes = await CreateMeshes(attrib, shapes);

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
            Material material = null;
            MaterialData data = new OBJMaterialData();
            if (materialAssigner == null)
            {
                material = this.CreateEvergineMaterial(data);
            }
            else
            {
                material = await materialAssigner(data);
            }

            this.assetsService.RegisterInstance<Material>(material);
            var materialCollection = new List<(string, Guid)>()
            {
                ("Default", material.Id),
            };

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

        private async Task<List<Mesh>> CreateMeshes(OBJAttrib attrib, List<OBJShape> shapes)
        {
            List<Mesh> meshes = new List<Mesh>(shapes.Count);

            await EvergineForegroundTask.Run(() =>
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

                        vertices[i] = new VertexPositionNormalTexture(positionId != -1 ? attrib.Vertices[positionId] : Vector3.Zero,
                                                                      normalId != -1 ? attrib.Vertices[normalId] : Vector3.Zero,
                                                                      texcoordId != -1 ? attrib.Texcoords[texcoordId] : Vector2.Zero);

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
                    //var ids = shape.Mesh.MaterialIds;

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

        private Material CreateEvergineMaterial(MaterialData data)
        {
            var effect = this.assetsService.Load<Effect>(DefaultResourcesIDs.StandardEffectID);
            var layer = this.assetsService.Load<RenderLayerDescription>(EvergineContent.RenderLayers.CullFront);
            StandardMaterial material = new StandardMaterial(effect)
            {
                LightingEnabled = data.HasVertexNormal,
                IBLEnabled = data.HasVertexNormal,
                BaseColor = data.BaseColor,
                Alpha = data.AlphaCutoff,
                LayerDescription = layer,
            };

            return material.Material;
        }
    }
}
