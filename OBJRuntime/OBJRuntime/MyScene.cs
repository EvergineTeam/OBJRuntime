using Evergine.Common.IO;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using System.IO;
using System;
using TinyObj;

namespace Evergine.Runtimes.OBJ
{
    public class MyScene : Scene
    {
        public override void RegisterManagers()
        {
            base.RegisterManagers();

            this.Managers.AddManager(new global::Evergine.Bullet.BulletPhysicManager3D());

        }

        protected override async void CreateScene()
        {
            /*var model = await OBJRuntime.Instance.Read("MyModel.obj");
            
            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            var entity = model.InstantiateModelHierarchy(assetsService);
            this.Managers.EntityManager.Add(entity);*/

            //Test
            var reader = new ObjReader();
            var config = new ObjReaderConfig
            {
                Triangulate = true,
                VertexColor = true,
                MtlSearchPath = ""
            };

            var assetsDirectory = Application.Current.Container.Resolve<AssetsDirectory>();
            using (var stream = assetsDirectory.Open("Models/Cube.obj"))
            {
                if (stream == null || !stream.CanRead)
                {
                    throw new ArgumentException("Stream must be readable");
                }

                using (StreamReader sr = new StreamReader(stream))
                {
                    string content = await sr.ReadToEndAsync();
                    
                    bool ok = reader.ParseFromString(content, string.Empty, config);
                    if (!ok) {
                        Console.WriteLine("Load failed. Error = " + reader.Error);
                        return;
                    }
                    Console.WriteLine("Warnings: " + reader.Warning);
                    Console.WriteLine($"Vertices count = {reader.Attrib.Vertices.Count/3}");
                    Console.WriteLine($"Shapes count   = {reader.Shapes.Count}");                                                           
                }
            }                        
        }
    }
}


