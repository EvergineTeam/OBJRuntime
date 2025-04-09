// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;
using System.IO;
using OBJRuntime.DataTypes;
using System.Collections.Generic;
using OBJRuntime.Readers;

namespace OBJTests
{
    public class OBJLoaderTests
    {
        private AssetsDirectory assetsDirectory;

        public OBJLoaderTests()
        {
            this.assetsDirectory = new AssetsDirectory();
        }

        [Fact]
        public void CheckVertexValues()
        {
            // Arrange
            var expectedVertices = new float[]
            {
                0.000000f, 2.000000f, 2.000000f,
                0.000000f, 0.000000f, 2.000000f,
                2.000000f, 0.000000f, 2.000000f,
                2.000000f, 2.000000f, 2.000000f,
                0.000000f, 2.000000f, 0.000000f,
                0.000000f, 0.000000f, 0.000000f,
                2.000000f, 0.000000f, 0.000000f,
                2.000000f, 2.000000f, 0.000000f
            };

            var attrib = new Attrib();
            var shapes = new List<Shape>();
            var materials = new List<MaterialInfo>();
            var warning = "";
            var error = "";

            // Act
            using (var streamObj = assetsDirectory.Open("Cube.obj"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    bool ok = ObjLoader.LoadObj(srObj, ref attrib, shapes, materials, ref warning, ref error, null, true, true);

                    // Assert
                    Assert.True(ok);
                    Assert.Equal(expectedVertices.Length, attrib.Vertices.Count);
                    for (int i = 0; i < expectedVertices.Length; i++)
                    {
                        Assert.Equal(expectedVertices[i], attrib.Vertices[i], 6);
                    }
                }
            }
        }

        [Fact]
        public void CheckShapeNames()
        {
            // Arrange
            var attrib = new Attrib();
            var shapes = new List<Shape>();
            var materials = new List<MaterialInfo>();
            var warning = "";
            var error = "";

            // Act
            using (var streamObj = assetsDirectory.Open("Cube.obj"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    bool ok = ObjLoader.LoadObj(srObj, ref attrib, shapes, materials, ref warning, ref error, null, true, true);

                    // Assert
                    Assert.True(ok);
                    Assert.Equal(6, shapes.Count);
                    Assert.Equal("front cube", shapes[0].Name);
                    Assert.Equal("back cube", shapes[1].Name);
                    Assert.Equal("right cube", shapes[2].Name);
                    Assert.Equal("top cube", shapes[3].Name);
                    Assert.Equal("left cube", shapes[4].Name);
                    Assert.Equal("bottom cube", shapes[5].Name);
                }
            }
        }

        [Fact]
        public void CheckFaceTessellation()
        {
            // Arrange
            var expectedVertices = new float[] { 0, 1, 2, 0, 2, 3 };

            var attrib = new Attrib();
            var shapes = new List<Shape>();
            var materials = new List<MaterialInfo>();
            var warning = "";
            var error = "";

            // Act
            using (var streamObj = assetsDirectory.Open("Cube.obj"))
            {
                using (StreamReader srObj = new StreamReader(streamObj))
                {
                    bool ok = ObjLoader.LoadObj(srObj, ref attrib, shapes, materials, ref warning, ref error, null, true, true);

                    // Assert
                    Assert.True(ok);

                    var indices = shapes[0].Mesh.Indices;
                    Assert.Equal(expectedVertices.Length, indices.Count);
                    for (int i = 0; i < expectedVertices.Length; i++)
                    {
                        Assert.Equal(expectedVertices[i], indices[i].VertexIndex);
                    }
                }
            }
        }
    }
}