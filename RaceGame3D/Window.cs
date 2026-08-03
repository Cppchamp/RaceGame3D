using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using System.Text.RegularExpressions;

namespace RaceGame3D
{
    public class Window : GameWindow
    {
        private List<int> _vaos = new();
        private List<int> _vbos = new();
        private List<int> _ebos = new();

        private List<int> _textures = new();

        private ModelLoader building;
        private ModelLoader car;

        private Matrix4 _model;
        private Matrix4 _model_car;
        private Matrix4 _view;
        private Matrix4 _projection;

        private Shader _shader;

        private Vector3 _cameraPosition = new Vector3(0, 10, 30);
        private Vector3 _cameraFront = -Vector3.UnitZ;
        private Vector3 _cameraUp = Vector3.UnitY;

        private float _cameraSpeed = 30f;
        private float _mouseSensitivity = 0.2f;

        private float _yaw = -90f;
        private float _pitch = -15f;

        private float _fov = 90f;

        private Vector2 _lastMousePosition;
        private bool _firstMouseMove = true;


        public Window(
            GameWindowSettings gameWindowSettings,
            NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }


        protected override void OnLoad()
        {
            base.OnLoad();

            CursorState = CursorState.Grabbed;

            GL.Enable(EnableCap.DepthTest);

            // Load OBJ + MTL + textures
            building = new ModelLoader();

            building.Load(
                "Assets/Models/Camellia_City.obj"
            );

            //car = new ModelLoader();

            //car.Load(
            //    "Assets/Models/kamaro.obj"
            //);

            _shader = new Shader(
                "Shaders/shader.vert",
                "Shaders/shader.frag"
            );

            _shader.Use();
            _shader.SetInt("texture0", 0);

            foreach (var mesh in building.Meshes)
            {
                int vao = GL.GenVertexArray();
                int vbo = GL.GenBuffer();
                int ebo = GL.GenBuffer();


                GL.BindVertexArray(vao);


                GL.BindBuffer(
                    BufferTarget.ArrayBuffer,
                    vbo
                );


                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    mesh.Vertices.Count * sizeof(float),
                    mesh.Vertices.ToArray(),
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



                _vaos.Add(vao);
                _vbos.Add(vbo);
                _ebos.Add(ebo);



                _textures.Add(
                    LoadTexture(mesh.TexturePath)
                );
            }
            GL.EnableVertexAttribArray(1);



            // Camera transforms
            // _model_car = Matrix4.CreateScale(1.00f);
            _model = Matrix4.CreateScale(1.01f);

        _view = Matrix4.LookAt(
                _cameraPosition,
                _cameraPosition + _cameraFront,
                _cameraUp
            );


            _projection =
                Matrix4.CreatePerspectiveFieldOfView(
                    MathHelper.DegreesToRadians(60),
                    Size.X / (float)Size.Y,
                    0.1f,
                    1000f
                );

        }

        private int LoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("No texture found");
                return 0;
            }


            if (!File.Exists(path))
            {
                Console.WriteLine(
                    $"Texture missing: {path}"
                );

                return 0;
            }


            int texture = GL.GenTexture();

            GL.BindTexture(
                TextureTarget.Texture2D,
                texture
            );


            using (FileStream stream = File.OpenRead(path))
            {
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


            GL.GenerateMipmap(
                GenerateMipmapTarget.Texture2D
            );


            GL.TextureParameter(
                (int)TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.LinearMipmapLinear
            );

            GL.TextureParameter(
                (int)TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear
            );




            Console.WriteLine(
                $"Loaded texture: {Regex.Replace(path, @"^.*[\\/]", "")}"
            );


            return texture;
        }


        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            // Clear FIRST
            GL.ClearColor(
                0.2f,
                0.3f,
                0.4f,
                1f
            );

            GL.Clear(
                ClearBufferMask.ColorBufferBit |
                ClearBufferMask.DepthBufferBit
            );


            // Use shader
            _shader.Use();

            _shader.SetMatrix4(
                "_model",
                _model
            );

            //_shader.SetMatrix4(
            //    "model_car",
            //    _model_car
            //);

            _shader.SetMatrix4(
                "view",
                _view
            );

