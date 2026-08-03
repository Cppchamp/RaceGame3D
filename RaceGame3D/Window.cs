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
    public class Window : GameWindow
    {
        private List<int> _vaos = new();
        private List<int> _vbos = new();
        private List<int> _ebos = new();
        private List<int> _carVaos = new();
        private List<int> _carVbos = new();
        private List<int> _carEbos = new();

        private List<int> _textures = new();
        private List<int> _carTextures = new();

        private Dictionary<string, int> textureCache = new();


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

        private enum CameraMode
        {
            Free,
            Car
        }

        private CameraMode _cameraMode = CameraMode.Free;

        private float _carVelocity = 0f;

        private float _carMaxSpeed = 45f;
        private float _carReverseSpeed = 10f;

        private float _carAcceleration = 15f;

        private float _carRotation = 0f;

        private float _grip = 12f;
        private float _driftGrip = 1.5f;

        private Vector3 _carPosition = new Vector3(0, 2, -10);

        private Vector3 _carVelocityVector = Vector3.Zero;


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

            car = new ModelLoader();

            car.Load(
                "Assets/Models/ghini.obj"
            );

            _shader = new Shader(
                "Shaders/shader.vert",
                "Shaders/shader.frag"
            );

            _shader.Use();
            _shader.SetInt("texture0", 0);

            LoadModelBuffers(
                building,
                _vaos,
                _vbos,
                _ebos,
                _textures
            );


            LoadModelBuffers(
                car,
                _carVaos,
                _carVbos,
                _carEbos,
                _carTextures
            );

            foreach (var mesh in building.Meshes)
            {
                mesh.Vertices.Clear();
                mesh.Vertices.TrimExcess();

                mesh.Indices.Clear();
                mesh.Indices.TrimExcess();
            }

            foreach (var mesh in car.Meshes)
            {
                mesh.Vertices.Clear();
                mesh.Vertices.TrimExcess();

                mesh.Indices.Clear();
                mesh.Indices.TrimExcess();
            }

            //GC.Collect();
            //GC.WaitForPendingFinalizers();

            GL.EnableVertexAttribArray(1);



            // Camera transforms
            _model = Matrix4.CreateScale(0.01f);

            UpdateCarMatrix();

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
                    building.Meshes[i].IndexCount,
                    DrawElementsType.UnsignedInt,
                    0
                );
            }

            _shader.SetMatrix4(
                "_model",
                _model_car
            );


            for (int i = 0; i < car.Meshes.Count; i++)
            {
                GL.BindVertexArray(
                    _carVaos[i]
                );


                GL.ActiveTexture(
                    TextureUnit.Texture0
                );


                GL.BindTexture(
                    TextureTarget.Texture2D,
                    _carTextures[i]
                );


                GL.DrawElements(
                    PrimitiveType.Triangles,
                    car.Meshes[i].IndexCount,
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

            foreach (var vbo in _carVbos)
                GL.DeleteBuffer(vbo);


            foreach (var ebo in _carEbos)
                GL.DeleteBuffer(ebo);


            foreach (var vao in _carVaos)
                GL.DeleteVertexArray(vao);


            foreach (var tex in _carTextures)
                GL.DeleteTexture(tex);

            base.OnUnload();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            UpdateControls((float)e.Time);
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

        private void UpdateCar(float deltaTime)
        {
            bool drifting = KeyboardState.IsKeyDown(Keys.Space);


            // INPUT
            float throttle = 0;

            if (KeyboardState.IsKeyDown(Keys.W))
                throttle = 1;

            if (KeyboardState.IsKeyDown(Keys.S))
                throttle = -1;


            float steering = 0;

            if (KeyboardState.IsKeyDown(Keys.A))
                steering = 1;

            if (KeyboardState.IsKeyDown(Keys.D))
                steering = -1;



            // ENGINE
            _carVelocity +=
                throttle *
                _carAcceleration *
                deltaTime;



            // LIMIT SPEED
            _carVelocity =
                MathHelper.Clamp(
                    _carVelocity,
                    -_carReverseSpeed,
                    _carMaxSpeed
                );



            // DIRECTION
            float angle =
                MathHelper.DegreesToRadians(_carRotation);


            Vector3 forward = new Vector3(
                MathF.Sin(angle),
                0,
                MathF.Cos(angle)
            );


            Vector3 right = new Vector3(
                MathF.Cos(angle),
                0,
                -MathF.Sin(angle)
            );



            // CURRENT VELOCITY COMPONENTS

            float forwardSpeed =
                Vector3.Dot(
                    _carVelocityVector,
                    forward
                );


            float sidewaysSpeed =
                Vector3.Dot(
                    _carVelocityVector,
                    right
                );



            // STEERING
            float speedFactor =
                MathHelper.Clamp(
                    MathF.Abs(forwardSpeed) / _carMaxSpeed,
                    0,
                    1
                );


            float steeringStrength =
                drifting ? 55f : 35f;


            _carRotation +=
                steering *
                steeringStrength *
                speedFactor *
                deltaTime;



            // REBUILD DIRECTIONS AFTER TURN

            angle =
                MathHelper.DegreesToRadians(_carRotation);


            forward = new Vector3(
                MathF.Sin(angle),
                0,
                MathF.Cos(angle)
            );


            right = new Vector3(
                MathF.Cos(angle),
                0,
                -MathF.Sin(angle)
            );



            // GRIP SYSTEM

            float grip;

            if (drifting)
                grip = 2.0f;       // loose rear tires
            else
                grip = 12.0f;      // normal tires



            sidewaysSpeed =
                MathHelper.Lerp(
                    sidewaysSpeed,
                    0,
                    grip * deltaTime
                );



            // DRIFT LOSSES

            if (drifting)
            {
                _carVelocity *= 0.985f; // handbrake drag
            }



            // CREATE FINAL VELOCITY

            _carVelocityVector =
                forward * _carVelocity +
                right * sidewaysSpeed;



            // NATURAL FRICTION

            if (MathF.Abs(throttle) < 0.1f)
            {
                _carVelocity *=
                    MathF.Pow(0.15f, deltaTime);
            }



            // STOP MICRO MOVEMENT

            if (_carVelocity < 0.05f &&
                _carVelocity > -0.05f)
            {
                _carVelocity = 0;
            }



            _carPosition +=
                _carVelocityVector *
                deltaTime;



            UpdateCarMatrix();
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

        private void UpdateCarMatrix()
        {
            _model_car =
                Matrix4.CreateScale(1.0f) *
                Matrix4.CreateRotationY(
                    MathHelper.DegreesToRadians(_carRotation)
                ) *
                Matrix4.CreateTranslation(
                    _carPosition
                );
        }

        private void UpdateControls(float deltaTime)
        {
            if (KeyboardState.IsKeyPressed(Keys.C))
            {
                if (_cameraMode == CameraMode.Free)
                {
                    _cameraMode = CameraMode.Car;
                    Console.WriteLine("Car camera");
                }
                else
                {
                    _cameraMode = CameraMode.Free;
                    Console.WriteLine("Free camera");
                }
            }


            if (_cameraMode == CameraMode.Free)
            {
                UpdateCamera(deltaTime);
            }
            else
            {
                UpdateCar(deltaTime);
                UpdateCarCamera();
            }
        }

        private void UpdateCarCamera()
        {
            Vector3 forward = new Vector3(
                MathF.Sin(MathHelper.DegreesToRadians(_carRotation)),
                0,
                MathF.Cos(MathHelper.DegreesToRadians(_carRotation))
            );


            // Camera distance behind car
            float distance = 8f;

            // Camera height
            float height = 3.0f;


            _cameraPosition =
                _carPosition
                - forward * distance
                + Vector3.UnitY * height;


            _cameraFront =
                Vector3.Normalize(
                    (_carPosition + Vector3.UnitY * 1.0f)
                    - _cameraPosition
                );


            _view = Matrix4.LookAt(
                _cameraPosition,
                _cameraPosition + _cameraFront,
                _cameraUp
            );
        }
        
        private int LoadTextureCached(string path)
        {
            if (textureCache.ContainsKey(path))
                return textureCache[path];

            int tex = LoadTexture(path);

            textureCache[path] = tex;

            return tex;
        }
    }

}