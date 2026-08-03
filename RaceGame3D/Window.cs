using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;

namespace RaceGame3D
{
    public class Window : GameWindow
    {
        private int _vao;
        private int _vbo;
        private int _texture;

        private ModelLoader building;

        private Matrix4 _model;
        private Matrix4 _view;
        private Matrix4 _projection;

        private Shader _shader;

        private Vector3 _cameraPosition = new Vector3(0, 10, 30);
        private Vector3 _cameraFront = -Vector3.UnitZ;
        private Vector3 _cameraUp = Vector3.UnitY;

        private float _cameraSpeed = 10f;
        private float _mouseSensitivity = 0.2f;

        private float _yaw = -90f;
        private float _pitch = -15f;

        private float _fov = 60f;

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
                "Assets/Models/building_G.obj"
            );

            _texture = LoadTexture(
                building.TexturePath
            );

            _shader = new Shader(
                "Shaders/shader.vert",
                "Shaders/shader.frag"
            );


            // Create VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);


            // Create VBO
            _vbo = GL.GenBuffer();

            GL.BindBuffer(
                BufferTarget.ArrayBuffer,
                _vbo
            );


            // Upload vertices
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                building.Vertices.Count * sizeof(float),
                building.Vertices.ToArray(),
                BufferUsageHint.StaticDraw
            );


            /*
             Vertex format:

             Position:
             x y z

             UV:
             u v

             Total:
             5 floats
            */


            // Position attribute
            GL.VertexAttribPointer(
                0,
                3,
                VertexAttribPointerType.Float,
                false,
                5 * sizeof(float),
                0
            );

            GL.EnableVertexAttribArray(0);



            // Texture coordinates
            GL.VertexAttribPointer(
                1,
                2,
                VertexAttribPointerType.Float,
                false,
                5 * sizeof(float),
                3 * sizeof(float)
            );

            GL.EnableVertexAttribArray(1);



            // Camera transforms
            _model = Matrix4.Identity;


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


            Console.WriteLine(
                $"Vertices: {building.Vertices.Count / 5}"
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
                $"Loaded texture: {path}"
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
                "model",
                _model
            );

            _shader.SetMatrix4(
                "view",
                _view
            );

            _shader.SetMatrix4(
                "projection",
                _projection
            );


            GL.BindVertexArray(_vao);

            GL.ActiveTexture(TextureUnit.Texture0);

            GL.BindTexture(
                TextureTarget.Texture2D,
                _texture
            );

            GL.DrawArrays(
                PrimitiveType.Triangles,
                0,
                building.Vertices.Count / 5
            );


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
            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);

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