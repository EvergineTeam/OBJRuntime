# OBJRuntime

**OBJRuntime** is a .NET library for loading and processing Wavefront OBJ files. It supports parsing vertex data, normals, texture coordinates, materials, and more. Designed for use with **.NET 8** and **C# 12.0**, this library enables easy integration of 3D models into your applications.

## Features

- Load OBJ files with:
  - Vertex positions  
  - Normals  
  - Texture coordinates  
  - Vertex colors and weights  
- Full support for materials defined in MTL files
- Handle multiple texture types: diffuse, specular, normal maps, etc.
- PBR (Physically Based Rendering) material properties support

## Usage

Integrating **OBJRuntime** is simple. Load and instantiate your model as follows:

```csharp
var model = await OBJRuntime.Instance.Read("Models/orc/orc.obj");
var assetsService = Application.Current.Container.Resolve<AssetsService>();
var entity = model.InstantiateModelHierarchy(assetsService);
this.Managers.EntityManager.Add(entity);
 ```

## Tests

The project includes a set of unit tests to ensure the correctness of the OBJ loader. The tests are located in the `OBJTests` project.


