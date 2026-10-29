using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace RaceGame3D
{
    public partial class Window : GameWindow
    {
        // PROCEDURAL TRACK
        private readonly List<Vector3> _trackPoints = new();

        private int _trackVao;
        private int _trackVbo;
        private int _trackEbo;
        private int _trackIndexCount;
        private int _trackTexture;

        private int _groundVao;
        private int _groundVbo;
        private int _groundEbo;
        private int _groundIndexCount;
        private int _groundTexture;

        private int _mountainVao;
        private int _mountainVbo;
        private int _mountainEbo;
        private int _mountainIndexCount;
        private int _mountainTexture;

        private const float TrackHalfWidth = 6f;

        // Change this seed to generate a different track.
        private const int TrackSeed = 5;
        //--------------HUD------------------------
        private int _speedometerTexture;
        private int _tachometerTexture;
        private int _needleTexture;
        private int _hudShader;

        private int _hudVao;
        private int _hudVbo;

        private bool _hudInitialized;

        // --------------------------
        private List<int> _vaos = new();
        private List<int> _vbos = new();
        private List<int> _ebos = new();
        private List<int> _carVaos = new();
        private List<int> _carVbos = new();
        private List<int> _carEbos = new();
        private int _skyboxVAO;
        private int _skyboxVBO;

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
        private Shader _skyboxShader;

        private Vector3 _cameraPosition = new Vector3(0, 10, 30);
        private Vector3 _cameraFront = -Vector3.UnitZ;
        private Vector3 _cameraUp = Vector3.UnitY;

        private float _cameraSpeed = 30f;
        private float _mouseSensitivity = 0.2f;

        // Camera effects
        private float _cameraDriftOffset = 0f;
        private float _cameraShake = 0f;
        private readonly Random _cameraRandom = new Random();

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

        private List<int> _wheelMeshes = new();
        private List<int> _frontWheels = new();

        private float _wheelRotation = 0f;
        private float _wheelSteer = 0f;

        private int _skyTexture;

        enum CarCameraMode
        {
            Chase,
            Side,
            Front,
            Hood
        }

        private CarCameraMode _carCameraMode = CarCameraMode.Chase;


        public Window(
            GameWindowSettings gameWindowSettings,
            NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }




        protected override void OnLoad()
        {
            base.OnLoad();

            CursorState = OpenTK.Windowing.Common.CursorState.Grabbed;

            GL.Enable(EnableCap.DepthTest);

            // Load OBJ + MTL + textures
            building = new ModelLoader();

            building.Load(
                "Assets/Models/Camellia_City.obj"
            );

            _skyTexture = LoadTexture("Assets/Background/skybox.png");

            CreateSkybox();

            _skyboxShader = new Shader(
                "Shaders/skybox.vert",
                "Shaders/skybox.frag"
            );

            CreateHUDMesh();

            _speedometerTexture = LoadTexture("Assets/HUD/speedometer.png");
            _tachometerTexture  = LoadTexture("Assets/HUD/tachometer.png");
            _needleTexture      = LoadTexture("Assets/HUD/needleshort.png");

            car = new ModelLoader();

            car.Load(
                "Assets/Models/ghini.obj"
            );

            for (int i = 0; i < car.Meshes.Count; i++)
            {
                string name = car.Meshes[i].Name.ToLower();

                Console.WriteLine($"{i}: {name}");


                if (name.Contains("wheels") ||
                    name == "texture")
                {
                    _wheelMeshes.Add(i);
                }


                // front left + right wheels AND their tire texture meshes
                if (i == 3 || i == 4 ||

                    i == 9 || i == 10)
                {
                    _frontWheels.Add(i);

                    Console.WriteLine(
                        $"STEERING WHEEL: {i} {name}"
                    );
                }
            }

            _shader = new Shader(
                "Shaders/shader.vert",
                "Shaders/shader.frag"
            );

            _shader.Use();
            _shader.SetInt("texture0", 0);

            GenerateMountainTrack();

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

            CreateParticleMesh();

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

            // PROCEDURAL TRACK CLEANUP
            GL.DeleteVertexArray(_trackVao);
            GL.DeleteBuffer(_trackVbo);
            GL.DeleteBuffer(_trackEbo);
            GL.DeleteTexture(_trackTexture);

            GL.DeleteVertexArray(_groundVao);
            GL.DeleteBuffer(_groundVbo);
            GL.DeleteBuffer(_groundEbo);
            GL.DeleteTexture(_groundTexture);

            GL.DeleteVertexArray(_mountainVao);
            GL.DeleteBuffer(_mountainVbo);
            GL.DeleteBuffer(_mountainEbo);
            GL.DeleteTexture(_mountainTexture);

            base.OnUnload();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            // WHEEL SPIN BASED ON SPEED
            float wheelRadius = 0.35f;

            // real speed of the car
            float wheelSpeed =
                _carVelocityVector.Length;

            // keep direction when reversing
            float direction =
                MathF.Sign(
                    Vector3.Dot(
                        _carVelocityVector,
                        new Vector3(
                            MathF.Sin(MathHelper.DegreesToRadians(_carRotation)),
                            0,
                            MathF.Cos(MathHelper.DegreesToRadians(_carRotation))
                        )
                    )
                );

            _wheelRotation +=
                (wheelSpeed * direction / wheelRadius)
                * (float)e.Time;


            // prevent huge values
            if (_wheelRotation > MathF.PI * 2)
                _wheelRotation -= MathF.PI * 2;

            if (_wheelRotation < -MathF.PI * 2)
                _wheelRotation += MathF.PI * 2;

            UpdateControls((float)e.Time);
            UpdateHUD((float)e.Time);
        }
    }
}
