// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Common.IO;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Graphics.Materials;
using Evergine.Framework.Runtimes;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using OBJRuntime.DataTypes;
using OBJRuntime.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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


            return new List<Mesh>();
        }

        private Material CreateEvergineMaterial(MaterialData data)
        {
            var effect = this.assetsService.Load<Effect>(DefaultResourcesIDs.StandardEffectID);
            var layer = this.assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.OpaqueRenderLayerID);
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
