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
        private void CreateSkybox()
        {
            float[] vertices =
            {
                // positions        // UV
                -1,-1,-1, 0,0,
                 1,-1,-1, 1,0,
                 1, 1,-1, 1,1,
                -1, 1,-1, 0,1,

                -1,-1, 1, 0,0,
                 1,-1, 1, 1,0,
                 1, 1, 1, 1,1,
                -1, 1, 1, 0,1
            };


            uint[] indices =
            {
                0,1,2, 2,3,0,
                4,5,6, 6,7,4,
                0,4,7, 7,3,0,
                1,5,6, 6,2,1,
                3,2,6, 6,7,3,
                0,1,5, 5,4,0
            };


            _skyboxVAO = GL.GenVertexArray();
            _skyboxVBO = GL.GenBuffer();
            int ebo = GL.GenBuffer();


            GL.BindVertexArray(_skyboxVAO);


            GL.BindBuffer(
                BufferTarget.ArrayBuffer,
                _skyboxVBO
            );

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
                indices.Length * sizeof(uint),
                indices,
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

            GL.DepthMask(false);

            _skyboxShader.Use();

            _skyboxShader.SetMatrix4(
                "view",
                _view
            );

            _skyboxShader.SetMatrix4(
                "projection",
                _projection
            );

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(false);

            GL.ActiveTexture(TextureUnit.Texture0);

            GL.BindTexture(
                TextureTarget.Texture2D,
                _skyTexture
            );


            _skyboxShader.SetInt(
                "skyTexture",
                0
            );


            GL.BindVertexArray(_skyboxVAO);

            GL.DrawElements(
                PrimitiveType.Triangles,
                36,
                DrawElementsType.UnsignedInt,
                0
            );


            GL.DepthMask(true);
            GL.DepthFunc(DepthFunction.Less);
            GL.Enable(EnableCap.DepthTest);

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


            // PROCEDURAL MOUNTAIN ENVIRONMENT
            DrawGeneratedMesh(
                _groundVao, _groundTexture, _groundIndexCount
            );

            DrawGeneratedMesh(
                _mountainVao, _mountainTexture, _mountainIndexCount
            );

            DrawGeneratedMesh(
                _trackVao, _trackTexture, _trackIndexCount
            );

            // Restore the original building model transform.
            _shader.SetMatrix4("_model", _model);


            //BUILDING MODEL

            //for (int i = 0; i < building.Meshes.Count; i++)
            //{
            //    GL.BindVertexArray(
            //        _vaos[i]
            //    );


            //    GL.ActiveTexture(
            //        TextureUnit.Texture0
            //    );


            //    GL.BindTexture(
            //        TextureTarget.Texture2D,
            //        _textures[i]
            //    );


            //    GL.DrawElements(
            //        PrimitiveType.Triangles,
            //        building.Meshes[i].IndexCount,
            //        DrawElementsType.UnsignedInt,
            //        0
            //    );
            //}

            //CAR MODEL

            _shader.SetMatrix4(
                "_model",
                _model_car
            );

            for (int i = 0; i < car.Meshes.Count; i++)
            {
                Matrix4 meshModel = _model_car;


                if (_wheelMeshes.Contains(i))
                {
                    meshModel = ApplyWheelTransform(
                        _model_car,
                        i
                    );
                }


                _shader.SetMatrix4(
                    "_model",
                    meshModel
                );


                GL.BindVertexArray(_carVaos[i]);

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
            DrawDriftParticles();
            DrawHUD();
            SwapBuffers();
        }

        private void DrawDriftParticles()
        {
            if (_driftParticles.Count == 0)
                return;

            List<float> vertices = new();

            foreach (DriftParticle particle in _driftParticles)
            {
                float size = 0.1f;
                //float size = particle.Size * 0.5f;

                Vector3 toCamera = _cameraPosition - particle.Position;
                if (toCamera.LengthSquared < 0.0001f)
                    continue;

                toCamera.Normalize();

                Vector3 right = Vector3.Cross(Vector3.UnitY, toCamera);
                if (right.LengthSquared < 0.0001f)
                    right = Vector3.UnitX;
                else
                    right.Normalize();

                Vector3 up = Vector3.UnitY;

                Vector3 a = particle.Position - right * size - up * size;
                Vector3 b = particle.Position + right * size - up * size;
                Vector3 c = particle.Position + right * size + up * size;
                Vector3 d = particle.Position - right * size + up * size;

                AddVertex(vertices, a, 0, 0);
                AddVertex(vertices, b, 1, 0);
                AddVertex(vertices, c, 1, 1);

                AddVertex(vertices, a, 0, 0);
                AddVertex(vertices, c, 1, 1);
                AddVertex(vertices, d, 0, 1);
            }

            if (vertices.Count == 0)
                return;

            // TEMPORARY TEST
            // Test shader and texture setup only
            _shader.Use();

            _shader.SetMatrix4("_model", Matrix4.Identity);
            _shader.SetMatrix4("view", _view);
            _shader.SetMatrix4("projection", _projection);
            _shader.SetInt("texture0", 0);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _particleTexture);

            GL.BindVertexArray(_particleVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _particleVbo);

            GL.BufferData(
                BufferTarget.ArrayBuffer,
                vertices.Count * sizeof(float),
                vertices.ToArray(),
                BufferUsageHint.DynamicDraw);

            GL.DrawArrays(
            PrimitiveType.Triangles,
            0,
            vertices.Count / 5);

            GL.BindVertexArray(0);

            // Restore the normal shader uniforms
            _shader.Use();
            _shader.SetMatrix4("_model", _model);
            _shader.SetMatrix4("view", _view);
            _shader.SetMatrix4("projection", _projection);
        }

        private void DrawGeneratedMesh(
    int vao,
    int texture,
    int indexCount)
        {
            _shader.SetMatrix4("_model", Matrix4.Identity);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.BindVertexArray(vao);

            GL.DrawElements(
                PrimitiveType.Triangles,
                indexCount,
                DrawElementsType.UnsignedInt,
                0
            );
        }

        private void DrawHUDTexture(
        int texture,
        float x,
        float y,
        float width,
        float height)
        {
            if (texture == 0 || !_hudInitialized)
                return;

            float screenWidth = ClientSize.X;
            float screenHeight = ClientSize.Y;

            if (screenWidth <= 0 || screenHeight <= 0)
                return;

            // Convert screen pixels to OpenGL normalized coordinates.
            float left = 2f * x / screenWidth - 1f;
            float right = 2f * (x + width) / screenWidth - 1f;
            float top = 1f - 2f * y / screenHeight;
            float bottom = 1f - 2f * (y + height) / screenHeight;

            float[] vertices =
            {
        left,  bottom, 0f, 1f,
        right, bottom, 1f, 1f,
        left,  top,    0f, 0f,
        right, top,    1f, 0f
    };

            GL.UseProgram(_hudShader);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);

            GL.BindVertexArray(_hudVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _hudVbo);

            GL.BufferSubData(
                BufferTarget.ArrayBuffer,
                IntPtr.Zero,
                vertices.Length * sizeof(float),
                vertices
            );

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.UseProgram(0);
        }

        private void DrawNeedle(
        int texture,
        float centerX,
        float pivotY,
        float width,
        float height,
        float angleDegrees)
        {
            if (texture == 0 || !_hudInitialized)
                return;

            float screenWidth = ClientSize.X;
            float screenHeight = ClientSize.Y;

            if (screenWidth <= 0 || screenHeight <= 0)
                return;

            float radians = MathHelper.DegreesToRadians(angleDegrees);
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);

            // Each corner is relative to the needle's bottom-center pivot.
            Vector2[] corners =
            {
        new Vector2(-width / 2f, -height),
        new Vector2( width / 2f, -height),
        new Vector2(-width / 2f, 0f),
        new Vector2( width / 2f, 0f)
    };

            float[] uvs = { 0f, 0f, 1f, 0f, 0f, 1f, 1f, 1f };
            float[] vertices = new float[16];

            for (int i = 0; i < 4; i++)
            {
                float dx = corners[i].X;
                float dy = corners[i].Y;

                float rotatedX = dx * cos - dy * sin;
                float rotatedY = dx * sin + dy * cos;

                float px = centerX + rotatedX;
                float py = pivotY + rotatedY;

                vertices[i * 4] = 2f * px / screenWidth - 1f;
                vertices[i * 4 + 1] = 1f - 2f * py / screenHeight;
                vertices[i * 4 + 2] = uvs[i * 2];
                vertices[i * 4 + 3] = uvs[i * 2 + 1];
            }

            GL.UseProgram(_hudShader);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);

            GL.BindVertexArray(_hudVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _hudVbo);

            GL.BufferSubData(
                BufferTarget.ArrayBuffer,
                IntPtr.Zero,
                vertices.Length * sizeof(float),
                vertices
            );

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.UseProgram(0);
        }

        private void DrawHUD()
        {
            if (!_hudInitialized)
                return;

            float screenWidth = ClientSize.X;
            float screenHeight = ClientSize.Y;

            if (screenWidth <= 0 || screenHeight <= 0)
                return;

            // Smaller gauges.
            float gaugeSize = MathF.Min(165f, screenHeight * 0.24f);
            float margin = 18f;
            float y = screenHeight - gaugeSize - margin;

            float tachX = margin;
            float speedX = screenWidth - gaugeSize - margin;

            GL.GetBoolean(GetPName.DepthTest, out bool depthWasEnabled);
            GL.GetBoolean(GetPName.Blend, out bool blendWasEnabled);

            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(
                BlendingFactor.SrcAlpha,
                BlendingFactor.OneMinusSrcAlpha
            );

            // Gauge faces.
            DrawHUDTexture(
                _tachometerTexture,
                tachX, y,
                gaugeSize, gaugeSize
            );

            DrawHUDTexture(
                _speedometerTexture,
                speedX, y,
                gaugeSize, gaugeSize
            );

            // Speedometer needle: follows speed only.
            float speedNormalized = Math.Clamp(
                GetSpeedKmh() / 240f,
                0f, 1f
            );

            float speedAngle = -135f + speedNormalized * 270f;

            DrawNeedle(
                _needleTexture,
                speedX + gaugeSize / 2f,
                y + gaugeSize / 2f,
                gaugeSize * 0.055f,
                gaugeSize * 0.36f,
                speedAngle
            );

            // Tachometer needle: follows simulated RPM, not speed directly.
            float rpmNormalized = Math.Clamp(
                _displayedRpm / 8000f,
                0f, 1f
            );

            float rpmAngle = -135f + rpmNormalized * 270f;

            DrawNeedle(
                _needleTexture,
                tachX + gaugeSize / 2f,
                y + gaugeSize / 2f,
                gaugeSize * 0.055f,
                gaugeSize * 0.36f,
                rpmAngle
            );

            if (depthWasEnabled)
                GL.Enable(EnableCap.DepthTest);
            else
                GL.Disable(EnableCap.DepthTest);

            if (blendWasEnabled)
                GL.Enable(EnableCap.Blend);
            else
                GL.Disable(EnableCap.Blend);
        }
    }
}
