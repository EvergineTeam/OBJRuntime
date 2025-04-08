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
            var model = await OBJRuntime.Instance.Read("MyModel.obj");
            
            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            var entity = model.InstantiateModelHierarchy(assetsService);
            this.Managers.EntityManager.Add(entity);
        }
    }
}


