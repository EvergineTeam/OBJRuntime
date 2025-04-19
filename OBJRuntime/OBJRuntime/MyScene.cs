// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System;

namespace Evergine.Runtimes.OBJ
{
    public class MyScene : Scene
    {
        private bool showDebugLines = false;

        public override void RegisterManagers()
        {
            base.RegisterManagers();

            this.Managers.AddManager(new global::Evergine.Bullet.BulletPhysicManager3D());

        }

        protected override async void CreateScene()
        {
            ((RenderManager)this.Managers.RenderManager).DebugLines = this.showDebugLines;

            // https://casual-effects.com/data/
            //var model = await OBJRuntime.Instance.Read("Models/bunny/bunny.obj");                         // OK
            //var model = await OBJRuntime.Instance.Read("Models/orc/orc.obj", useSmoothNormals: true);     // OK
            //var model = await OBJRuntime.Instance.Read("Models/dragon/dragon.obj");                       // Ok
            //var model = await OBJRuntime.Instance.Read("Models/armadillo/armadillo.obj");                 // Ok
            //var model = await OBJRuntime.Instance.Read("Models/suzanne/suzanne.obj");                     // Ok
            //var model = await OBJRuntime.Instance.Read("Models/horse/horse.obj");                         // OK
            //var model = await OBJRuntime.Instance.Read("Models/house/house.obj");                         // Ok
            //var model = await OBJRuntime.Instance.Read("Models/sponza/sponza.obj");                       // OK
            //var model = await OBJRuntime.Instance.Read("Models/sibenik/sibenik.obj");                     // Ok
            //var model = await OBJRuntime.Instance.Read("Models/empire/lost_empire.obj");                  // Ok 
            //var model = await OBJRuntime.Instance.Read("Models/sportsCar/sportsCar.obj");                 // OK
            //var model = await OBJRuntime.Instance.Read("Models/conference/conference.obj");               // OK
            //var model = await OBJRuntime.Instance.Read("Models/CornellBox/CornellBox-Original.obj");      // Ok
            //var model = await OBJRuntime.Instance.Read("Models/mitsuba/mitsuba.obj");                     // Ok
            //var model = await OBJRuntime.Instance.Read("Models/roadBike/roadBike.obj");                   // OK
            //var model = await OBJRuntime.Instance.Read("Models/bmw/bmw.obj");                             // Ok 
            //var model = await OBJRuntime.Instance.Read("Models/breakfast_room/breakfast_room.obj");       // Ok
            //var model = await OBJRuntime.Instance.Read("Models/oak/white_oak.obj");                       // Ok
            //var model = await OBJRuntime.Instance.Read("Models/buddha/buddha.obj");                       // Ok
            //var model = await OBJRuntime.Instance.Read("Models/erato/erato.obj");                         // Ok
            //var model = await OBJRuntime.Instance.Read("Models/pine/scrubPine.obj");                      // Ok
            //var model = await OBJRuntime.Instance.Read("Models/fireplace_room/fireplace_room.obj");       // Ok
            //var model = await OBJRuntime.Instance.Read("Models/teapot/teapot.obj");                       // Ok
            //var model = await OBJRuntime.Instance.Read("Models/head/head.obj");                           // Ok
            var model = await OBJRuntime.Instance.Read("Models/holodeck/holodeck.obj");                   // Ok

            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            var root = model.InstantiateModelHierarchy(assetsService);
            var boundingBox = model.BoundingBox.Value;
            boundingBox.Transform(root.FindComponent<Transform3D>().WorldTransform);

            root.FindComponent<Transform3D>().Scale = Vector3.One * (1.0f / boundingBox.HalfExtent.Length());
            root.AddComponent(new BoxCollider3D()
            {
                Size = boundingBox.HalfExtent * 2,
                Offset = boundingBox.Center,
            });
            root.AddComponent(new StaticBody3D());

            this.Managers.EntityManager.Add(root);
        }

        protected override void Draw(TimeSpan gameTime)
        {
            if (this.showDebugLines)
            {
                foreach (var mesh in this.Managers.RenderManager.ActiveCamera3D.DrawContext.CullingResult.VisibleMeshes)
                {
                    if (mesh?.BoundingBox.HasValue == true)
                    {
                        ((RenderManager)this.Managers.RenderManager).LineBatch3D.DrawBoundingBox(mesh.BoundingBox.Value, Color.Orange);
                    }
                }
            }
            base.Draw(gameTime);
        }
    }
}


