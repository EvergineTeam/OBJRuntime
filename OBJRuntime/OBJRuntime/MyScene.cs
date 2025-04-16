// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;

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
            //var model = await OBJRuntime.Instance.Read("Models/bmw/bmw.obj");                             // Ok -> materials
            //var model = await OBJRuntime.Instance.Read("Models/breakfast_room/breakfast_room.obj");       // Ok -> Textures
            var model = await OBJRuntime.Instance.Read("Models/oak/white_oak.obj");                       // Ok -> Transparent

            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            var entity = model.InstantiateModelHierarchy(assetsService);
            entity.FindComponent<Transform3D>().Scale = new Vector3(0.01f, 0.01f, 0.01f);
            this.Managers.EntityManager.Add(entity);                       
        }
    }
}


