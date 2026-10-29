using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RaceGame3D
{
    public partial class Window : GameWindow
    {
        private int LoadTexture(string path)
        {
            // fallback texture
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Console.WriteLine($"Missing texture: {path}");
                return CreateWhiteTexture();
            }


            int texture = GL.GenTexture();

            GL.BindTexture(
                TextureTarget.Texture2D,
                texture
            );


            using (FileStream stream = File.OpenRead(path))
            {
                // REMOVE flipping because Assimp already does FlipUVs

                ImageResult image =
                    ImageResult.FromStream(
                        stream,
                        ColorComponents.RedGreenBlueAlpha
                    );


                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    PixelInternalFormat.Rgba,
                    image.Width,
                    image.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    image.Data
                );
            }


            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.LinearMipmapLinear
            );


            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear
            );


            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureWrapS,
                (int)TextureWrapMode.Repeat
            );


            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureWrapT,
                (int)TextureWrapMode.Repeat
            );


            GL.GenerateMipmap(
                GenerateMipmapTarget.Texture2D
            );



            Console.WriteLine(
                $"Loaded texture: {Path.GetFileName(path)}"
            );

            return texture;
        }

        private int CreateWhiteTexture()
        {
            int texture = GL.GenTexture();

            GL.BindTexture(
                TextureTarget.Texture2D,
                texture
            );


            byte[] white =
            {
        255,255,255,255
    };


            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                1,
                1,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                white
            );


            return texture;
        }

        private static void AddVertex(
            List<float> vertices,
            Vector3 position,
            float u,
            float v)
        {
            vertices.Add(position.X);
            vertices.Add(position.Y);
            vertices.Add(position.Z);
            vertices.Add(u);
            vertices.Add(v);
        }

        private void CreateMesh(
            List<float> vertices,
            List<uint> indices,
            out int vao,
            out int vbo,
            out int ebo,
            out int indexCount)
        {
            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            ebo = GL.GenBuffer();

            GL.BindVertexArray(vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                vertices.Count * sizeof(float),
                vertices.ToArray(),
                BufferUsageHint.StaticDraw
            );

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                indices.Count * sizeof(uint),
                indices.ToArray(),
                BufferUsageHint.StaticDraw
            );

            GL.VertexAttribPointer(
                0, 3, VertexAttribPointerType.Float,
                false, 5 * sizeof(float), 0
            );
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(
                1, 2, VertexAttribPointerType.Float,
                false, 5 * sizeof(float), 3 * sizeof(float)
            );
            GL.EnableVertexAttribArray(1);

            indexCount = indices.Count;

            GL.BindVertexArray(0);
        }

        private int CreateSolidTexture(byte r, byte g, byte b)
        {
            int texture = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, texture);

            byte[] pixel = { r, g, b, 255 };

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                1,
                1,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                pixel
            );

            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Nearest
            );

            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest
            );

            GL.BindTexture(TextureTarget.Texture2D, 0);

            return texture;
        }

        private void LoadModelBuffers(
            ModelLoader model,
            List<int> vaos,
            List<int> vbos,
            List<int> ebos,
            List<int> textures)
        {
            foreach (var mesh in model.Meshes)
            {
                int vao = GL.GenVertexArray();
                int vbo = GL.GenBuffer();
                int ebo = GL.GenBuffer();


                GL.BindVertexArray(vao);


                GL.BindBuffer(
                    BufferTarget.ArrayBuffer,
                    vbo
                );

                float[] vertices = mesh.Vertices.ToArray();

                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    vertices.Length * sizeof(float),
                    vertices,
                    BufferUsageHint.StaticDraw
                );


                GL.BindBuffer(
                    BufferTarget.ElementArrayBuffer,
                    ebo
                );

                GL.BufferData(
                    BufferTarget.ElementArrayBuffer,
                    mesh.Indices.Count * sizeof(uint),
                    mesh.Indices.ToArray(),
                    BufferUsageHint.StaticDraw
                );


                GL.VertexAttribPointer(
                    0,
                    3,
                    VertexAttribPointerType.Float,
                    false,
                    5 * sizeof(float),
                    0
                );

                GL.EnableVertexAttribArray(0);


                GL.VertexAttribPointer(
                    1,
                    2,
                    VertexAttribPointerType.Float,
                    false,
                    5 * sizeof(float),
                    3 * sizeof(float)
                );

                GL.EnableVertexAttribArray(1);


                vaos.Add(vao);
                vbos.Add(vbo);
                ebos.Add(ebo);


                textures.Add(
                    LoadTextureCached(mesh.TexturePath)
                );
            }
        }

        private int LoadTextureCached(string path)
        {
            if (textureCache.ContainsKey(path))
                return textureCache[path];

            int tex = LoadTexture(path);

            textureCache[path] = tex;

            return tex;
        }

        private int LoadHUDTextures(string path)
        {
            if (textureCache.ContainsKey(path))
                return textureCache[path];

            int texHud = LoadTexture(path);
            return texHud;
        }

        private void CreateParticleMesh()
        {
            _particleVao = GL.GenVertexArray();
            _particleVbo = GL.GenBuffer();

            GL.BindVertexArray(_particleVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _particleVbo);

            GL.BufferData(
                BufferTarget.ArrayBuffer,
                6 * 5 * sizeof(float),
                IntPtr.Zero,
                BufferUsageHint.DynamicDraw
            );

            GL.VertexAttribPointer(
                0, 3, VertexAttribPointerType.Float,
                false, 5 * sizeof(float), 0
            );
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(
                1, 2, VertexAttribPointerType.Float,
                false, 5 * sizeof(float), 3 * sizeof(float)
            );
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);

            _particleTexture = CreateDustTexture();
        }

        private int CreateDustTexture()
        {
            const int size = 32;
            byte[] pixels = new byte[size * size * 4];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - size / 2f) / (size / 2f);
                    float dy = (y + 0.5f - size / 2f) / (size / 2f);

                    float distance = MathF.Sqrt(dx * dx + dy * dy);
                    float alpha = MathHelper.Clamp(1f - distance, 0f, 1f);
                    alpha *= alpha * 0.75f;

                    int i = (y * size + x) * 4;

                    pixels[i] = 190;
                    pixels[i + 1] = 177;
                    pixels[i + 2] = 145;
                    pixels[i + 3] = (byte)(alpha * 255f);
                }
            }

            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                size,
                size,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                pixels
            );

            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear
            );

            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear
            );

            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureWrapS,
                (int)TextureWrapMode.ClampToEdge
            );

            GL.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureWrapT,
                (int)TextureWrapMode.ClampToEdge
            );

            GL.BindTexture(TextureTarget.Texture2D, 0);

            return texture;
        }

        private void CreateHUDMesh()
        {
            _hudVao = GL.GenVertexArray();
            _hudVbo = GL.GenBuffer();

            GL.BindVertexArray(_hudVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _hudVbo);

            // Four vertices: x, y, u, v
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                4 * 4 * sizeof(float),
                IntPtr.Zero,
                BufferUsageHint.DynamicDraw
            );

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(
                0, 2, VertexAttribPointerType.Float,
                false, 4 * sizeof(float), 0
            );

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(
                1, 2, VertexAttribPointerType.Float,
                false, 4 * sizeof(float), 2 * sizeof(float)
            );

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            string vertexSource =
            @"#version 330 core
            layout (location = 0) in vec2 aPosition;
            layout (location = 1) in vec2 aTexCoord;

            out vec2 TexCoord;

            void main()
            {
                gl_Position = vec4(aPosition, 0.0, 1.0);
                TexCoord = aTexCoord;
            }";

            string fragmentSource = @"#version 330 core
            in vec2 TexCoord;
            out vec4 FragColor;

            uniform sampler2D hudTexture;

            void main()
            {
                FragColor = texture(hudTexture, TexCoord);
            }";

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSource);
            GL.CompileShader(vertexShader);

            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vertexStatus);
            if (vertexStatus == 0)
                Console.WriteLine("HUD vertex shader: " + GL.GetShaderInfoLog(vertexShader));

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentSource);
            GL.CompileShader(fragmentShader);

            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fragmentStatus);
            if (fragmentStatus == 0)
                Console.WriteLine("HUD fragment shader: " + GL.GetShaderInfoLog(fragmentShader));

            _hudShader = GL.CreateProgram();
            GL.AttachShader(_hudShader, vertexShader);
            GL.AttachShader(_hudShader, fragmentShader);
            GL.LinkProgram(_hudShader);

            GL.GetProgram(_hudShader, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus == 0)
                Console.WriteLine("HUD shader link: " + GL.GetProgramInfoLog(_hudShader));

            GL.DetachShader(_hudShader, vertexShader);
            GL.DetachShader(_hudShader, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            GL.UseProgram(_hudShader);
            GL.Uniform1(GL.GetUniformLocation(_hudShader, "hudTexture"), 0);
            GL.UseProgram(0);

            _hudInitialized = true;
        }

    }

}