            _shader.SetMatrix4(
                "projection",
                _projection
            );


            for (int i = 0; i < building.Meshes.Count; i++)
            {
                GL.BindVertexArray(
                    _vaos[i]
                );


                GL.ActiveTexture(
                    TextureUnit.Texture0
                );


                GL.BindTexture(
                    TextureTarget.Texture2D,
                    _textures[i]
                );


                GL.DrawElements(
                    PrimitiveType.Triangles,
                    building.Meshes[i].Indices.Count,
                    DrawElementsType.UnsignedInt,
                    0
                );
            }

            for (int i = 0; i < building.Meshes.Count; i++)
            {
                GL.BindTexture(
                    TextureTarget.Texture2D,
                    _textures[i]
                );


                GL.DrawElements(
                    PrimitiveType.Triangles,
                    building.Meshes[i].Indices.Count,
                    DrawElementsType.UnsignedInt,
                    0
                );
            }


            SwapBuffers();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(
                0,
                0,
                Size.X,
                Size.Y
            );
        }

        protected override void OnUnload()
        {
            foreach (var vbo in _vbos)
                GL.DeleteBuffer(vbo);


            foreach (var ebo in _ebos)
                GL.DeleteBuffer(ebo);


            foreach (var vao in _vaos)
                GL.DeleteVertexArray(vao);


            foreach (var tex in _textures)
                GL.DeleteTexture(tex);

            base.OnUnload();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            UpdateCamera((float)e.Time);
        }
        
        private void UpdateCamera(float deltaTime)
        {
            float speed = _cameraSpeed * deltaTime;

            if (KeyboardState.IsKeyDown(Keys.W))
                _cameraPosition += _cameraFront * speed;

            if (KeyboardState.IsKeyDown(Keys.S))
                _cameraPosition -= _cameraFront * speed;


            Vector3 right = Vector3.Normalize(
                Vector3.Cross(_cameraFront, _cameraUp)
            );


            if (KeyboardState.IsKeyDown(Keys.A))
                _cameraPosition -= right * speed;

            if (KeyboardState.IsKeyDown(Keys.D))
                _cameraPosition += right * speed;

            if (KeyboardState.IsKeyPressed(Keys.P))
            {
                Console.WriteLine(
                    $"Camera Position: {_cameraPosition}"
                );

                Console.WriteLine(
                    $"Camera Direction: {_cameraFront}"
                );
            }


            if (KeyboardState.IsKeyDown(Keys.Space))
                _cameraPosition += _cameraUp * speed;

            if (KeyboardState.IsKeyDown(Keys.LeftControl))
                _cameraPosition -= _cameraUp * speed;


            _view = Matrix4.LookAt(
                _cameraPosition,
                _cameraPosition + _cameraFront,
                _cameraUp
            );

        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);


            if (_firstMouseMove)
            {
                _lastMousePosition = e.Position;
                _firstMouseMove = false;
            }


            float deltaX = e.Position.X - _lastMousePosition.X;
            float deltaY = _lastMousePosition.Y - e.Position.Y;


            _lastMousePosition = e.Position;


            deltaX *= _mouseSensitivity;
            deltaY *= _mouseSensitivity;


            _yaw += deltaX;
            _pitch += deltaY;


            _pitch = MathHelper.Clamp(
                _pitch,
                -89f,
                89f
            );


            Vector3 direction;

            direction.X =
                MathF.Cos(MathHelper.DegreesToRadians(_yaw)) *
                MathF.Cos(MathHelper.DegreesToRadians(_pitch));

            direction.Y =
                MathF.Sin(MathHelper.DegreesToRadians(_pitch));

            direction.Z =
                MathF.Sin(MathHelper.DegreesToRadians(_yaw)) *
                MathF.Cos(MathHelper.DegreesToRadians(_pitch));


            _cameraFront = Vector3.Normalize(direction);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);


            _fov -= e.OffsetY * 2f;


            _fov = MathHelper.Clamp(
                _fov,
                20f,
                90f
            );


            _projection =
                Matrix4.CreatePerspectiveFieldOfView(
                    MathHelper.DegreesToRadians(_fov),
                    Size.X / (float)Size.Y,
                    0.1f,
                    1000f
                );
        }
    }

}