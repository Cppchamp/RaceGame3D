using Assimp;
using OpenTK.Mathematics;
using System.Threading.Channels;

namespace RaceGame3D
{
    internal class ModelMesh
    {
        public List<float> Vertices = new();
        public List<uint> Indices = new();

        public string TexturePath = "";
        public string Name = "";
        public int IndexCount;

        public Vector3 Pivot = Vector3.Zero;

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

                modelMesh.Name = mesh.Name;

                Console.WriteLine(
                    $"IMPORTING MESH: {mesh.Name}"
                );

                bool hasUV = mesh.HasTextureCoords(0);

                Vector3 center = Vector3.Zero;

                // Vertices
                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    var vertex = mesh.Vertices[i];

                    center += new Vector3(
                        vertex.X,
                        vertex.Y,
                        vertex.Z
                    );


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

                modelMesh.Pivot =
                    center / mesh.VertexCount;

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