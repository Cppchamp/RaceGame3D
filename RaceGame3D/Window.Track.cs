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
        private FastNoiseLite _terrainNoise;

        private void InitializeTerrainNoise()
        {
            _terrainNoise = new FastNoiseLite(TrackSeed);

            _terrainNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

            // Controls the size of the hills.
            _terrainNoise.SetFrequency(0.0025f);

            // Adds detail at multiple scales.
            _terrainNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            _terrainNoise.SetFractalOctaves(5);
            _terrainNoise.SetFractalLacunarity(2.0f);
            _terrainNoise.SetFractalGain(0.45f);
        }   

        private void GenerateMountainTrack()
        {
            _trackPoints.Clear();

            Random random = new Random(TrackSeed);
            const int pointCount = 180;

            // GENERATE LARGE TRACK WITH UPHILL AND DOWNHILL SECTIONS
            for (int i = 0; i < pointCount; i++)
            {
                float t = i * MathF.PI * 2f / pointCount;

                float radiusX = 180f
                    + 34f * MathF.Sin(3f * t + 0.4f)
                    + 16f * MathF.Sin(5f * t);

                float radiusZ = 250f
                    + 40f * MathF.Sin(2f * t + 0.8f)
                    + 18f * MathF.Sin(6f * t);

                float x = MathF.Cos(t) * radiusX;
                float z = MathF.Sin(t) * radiusZ;

                float y = GetTerrainHeight(x, z) + 0.3f;

                _trackPoints.Add(new Vector3(x, y, z));
            }

            // ROAD MESH
            List<float> roadVertices = new();
            List<uint> roadIndices = new();

            for (int i = 0; i < pointCount; i++)
            {
                int next = (i + 1) % pointCount;

                Vector3 current = _trackPoints[i];
                Vector3 following = _trackPoints[next];

                Vector3 tangent = new Vector3(
                    following.X - current.X,
                    0f,
                    following.Z - current.Z
                );

                if (tangent.LengthSquared < 0.0001f)
                    continue;

                tangent.Normalize();

                Vector3 right = new Vector3(tangent.Z, 0f, -tangent.X);

                Vector3 leftA = current - right * TrackHalfWidth;
                Vector3 rightA = current + right * TrackHalfWidth;
                Vector3 leftB = following - right * TrackHalfWidth;
                Vector3 rightB = following + right * TrackHalfWidth;

                // Conform both road edges to the terrain.
                leftA.Y = GetTerrainHeight(leftA.X, leftA.Z) + 0.3f;
                rightA.Y = GetTerrainHeight(rightA.X, rightA.Z) + 0.3f;
                leftB.Y = GetTerrainHeight(leftB.X, leftB.Z) + 0.3f;
                rightB.Y = GetTerrainHeight(rightB.X, rightB.Z) + 0.3f;

                uint start = (uint)(roadVertices.Count / 5);

                AddVertex(roadVertices, leftA, 0f, 0f);
                AddVertex(roadVertices, rightA, 1f, 0f);
                AddVertex(roadVertices, rightB, 1f, 1f);
                AddVertex(roadVertices, leftB, 0f, 1f);

                roadIndices.AddRange(new uint[]
                {
            start, start + 1, start + 2,
            start, start + 2, start + 3
                });
            }

            CreateMesh(
                roadVertices, roadIndices,
                out _trackVao, out _trackVbo,
                out _trackEbo, out _trackIndexCount
            );

            _trackTexture = CreateSolidTexture(65, 68, 73);

            // ROLLING HILL TERRAIN
            List<float> groundVertices = new();
            List<uint> groundIndices = new();

            float groundSize = 1600f;
            int terrainSegments = 200;
            float step = (groundSize * 2f) / terrainSegments;

            for (int z = 0; z <= terrainSegments; z++)
            {
                for (int x = 0; x <= terrainSegments; x++)
                {
                    float worldX = -groundSize + x * step;
                    float worldZ = -groundSize + z * step;
                    float worldY = GetTerrainHeight(worldX, worldZ);

                    AddVertex(
                        groundVertices,
                        new Vector3(worldX, worldY, worldZ),
                        x / 10f,
                        z / 10f
                    );
                }
            }

            int rowSize = terrainSegments + 1;

            for (int z = 0; z < terrainSegments; z++)
            {
                for (int x = 0; x < terrainSegments; x++)
                {
                    uint a = (uint)(z * rowSize + x);
                    uint b = a + 1;
                    uint d = (uint)((z + 1) * rowSize + x);
                    uint c = d + 1;

                    groundIndices.AddRange(new uint[]
                    {
                a, c, b,
                a, d, c
                    });
                }
            }

            CreateMesh(
                groundVertices, groundIndices,
                out _groundVao, out _groundVbo,
                out _groundEbo, out _groundIndexCount
            );

            _groundTexture = CreateSolidTexture(75, 125, 66);

            // LOW-POLY MOUNTAINS
            List<float> mountainVertices = new();
            List<uint> mountainIndices = new();

            for (int i = 0; i < 100; i++)
            {
                float angle = (float)(
                    random.NextDouble() * Math.PI * 2.0
                );

                float distance =
                    450f + (float)random.NextDouble() * 600f;

                float radius =
                    25f + (float)random.NextDouble() * 45f;

                float height =
                    35f + (float)random.NextDouble() * 85f;

                float centerX = MathF.Cos(angle) * distance;
                float centerZ = MathF.Sin(angle) * distance;

                float baseHeight = GetTerrainHeight(centerX, centerZ);

                int sides = random.Next(5, 8);
                uint start = (uint)(mountainVertices.Count / 5);

                // Mountain peak above the terrain
                AddVertex(
                    mountainVertices,
                    new Vector3(
                        centerX,
                        baseHeight + height,
                        centerZ
                    ),
                    0.5f, 1f
                );

                // Mountain base follows terrain elevation
                for (int j = 0; j < sides; j++)
                {
                    float a = j * MathF.PI * 2f / sides;

                    float mountainX = centerX + MathF.Cos(a) * radius;
                    float mountainZ = centerZ + MathF.Sin(a) * radius;
                    float mountainY = GetTerrainHeight(mountainX, mountainZ);

                    AddVertex(
                        mountainVertices,
                        new Vector3(mountainX, mountainY, mountainZ),
                        (float)j / sides,
                        0f
                    );
                }

                for (int j = 0; j < sides; j++)
                {
                    uint a = start + 1u + (uint)j;
                    uint b = start + 1u + (uint)((j + 1) % sides);

                    mountainIndices.Add(start);
                    mountainIndices.Add(a);
                    mountainIndices.Add(b);
                }
            }

            CreateMesh(
                mountainVertices, mountainIndices,
                out _mountainVao, out _mountainVbo,
                out _mountainEbo, out _mountainIndexCount
            );

            _mountainTexture = CreateSolidTexture(95, 101, 108);

            // PLACE CAR ON STARTING SECTION
            Vector3 startPoint = _trackPoints[0];
            Vector3 nextPoint = _trackPoints[1];

            _carPosition = new Vector3(
                startPoint.X,
                GetTerrainHeight(startPoint.X, startPoint.Z) + 0.3f,
                startPoint.Z);

            Vector3 startDirection = new Vector3(
                nextPoint.X - startPoint.X,
                0f,
                nextPoint.Z - startPoint.Z
            );

            if (startDirection.LengthSquared > 0.0001f)
                startDirection.Normalize();

            _carRotation = MathHelper.RadiansToDegrees(
                MathF.Atan2(startDirection.X, startDirection.Z)
            );

            _carVelocity = 0f;
            _carVelocityVector = Vector3.Zero;

            UpdateCarMatrix();
        }

        private float GetTerrainHeight(float x, float z)
        {
            if (_terrainNoise == null)
                return 0f;

            // Main rolling landscape.
            float hills = _terrainNoise.GetNoise(x, z) * 12f;

            // Keep the terrain from becoming too extreme.
            return hills;
        }

        private float GetRoadHeightAt(float x, float z)
        {
            if (_trackPoints.Count < 2)
                return GetTerrainHeight(x, z) + 0.3f;

            float closestDistanceSq = float.MaxValue;
            float roadHeight = GetTerrainHeight(x, z) + 0.3f;

            Vector2 position = new Vector2(x, z);

            for (int i = 0; i < _trackPoints.Count; i++)
            {
                Vector3 a = _trackPoints[i];
                Vector3 b = _trackPoints[(i + 1) % _trackPoints.Count];

                Vector2 a2 = new Vector2(a.X, a.Z);
                Vector2 b2 = new Vector2(b.X, b.Z);
                Vector2 segment = b2 - a2;

                float lengthSq = segment.LengthSquared;
                float t = lengthSq > 0.0001f
                    ? Vector2.Dot(position - a2, segment) / lengthSq
                    : 0f;

                t = MathHelper.Clamp(t, 0f, 1f);

                Vector2 closest = a2 + segment * t;
                float distanceSq = (position - closest).LengthSquared;

                if (distanceSq < closestDistanceSq)
                {
                    closestDistanceSq = distanceSq;
                    roadHeight = a.Y + (b.Y - a.Y) * t;
                }
            }

            return roadHeight;
        }

        private void ApplyOffRoadPenalty()
        {
            if (_trackPoints.Count < 2)
                return;

            float nearestDistance = float.MaxValue;

            for (int i = 0; i < _trackPoints.Count; i++)
            {
                Vector3 point = _trackPoints[i];

                float dx = point.X - _carPosition.X;
                float dz = point.Z - _carPosition.Z;
                float distance = MathF.Sqrt(dx * dx + dz * dz);

                if (distance < nearestDistance)
                    nearestDistance = distance;
            }

            float roadLimit = TrackHalfWidth - 1.0f;

            if (nearestDistance > roadLimit)
            {
                _carVelocity *= 0.97f;
                _carVelocityVector *= 0.97f;
            }
        }

        private float GetRoadHeightAtCar()
        {
            float nearestDistanceSq = float.MaxValue;
            float roadHeight = _trackPoints[0].Y;

            for (int i = 0; i < _trackPoints.Count; i++)
            {
                Vector3 a = _trackPoints[i];
                Vector3 b = _trackPoints[(i + 1) % _trackPoints.Count];

                Vector2 carXZ = new Vector2(_carPosition.X, _carPosition.Z);
                Vector2 aXZ = new Vector2(a.X, a.Z);
                Vector2 bXZ = new Vector2(b.X, b.Z);

                Vector2 segment = bXZ - aXZ;
                float lengthSq = segment.LengthSquared;

                float t = lengthSq > 0.0001f
                    ? Vector2.Dot(carXZ - aXZ, segment) / lengthSq
                    : 0f;

                t = MathHelper.Clamp(t, 0f, 1f);

                Vector2 closest = aXZ + segment * t;
                float distanceSq = (carXZ - closest).LengthSquared;

                if (distanceSq < nearestDistanceSq)
                {
                    nearestDistanceSq = distanceSq;
                    roadHeight = a.Y + (b.Y - a.Y) * t;
                }
            }

            return roadHeight;
        }

        private void ResolveTrackCollision()
        {
            if (_trackPoints.Count < 2)
                return;

            Vector3 carXZ = new Vector3(
                _carPosition.X, 0f, _carPosition.Z
            );

            Vector3 closestPoint = Vector3.Zero;
            float closestDistanceSquared = float.MaxValue;

            for (int i = 0; i < _trackPoints.Count; i++)
            {
                Vector3 a = _trackPoints[i];
                Vector3 b = _trackPoints[(i + 1) % _trackPoints.Count];

                // Work in the horizontal plane.
                a.Y = 0f;
                b.Y = 0f;

                Vector3 segment = b - a;
                float lengthSquared = segment.LengthSquared;

                if (lengthSquared < 0.0001f)
                    continue;

                float t = Vector3.Dot(carXZ - a, segment) / lengthSquared;
                t = MathHelper.Clamp(t, 0f, 1f);

                Vector3 point = a + segment * t;
                Vector3 difference = carXZ - point;

                float distanceSquared = difference.LengthSquared;

                if (distanceSquared < closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closestPoint = point;
                }
            }

            // Leave some space for the width of the car.
            float allowedDistance = TrackHalfWidth - 1.2f;

            if (closestDistanceSquared > allowedDistance * allowedDistance)
            {
                Vector3 outward = carXZ - closestPoint;

                if (outward.LengthSquared > 0.0001f)
                    outward = Vector3.Normalize(outward);
                else
                    outward = Vector3.UnitX;

                Vector3 corrected = closestPoint + outward * allowedDistance;

                _carPosition.X = corrected.X;
                _carPosition.Z = corrected.Z;

                // Keep the car above the road surface.
                // Find the road height at the nearest track point.
                float roadHeight = 0f;
                float nearestDistance = float.MaxValue;

                foreach (Vector3 point in _trackPoints)
                {
                    float dx = point.X - _carPosition.X;
                    float dz = point.Z - _carPosition.Z;
                    float distance = dx * dx + dz * dz;

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        roadHeight = point.Y;
                    }
                }

                // Raise the car above the road.
                // Adjust this offset if your car model's origin is different.
                _carPosition.Y = roadHeight + 1.0f;

                // Reduce speed when hitting the edge.
                _carVelocityVector *= 0.65f;
                _carVelocity *= 0.65f;
            }
        }
    }
}
