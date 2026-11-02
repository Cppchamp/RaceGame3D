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

        //DRIFT PARTICLES
        private struct DriftParticle
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public float Life;
            public float MaxLife;
            public float Size;
        }
        private float _displayedRpm = 900f;

        private readonly List<DriftParticle> _driftParticles = new();
        private readonly Random _particleRandom = new();

        private int _particleVao;
        private int _particleVbo;
        private int _particleTexture;

        private void UpdateCar(float deltaTime)
        {
            bool handbrake = KeyboardState.IsKeyDown(Keys.Space);

            // INPUT
            float throttle = 0f;
            if (KeyboardState.IsKeyDown(Keys.W))
                throttle += 1f;
            if (KeyboardState.IsKeyDown(Keys.S))
                throttle -= 1f;

            float steering = 0f;
            if (KeyboardState.IsKeyDown(Keys.A))
                steering += 1f;
            if (KeyboardState.IsKeyDown(Keys.D))
                steering -= 1f;

            float angle = MathHelper.DegreesToRadians(_carRotation);

            Vector3 forward = new Vector3(
                MathF.Sin(angle), 0f, MathF.Cos(angle));

            Vector3 right = new Vector3(
                MathF.Cos(angle), 0f, -MathF.Sin(angle));

            // ACTUAL SPEED AND DIRECTION
            float speed = _carVelocityVector.Length;
            float forwardSpeed = Vector3.Dot(_carVelocityVector, forward);

            float speedFactor = MathHelper.Clamp(
                speed / _carMaxSpeed, 0f, 1f);

            // SMOOTHER STEERING
            float steeringStrength = handbrake ? 92f : 42f;

            float reverseSteer = forwardSpeed < -0.5f ? -1f : 1f;

            if (speed > 0.8f)
            {
                _carRotation += steering
                    * steeringStrength
                    * speedFactor
                    * reverseSteer
                    * deltaTime;
            }

            // FRONT WHEEL VISUALS
            float targetSteer = steering * 0.55f;

            _wheelSteer = MathHelper.Lerp(
                _wheelSteer,
                targetSteer,
                MathHelper.Clamp(deltaTime * 7f, 0f, 1f));

            // UPDATE DIRECTIONS AFTER STEERING
            angle = MathHelper.DegreesToRadians(_carRotation);

            forward = new Vector3(
                MathF.Sin(angle), 0f, MathF.Cos(angle));

            right = new Vector3(
                MathF.Cos(angle), 0f, -MathF.Sin(angle));

            // ENGINE FORCE: ACCELERATE THE ACTUAL CAR VELOCITY
            if (throttle != 0f)
            {
                float acceleration = _carAcceleration;

                // Brake more strongly when moving against the input.
                float currentForwardSpeed =
                    Vector3.Dot(_carVelocityVector, forward);

                if ((throttle < 0f && currentForwardSpeed > 0.5f) ||
                    (throttle > 0f && currentForwardSpeed < -0.5f))
                {
                    acceleration = 24f;
                }

                _carVelocityVector +=
                    forward * throttle * acceleration * deltaTime;
            }

            // SPEED LIMIT
            float maxSpeed = throttle < 0f &&
                             Vector3.Dot(_carVelocityVector, forward) < 0f
                ? _carReverseSpeed
                : _carMaxSpeed;

            float currentSpeed = _carVelocityVector.Length;

            if (currentSpeed > maxSpeed && currentSpeed > 0.001f)
            {
                _carVelocityVector *= maxSpeed / currentSpeed;
            }

            // SPLIT MOVEMENT INTO FORWARD AND SIDEWAYS MOTION
            forwardSpeed = Vector3.Dot(_carVelocityVector, forward);
            float sidewaysSpeed = Vector3.Dot(_carVelocityVector, right);

            // NORMAL GRIP RECOVERS CONTROL; HANDBRAKE ALLOWS SLIDING
            float lateralGrip = handbrake ? 0.35f : 5.5f;

            sidewaysSpeed *= MathF.Exp(-lateralGrip * deltaTime);

            // REBUILD VELOCITY, KEEPING THE CAR'S SIDEWAYS MOMENTUM
            _carVelocityVector =
                forward * forwardSpeed +
                right * sidewaysSpeed;

            // HANDBRAKE DRAG
            if (handbrake)
            {
                _carVelocityVector *= MathF.Exp(-1.1f * deltaTime);

                // Force the car to stop once it is moving very slowly.
                if (_carVelocityVector.Length < 0.35f)
                {
                    _carVelocityVector = Vector3.Zero;
                    _carVelocity = 0f;
                }
            }

            // CAMERA SHIFTS SIDEWAYS DURING A HANDBRAKE DRIFT
            float targetDriftOffset = handbrake
                ? steering * 0.45f
                : 0f;

            _cameraDriftOffset +=
                (targetDriftOffset - _cameraDriftOffset)
                * MathF.Min(1f, 3f * deltaTime);

            // SUBTLE SHAKE: MOSTLY HORIZONTAL, VERY LITTLE VERTICAL
            float targetShake =
                handbrake && _carVelocityVector.Length > 2f
                    ? 0.025f
                    : 0f;

            _cameraShake +=
                (targetShake - _cameraShake)
                * MathF.Min(1f, 6f * deltaTime);


            // NATURAL COASTING
            if (MathF.Abs(throttle) < 0.01f && !handbrake)
            {
                _carVelocityVector *=
                    MathF.Exp(-0.45f * deltaTime);
            }

            // KEEP THE SPEED VALUE SYNCHRONIZED
            _carVelocity = Vector3.Dot(
                _carVelocityVector, forward);

            if (_carVelocityVector.LengthSquared < 0.0025f)
            {
                _carVelocityVector = Vector3.Zero;
                _carVelocity = 0f;
            }

            //MOVE CAR
            _carPosition += _carVelocityVector * deltaTime;

            // Keep the car above the terrain.
            float groundHeight = GetTerrainHeight(
                _carPosition.X,
                _carPosition.Z);

            _carPosition.Y = groundHeight + 0.3f;

            UpdateCarMatrix();
            UpdateDriftParticles(deltaTime);
        }

        private void UpdateCarPitch(float deltaTime)
        {
            Vector3 forward = new Vector3(
                MathF.Sin(MathHelper.DegreesToRadians(_carRotation)),
                0f,
                MathF.Cos(MathHelper.DegreesToRadians(_carRotation))
            );

            float sampleDistance = 2.0f;

            Vector3 ahead = _carPosition + forward * sampleDistance;
            Vector3 behind = _carPosition - forward * sampleDistance;

            float heightAhead = GetTerrainHeight(ahead.X, ahead.Z);
            float heightBehind = GetTerrainHeight(behind.X, behind.Z);

            float slope = (heightAhead - heightBehind) / (sampleDistance * 2f);

            // Positive pitch means nose up.
            _targetCarPitch = -MathF.Atan(slope);

            // Limit the visual tilt to approximately 15 degrees.
            _targetCarPitch = MathHelper.Clamp(
                _targetCarPitch,
                MathHelper.DegreesToRadians(-15f),
                MathHelper.DegreesToRadians(15f)
            );

            // Smooth the transition.
            float smooth = 1f - MathF.Exp(-5f * deltaTime);

            _carPitch += (_targetCarPitch - _carPitch) * smooth;
        }

        private void UpdateCarMatrix()
        {
            _model_car =
                Matrix4.CreateScale(1.0f) *
                Matrix4.CreateRotationX(_carPitch) *
                Matrix4.CreateRotationY(
                    MathHelper.DegreesToRadians(_carRotation)
                ) *
                Matrix4.CreateTranslation(
                    _carPosition
                );

        }

        private Matrix4 ApplyWheelTransform(
          Matrix4 carMatrix,
          int meshIndex)
        {
            Vector3 pivot = car.Meshes[meshIndex].Pivot;

            Matrix4 transform =
                Matrix4.CreateTranslation(-pivot);


            // roll wheel around axle
            if (_wheelMeshes.Contains(meshIndex))
            {
                transform *=
                    Matrix4.CreateRotationX(_wheelRotation);
            }


            // steer front wheels
            if (_frontWheels.Contains(meshIndex))
            {
                transform *=
                    Matrix4.CreateRotationY(_wheelSteer);
            }


            transform *= Matrix4.CreateTranslation(pivot);


            return transform * carMatrix;
        }

        private float GetSpeedKmh()
        {
            return _carVelocityVector.Length * 3.6f;
        }

        private void UpdateDriftParticles(float deltaTime)
        {
            bool handbrake = KeyboardState.IsKeyDown(Keys.Space);
            float speed = _carVelocityVector.Length;

            float angle = MathHelper.DegreesToRadians(_carRotation);

            Vector3 forward = new Vector3(
                MathF.Sin(angle), 0f, MathF.Cos(angle));

            Vector3 right = new Vector3(
                MathF.Cos(angle), 0f, -MathF.Sin(angle));

            // Spawn smoke at both rear tires while drifting.
            if (handbrake && speed > 2f && _driftParticles.Count < 450)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    // Randomize the spawn position around each rear tire.
                    float sideJitter = (float)(_particleRandom.NextDouble() - 0.5) * 0.45f;
                    float rearJitter = (float)(_particleRandom.NextDouble() - 0.5) * 0.5f;

                    Vector3 position =
                        _carPosition
                        - forward * (1.0f + rearJitter)
                        + right * (side * 1.0f + sideJitter);

                    position.Y = GetTerrainHeight(position.X, position.Z) + 0.25f;

                    // Spread smoke behind and sideways, with a little upward lift.
                    float backwardSpeed = 0.8f + (float)_particleRandom.NextDouble() * 1.8f;
                    float sidewaysSpeed = ((float)_particleRandom.NextDouble() - 0.5f) * 2.8f;
                    float upwardSpeed = 0.4f + (float)_particleRandom.NextDouble() * 1.2f;

                    DriftParticle particle = new DriftParticle
                    {
                        Position = position,
                        Velocity =
                            -forward * backwardSpeed
                            + right * (side * 0.3f + sidewaysSpeed)
                            + Vector3.UnitY * upwardSpeed,

                        Life = 1.0f + (float)_particleRandom.NextDouble() * 1.2f,
                        MaxLife = 2.2f,
                        Size = 0.45f + (float)_particleRandom.NextDouble() * 0.65f
                    };

                    _driftParticles.Add(particle);
                }
            }

            // Update existing smoke particles.
            for (int i = _driftParticles.Count - 1; i >= 0; i--)
            {
                DriftParticle particle = _driftParticles[i];

                particle.Life -= deltaTime;

                if (particle.Life <= 0f)
                {
                    _driftParticles.RemoveAt(i);
                    continue;
                }

                particle.Position += particle.Velocity * deltaTime;

                // Gradually slow horizontal movement.
                particle.Velocity.X *= MathF.Exp(-1.0f * deltaTime);
                particle.Velocity.Z *= MathF.Exp(-1.0f * deltaTime);

                // Smoke rises rather than falling into the road.
                particle.Velocity.Y += 0.35f * deltaTime;

                // Expand as the smoke ages.
                float age = 1f - particle.Life / particle.MaxLife;
                particle.Size += 0.35f * deltaTime;

                _driftParticles[i] = particle;
            }
        }

        private void UpdateHUD(float deltaTime)
        {
            float speedKmh = GetSpeedKmh();

            bool accelerating = KeyboardState.IsKeyDown(Keys.W)
                             || KeyboardState.IsKeyDown(Keys.Up);

            bool drifting = KeyboardState.IsKeyDown(Keys.Space)
                         && _carVelocityVector.Length > 2f;

            float targetRpm = 900f + speedKmh * 22f;

            if (accelerating)
                targetRpm += 800f;

            if (drifting)
                targetRpm += 900f;

            targetRpm = Math.Clamp(targetRpm, 900f, 5000f);

            float smoothing = 1f - MathF.Exp(-3f * deltaTime);

            _displayedRpm = MathHelper.Lerp(
                _displayedRpm,
                targetRpm,
                smoothing
            );
        }
    }
}
