using System.Collections.Generic;
using UnityEngine;

namespace Joust.Editor
{
    /// <summary>
    /// Generates natural-looking rock meshes.
    ///
    /// Box primitives read as boxes no matter how they are textured, so
    /// platforms are built as subdivided slabs whose vertices are displaced by
    /// multi-octave noise. The walkable top face is deliberately displaced far
    /// less than the rest: it has to stay landable, while the sides and
    /// underside erode freely.
    /// </summary>
    public static class ProceduralRock
    {
        /// <summary>World units covered by one repeat of the rock texture.</summary>
        private const float UvUnits = 1.4f;

        /// <summary>
        /// Builds a rock slab of the given size.
        /// </summary>
        /// <param name="size">Overall bounds of the slab in world units.</param>
        /// <param name="seed">Per-platform seed, so no two rocks are identical.</param>
        /// <param name="roughness">Displacement amplitude as a fraction of size.</param>
        /// <param name="topFlatness">0 = top erodes like the rest, 1 = top stays flat.</param>
        public static Mesh CreateSlab(Vector3 size, int seed, float roughness = 0.22f, float topFlatness = 0.82f)
        {
            // Subdivision scales with size so large and small rocks carry a
            // similar density of detail.
            var nx = Mathf.Clamp(Mathf.RoundToInt(size.x * 4f), 6, 40);
            var ny = Mathf.Clamp(Mathf.RoundToInt(size.y * 6f), 4, 20);
            var nz = Mathf.Clamp(Mathf.RoundToInt(size.z * 5f), 4, 20);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            var offset = new Vector2(seed * 0.37f, seed * 0.71f);

            void Face(Vector3 origin, Vector3 uDir, Vector3 vDir, int uSteps, int vSteps, Vector3 normal)
            {
                var baseIndex = vertices.Count;
                // World-scaled UVs: one texture repeat per UvUnits of surface,
                // identical on every face and every rock size.
                var uLength = uDir.magnitude;
                var vLength = vDir.magnitude;

                for (var v = 0; v <= vSteps; v++)
                {
                    for (var u = 0; u <= uSteps; u++)
                    {
                        var fu = (float)u / uSteps;
                        var fv = (float)v / vSteps;
                        var position = origin + uDir * fu + vDir * fv;

                        // Displacement is driven by position, so vertices shared
                        // between faces at an edge move together and the slab
                        // stays closed.
                        var amount = Displacement(position, offset, size, roughness, normal, topFlatness);
                        vertices.Add(position + amount);
                        uvs.Add(new Vector2(fu * uLength / UvUnits, fv * vLength / UvUnits));
                    }
                }

                for (var v = 0; v < vSteps; v++)
                {
                    for (var u = 0; u < uSteps; u++)
                    {
                        var i0 = baseIndex + v * (uSteps + 1) + u;
                        var i1 = i0 + 1;
                        var i2 = i0 + (uSteps + 1);
                        var i3 = i2 + 1;

                        triangles.Add(i0); triangles.Add(i2); triangles.Add(i1);
                        triangles.Add(i1); triangles.Add(i2); triangles.Add(i3);
                    }
                }
            }

            var half = size * 0.5f;

            // top (+Y) and bottom (-Y)
            Face(new Vector3(-half.x, half.y, -half.z), Vector3.right * size.x, Vector3.forward * size.z, nx, nz, Vector3.up);
            Face(new Vector3(-half.x, -half.y, half.z), Vector3.right * size.x, Vector3.back * size.z, nx, nz, Vector3.down);
            // front (-Z) and back (+Z)
            Face(new Vector3(-half.x, -half.y, -half.z), Vector3.right * size.x, Vector3.up * size.y, nx, ny, Vector3.back);
            Face(new Vector3(half.x, -half.y, half.z), Vector3.left * size.x, Vector3.up * size.y, nx, ny, Vector3.forward);
            // left (-X) and right (+X)
            Face(new Vector3(-half.x, -half.y, half.z), Vector3.forward * -size.z, Vector3.up * size.y, nz, ny, Vector3.left);
            Face(new Vector3(half.x, -half.y, -half.z), Vector3.forward * size.z, Vector3.up * size.y, nz, ny, Vector3.right);

            var mesh = new Mesh { name = $"rock_{seed}" };
            mesh.indexFormat = vertices.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Three octaves of Perlin noise. The lowest frequency gives the overall
        /// lumpy shape, the highest gives surface bite.
        /// </summary>
        private static Vector3 Displacement(Vector3 position, Vector2 offset, Vector3 size,
            float roughness, Vector3 faceNormal, float topFlatness)
        {
            float Octave(float frequency)
            {
                var x = (position.x + offset.x) * frequency;
                var y = (position.y + offset.y) * frequency;
                var z = (position.z + offset.x * 0.5f) * frequency;
                // Two samples combined so the field varies in all three axes.
                return (Mathf.PerlinNoise(x, z) + Mathf.PerlinNoise(y, z + 11.3f)) * 0.5f - 0.5f;
            }

            var n = Octave(0.55f) * 1.0f + Octave(1.6f) * 0.45f + Octave(4.1f) * 0.18f;

            var scale = roughness * Mathf.Min(size.x, Mathf.Max(size.y, size.z)) * 2.2f;

            // Keep the deck landable: the upward face barely moves.
            var upness = Mathf.Clamp01(faceNormal.y);
            scale *= Mathf.Lerp(1f, 1f - topFlatness, upness);

            // Undersides erode hardest, which is what reads as weathered rock.
            var downness = Mathf.Clamp01(-faceNormal.y);
            scale *= 1f + downness * 0.9f;

            return faceNormal.normalized * (n * scale);
        }
    }
}
