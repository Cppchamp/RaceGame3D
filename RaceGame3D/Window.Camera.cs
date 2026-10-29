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

            if (KeyboardState.IsKeyPressed(Keys.V))
            {
                CycleCamera();
            }
        }

        private void UpdateCarCamera()
        {
            float radians = MathHelper.DegreesToRadians(_carRotation);


            Vector3 forward = new Vector3(
                MathF.Sin(radians), 0f, MathF.Cos(radians)
            );

            Vector3 right = new Vector3(
                forward.Z, 0f, -forward.X
            );

            Vector3 target;


            switch (_carCameraMode)
            {
                case CarCameraMode.Chase:
                    _cameraPosition =
                        _carPosition
                        - forward * 8f
                        + Vector3.UnitY * 3f;

                    target = _carPosition + Vector3.UnitY;
                    break;


                case CarCameraMode.Side:
                    _cameraPosition =
                        _carPosition
                        + right * 8f
                        + Vector3.UnitY * 3f;

                    target = _carPosition + Vector3.UnitY;
                    break;


                case CarCameraMode.Front:
                    _cameraPosition =
                        _carPosition
                        + forward * 8f
                        + Vector3.UnitY * 3f;

                    target = _carPosition + Vector3.UnitY;
                    break;


                case CarCameraMode.Hood:
                    // Put camera on hood
                    _cameraPosition =
                        _carPosition
                        + forward * 1.5f
                        + Vector3.UnitY * 1.2f;

                    // Look forward, not at car
                    target =
                        _cameraPosition
                        + forward * 20f
                        + Vector3.UnitY * 0.2f;

                    break;


                default:
                    target = _carPosition;
                    break;
            }

            _cameraPosition += right * _cameraDriftOffset;
            target += right * (_cameraDriftOffset * 1f);

            // VERY LIGHT SCREEN SHAKE
            if (_cameraShake > 0.001f)
            {
                float horizontalShake =
                    ((float)_cameraRandom.NextDouble() * 2f - 1f)
                    * _cameraShake;

                float verticalShake =
                    ((float)_cameraRandom.NextDouble() * 2f - 1f)
                    * _cameraShake * 0.08f;

                Vector3 shake = right * horizontalShake
                    + Vector3.UnitY * verticalShake;

                _cameraPosition += shake;
                target += shake;
            }

            _cameraFront = Vector3.Normalize(target - _cameraPosition);

            _view = Matrix4.LookAt(
                _cameraPosition,
                target,
                _cameraUp
            );


        }

        private void CycleCamera()
        {
            _carCameraMode++;

            if (_carCameraMode > CarCameraMode.Hood)
                _carCameraMode = CarCameraMode.Chase;
        }
    }
}
