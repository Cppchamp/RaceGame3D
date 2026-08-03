using Assimp;

namespace RaceGame3D
{
    internal class ModelLoader
    {
        public List<float> Vertices = new();

        public string TexturePath = "";


        public void Load(string path)
        {
            AssimpContext importer = new();


            Scene scene = importer.ImportFile(
                path,
                PostProcessSteps.Triangulate |
                PostProcessSteps.GenerateNormals |
                PostProcessSteps.FlipUVs
            );
            TexturePath = FindTextureInMtl(path);

            string modelDirectory =
                Path.GetDirectoryName(path)!;


            Console.WriteLine(
                $"Materials: {scene.MaterialCount}"
            );

            foreach (var material in scene.Materials)
            {
                Console.WriteLine(
                    $"Material: {material.Name}"
                );

                int textureCount = material.GetMaterialTextureCount(
                    TextureType.Diffuse
                );

                Console.WriteLine(
                    $"Diffuse textures: {textureCount}"
                );


                if (textureCount > 0)
                {
                    TextureSlot textureSlot;

                    material.GetMaterialTexture(
                        TextureType.Diffuse,
                        0,
                        out textureSlot
                    );


                    TexturePath = Path.Combine(
                        Path.GetDirectoryName(path)!,
                        textureSlot.FilePath
                    );


                    Console.WriteLine(
                        $"Texture: {TexturePath}"
                    );


                    Console.WriteLine(
                        $"Exists: {File.Exists(TexturePath)}"
                    );
                }
            }

            // Read mesh data
            foreach (var mesh in scene.Meshes)
            {
                Console.WriteLine(
                    $"Mesh material: {mesh.MaterialIndex}"
                );


                bool hasUV =
                    mesh.HasTextureCoords(0);


                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    var vertex =
                        mesh.Vertices[i];


                    // Position
                    Vertices.Add(vertex.X);
                    Vertices.Add(vertex.Y);
                    Vertices.Add(vertex.Z);



                    // UV
                    if (hasUV)
                    {
                        var uv =
                            mesh.TextureCoordinateChannels[0][i];


                        Vertices.Add(uv.X);
                        Vertices.Add(uv.Y);
                    }
                    else
                    {
                        Vertices.Add(0);
                        Vertices.Add(0);
                    }
                }
            }


            Console.WriteLine(
                $"Loaded {Vertices.Count / 5} vertices"
            );
        }

        private string FindTextureInMtl(string objPath)
        {
            string folder = Path.GetDirectoryName(
                Path.GetFullPath(objPath)
            )!;


            string mtlFile = "";


            // Find MTL name from OBJ
            foreach (string line in File.ReadLines(objPath))
            {
                if (line.StartsWith("mtllib"))
                {
                    mtlFile = line.Substring(7).Trim();
                    break;
                }
            }


            if (string.IsNullOrEmpty(mtlFile))
            {
                Console.WriteLine("No mtllib in OBJ");
                return "";
            }


            string mtlPath = Path.Combine(
                folder,
                mtlFile
            );


            Console.WriteLine(
                $"MTL path: {mtlPath}"
            );


            if (!File.Exists(mtlPath))
            {
                Console.WriteLine("MTL missing");
                return "";
            }



            foreach (string line in File.ReadLines(mtlPath))
            {
                string cleanLine = line.Trim();


                if (cleanLine.StartsWith("map_Kd"))
                {
                    string textureName =
                        cleanLine.Substring(6).Trim();


                    string texturePath =
                        Path.Combine(
                            folder,
                            textureName
                        );


                    Console.WriteLine(
                        $"Texture path: {texturePath}"
                    );


                    Console.WriteLine(
                        $"Texture exists: {File.Exists(texturePath)}"
                    );


                    return texturePath;
                }
            }


            Console.WriteLine("map_Kd not found");

            return "";
        }
    }
}