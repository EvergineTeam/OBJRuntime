// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Services;

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
            //var model = await OBJRuntime.Instance.Read("Models/cube.obj");
            //var model = await OBJRuntime.Instance.Read("Models/cube-normals.obj");            
            //var model = await OBJRuntime.Instance.Read("Models/bunny.obj");
            var model = await OBJRuntime.Instance.Read("Models/orc.obj");

            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            var entity = model.InstantiateModelHierarchy(assetsService);
            this.Managers.EntityManager.Add(entity);                       
        }
    }
}


