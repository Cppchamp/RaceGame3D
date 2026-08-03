using Assimp;

namespace RaceGame3D
{
    internal class ModelMesh
    {
        public List<float> Vertices = new();
        public List<uint> Indices = new();

        public string TexturePath = "";
        public int IndexCount;

    }


    internal class ModelLoader
    {
        public List<ModelMesh> Meshes = new();


        public void Load(string path)
        {
            Meshes.Clear();

            using AssimpContext importer = new();

            Scene scene = importer.ImportFile(
                path,
                PostProcessSteps.Triangulate |
                PostProcessSteps.GenerateNormals |
                PostProcessSteps.FlipUVs |
                PostProcessSteps.JoinIdenticalVertices
            );


            Console.WriteLine($"Meshes: {scene.MeshCount}");
            Console.WriteLine($"Materials: {scene.MaterialCount}");
            Console.WriteLine($"Textures embedded: {scene.TextureCount}");


            string folder = Path.GetDirectoryName(
                Path.GetFullPath(path)
            )!;


            foreach (var mesh in scene.Meshes)
            {
                ModelMesh modelMesh = new();


                bool hasUV = mesh.HasTextureCoords(0);


                // Vertices
                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    var vertex = mesh.Vertices[i];


                    modelMesh.Vertices.Add(vertex.X);
                    modelMesh.Vertices.Add(vertex.Y);
                    modelMesh.Vertices.Add(vertex.Z);


                    if (hasUV)
                    {
                        var uv =
                            mesh.TextureCoordinateChannels[0][i];


                        modelMesh.Vertices.Add(uv.X);
                        modelMesh.Vertices.Add(uv.Y);
                    }
                    else
                    {
                        modelMesh.Vertices.Add(0);
                        modelMesh.Vertices.Add(0);
                    }
                }



                // Indices
                foreach (var face in mesh.Faces)
                {
                    foreach (var index in face.Indices)
                    {
                        modelMesh.Indices.Add((uint)index);
                    }
                }
                modelMesh.IndexCount = modelMesh.Indices.Count;


                // Texture loading
                if (mesh.MaterialIndex >= 0)
                {
                    Material material =
                        scene.Materials[mesh.MaterialIndex];


                    Console.WriteLine(
                        $"Material: {material.Name}"
                    );


                    int textureCount =
                        material.GetMaterialTextureCount(
                            TextureType.Diffuse
                        );


                    Console.WriteLine(
                        $"Diffuse textures: {textureCount}"
                    );


                    if (textureCount > 0)
                    {
                        material.GetMaterialTexture(
                            TextureType.Diffuse,
                            0,
                            out TextureSlot slot
                        );


                        string textureFile =
                            slot.FilePath;


                        // Embedded texture
                        if (textureFile.StartsWith("*"))
                        {
                            Console.WriteLine(
                                "Embedded texture found"
                            );

                            modelMesh.TexturePath =
                                textureFile;
                        }
                        else
                        {
                            textureFile =
                                textureFile.Replace("\\", "/");


                            modelMesh.TexturePath =
                                Path.GetFullPath(
                                    Path.Combine(
                                        folder,
                                        textureFile
                                    )
                                );


                            Console.WriteLine(
                                $"Texture: {modelMesh.TexturePath}"
                            );
                        }
                    }
                    else
                    {
                        Console.WriteLine(
                            "No diffuse texture"
                        );
                    }
                }



                // NEVER skip meshes
                Meshes.Add(modelMesh);


                Console.WriteLine(
                    $"Loaded mesh vertices: {mesh.VertexCount}"
                );
            }


            Console.WriteLine(
                $"Loaded {Meshes.Count} meshes"
            );
        }
    }
}