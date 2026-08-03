using Assimp;

namespace RaceGame3D
{
    internal class ModelMesh
    {
        public List<float> Vertices = new();
        public List<uint> Indices = new();

        public string TexturePath = "";
    }

    internal class ModelLoader
    {
        public List<ModelMesh> Meshes = new();

        public void Load(string path)
        {
            Meshes.Clear();

            AssimpContext importer = new();

            Scene scene = importer.ImportFile(
                path,
                PostProcessSteps.Triangulate |
                PostProcessSteps.GenerateNormals |
                PostProcessSteps.FlipWindingOrder
            );

            Console.WriteLine($"Materials: {scene.MaterialCount}");

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
                    var v = mesh.Vertices[i];

                    modelMesh.Vertices.Add(v.X);
                    modelMesh.Vertices.Add(v.Y);
                    modelMesh.Vertices.Add(v.Z);

                    if (hasUV)
                    {
                        var uv = mesh.TextureCoordinateChannels[0][i];

                        modelMesh.Vertices.Add(uv.X);
                        modelMesh.Vertices.Add(1.0f - uv.Y);
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

                // Texture
                Material material = scene.Materials[mesh.MaterialIndex];

                if (material.GetMaterialTextureCount(TextureType.Diffuse) > 0)
                {
                    material.GetMaterialTexture(
                        TextureType.Diffuse,
                        0,
                        out TextureSlot slot
                    );

                    modelMesh.TexturePath = Path.Combine(folder, slot.FilePath);

                    Console.WriteLine($"Mesh texture: {slot.FilePath}");
                }

                if (!string.IsNullOrWhiteSpace(modelMesh.TexturePath) && File.Exists(modelMesh.TexturePath))
                {
                    Meshes.Add(modelMesh);

                    Console.WriteLine(
                        $"Keeping: {Path.GetFileName(modelMesh.TexturePath)}"
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"Skipping mesh (no texture)"
                    );
                }
            }

            Console.WriteLine($"Loaded {Meshes.Count} meshes");
        }
    }
}