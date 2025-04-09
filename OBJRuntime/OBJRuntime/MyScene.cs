// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.IO;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using System.IO;
using System;
using OBJRuntime.DataTypes;
using OBJRuntime.Readers;
using static OBJRuntime.EvergineContent;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections.Generic;
using System.Diagnostics;

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
            var attrib = new Attrib();
            var shapes = new List<Shape>();
            var materials = new List<MaterialInfo>();
            var warning = "";
            var error = "";


            var assetsDirectory = Application.Current.Container.Resolve<AssetsDirectory>();
            using (var streamObj = assetsDirectory.Open("Models/Cube.obj"))
            using (var streamMtl = assetsDirectory.Open("Models/Cube.mtl"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    var mtlReader = new MaterialStreamReader(streamMtl);
                    bool ok = ObjLoader.LoadObj(srObj, ref attrib, shapes, materials, ref warning, ref error, mtlReader, true, true);
                    if (!ok)
                    {
                        Debug.WriteLine("Load failed. Error = " + error);
                        return;
                    }
                  
                    Debug.WriteLine("Warnings: " + warning);
                    Debug.WriteLine($"Vertices count = {attrib.Vertices.Count/3}");

                    Debug.WriteLine($"Shapes count   = {shapes.Count}");                                                           
                }
            }                        
        }
    }
}


