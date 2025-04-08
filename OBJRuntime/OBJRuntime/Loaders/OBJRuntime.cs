using Evergine.Common.IO;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Runtimes;
using System;
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

        private AssetsDirectory assetsDirectory;

        private OBJRuntime()
        {
        }

        public override string Extentsion => ".obj";

        public async Task<Model> Read(string filePath, Func<MaterialData, Task<Material>> materialAssigner = null)
        {
            Model model = null;

            if (this.assetsDirectory == null)
            {
                this.assetsDirectory = Application.Current.Container.Resolve<AssetsDirectory>();
            }

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

        public override Task<Model> Read(Stream stream, Func<MaterialData, Task<Material>> materialAssigner = null)
        {
            return null;
        }
    }
}
