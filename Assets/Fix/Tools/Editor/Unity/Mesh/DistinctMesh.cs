using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fix.Editor
{
    public class DistinctMesh : FixEditorBase
    {
        private const string Title = "网格除重";

        [FixEditor(FixRoot + nameof(Mesh) + "/" + Title + "(差异值)")]
        private static void DistinctByDelta()
        {
            const float compareTolerance = 0.001f;
            var candidateBuckets = new Dictionary<string, List<Mesh>>();
            var repList = new List<KeyValuePair<string, string>>();
            var paths = AssetDatabase.FindAssets("t:Mesh", new string[] {"Assets"})
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            int c = 0, total = paths.Length;
            try
            {
                foreach (var path in paths)
                {
                    EditorUtility.DisplayProgressBar("加载Mesh", path, (float) c++ / total);
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (mesh == null) continue;

                    var key = MeshHasher.GetCandidateKey(mesh);
                    if (candidateBuckets.TryGetValue(key, out var candidates))
                    {
                        Mesh matchedMesh = null;
                        foreach (var candidate in candidates)
                        {
                            if (!MeshHasher.AreMeshesGeometricallyEqual(mesh, candidate, compareTolerance)) continue;
                            matchedMesh = candidate;
                            break;
                        }

                        if (matchedMesh != null
                            && matchedMesh.TryGetRefString(out var @new)
                            && mesh.TryGetRefString(out var @old)
                            && @new != @old)
                        {
                            repList.Add(new KeyValuePair<string, string>(@old, @new));
                        }
                        else if (matchedMesh == null) candidates.Add(mesh);
                    }
                    else candidateBuckets.Add(key, new List<Mesh> {mesh});
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            FixEditorExtension.BatchReplaceRef(repList);
        }

        [FixEditor(FixRoot + nameof(Mesh) + "/" + Title + "(哈希)")]
        private static void DistinctByHash()
        {
            var dictionary = new Dictionary<long, Mesh>();
            var repList = new List<KeyValuePair<string, string>>();
            var paths = AssetDatabase.FindAssets("t:Mesh", new string[] {"Assets"})
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            int c = 0, total = paths.Length;
            try
            {
                foreach (var path in paths)
                {
                    EditorUtility.DisplayProgressBar("加载Mesh", path, (float) c++ / total);
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    var hash = MeshHasher.Hash(mesh);
                    if (dictionary.TryGetValue(hash, out var newMesh))
                    {
                        if (newMesh.TryGetRefString(out var @new)
                            && mesh.TryGetRefString(out var @old))
                        {
                            repList.Add(new KeyValuePair<string, string>(@old, @new));
                        }
                    }
                    else dictionary.Add(hash, mesh);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            FixEditorExtension.BatchReplaceRef(repList);
        }

        public static class MeshHasher
        {
            public static string GetCandidateKey(Mesh mesh)
            {
                if (mesh == null) return "null";

                var key = new StringBuilder(64);
                key.Append(mesh.vertexCount).Append('|')
                    .Append(mesh.subMeshCount).Append('|')
                    .Append(mesh.triangles.Length).Append('|')
                    .Append(mesh.normals.Length).Append('|')
                    .Append(mesh.tangents.Length).Append('|')
                    .Append(mesh.colors.Length).Append('|')
                    .Append(mesh.uv.Length).Append('|')
                    .Append(mesh.uv2.Length).Append('|')
                    .Append(mesh.uv3.Length).Append('|')
                    .Append(mesh.uv4.Length).Append('|');

                for (int i = 0; i < mesh.subMeshCount; i++)
                    key.Append((int) mesh.GetTopology(i)).Append(',');

                return key.ToString();
            }

            /// <summary>
            /// 计算Mesh的哈希值，基于网格数据而非名称
            /// </summary>
            public static long Hash(Mesh mesh)
            {
                if (mesh == null) return 0;

                // 使用增量哈希算法减少内存分配
                long hash = unchecked((long) 14695981039346656037L); // FNV-1a 64位偏移基础值

                // 1. 哈希基本元数据
                hash = Fnv1aHash64(hash, mesh.vertexCount);
                hash = Fnv1aHash64(hash, mesh.subMeshCount);
                hash = Fnv1aHash64(hash, mesh.triangles.Length);

                // 2. 哈希边界框信息（不直接使用浮点数）
                hash = Fnv1aHash64(hash, mesh.bounds.center.x.GetHashCode());
                hash = Fnv1aHash64(hash, mesh.bounds.center.y.GetHashCode());
                hash = Fnv1aHash64(hash, mesh.bounds.center.z.GetHashCode());
                hash = Fnv1aHash64(hash, mesh.bounds.size.x.GetHashCode());
                hash = Fnv1aHash64(hash, mesh.bounds.size.y.GetHashCode());
                hash = Fnv1aHash64(hash, mesh.bounds.size.z.GetHashCode());

                // 3. 哈希顶点数据（采样部分顶点以减少计算量）
                HashVertexData(mesh, ref hash);

                // 4. 哈希三角形拓扑结构
                HashTriangleData(mesh, ref hash);

                // 5. 哈希子网格信息
                HashSubMeshData(mesh, ref hash);

                // 6. 哈希其他顶点属性（法线、UV、颜色等）
                HashAdditionalAttributes(mesh, ref hash);

                return hash;
            }

            /// <summary>
            /// 快速计算Mesh哈希（性能优化版本）
            /// </summary>
            public static long QuickHash(Mesh mesh)
            {
                if (mesh == null) return 0;

                // 使用种子值
                long hash = 5381L;

                // 1. 元数据哈希
                hash = ((hash << 5) + hash) + mesh.vertexCount;
                hash = ((hash << 5) + hash) + mesh.subMeshCount;
                hash = ((hash << 5) + hash) + mesh.triangles.Length;

                // 2. 采样顶点数据
                if (mesh.vertexCount > 0)
                {
                    // 采样固定数量的顶点
                    int sampleCount = Mathf.Min(mesh.vertexCount, 100);
                    int step = mesh.vertexCount / sampleCount;

                    var vertices = mesh.vertices;
                    for (int i = 0; i < sampleCount; i++)
                    {
                        int idx = i * step;
                        if (idx < vertices.Length)
                        {
                            hash = ((hash << 5) + hash) + vertices[idx].x.GetHashCode();
                            hash = ((hash << 5) + hash) + vertices[idx].y.GetHashCode();
                            hash = ((hash << 5) + hash) + vertices[idx].z.GetHashCode();
                        }
                    }
                }

                // 3. 采样三角形数据
                if (mesh.triangles.Length > 0)
                {
                    int sampleCount = Mathf.Min(mesh.triangles.Length, 50);
                    int step = mesh.triangles.Length / sampleCount;

                    var triangles = mesh.triangles;
                    for (int i = 0; i < sampleCount; i++)
                    {
                        int idx = i * step;
                        if (idx < triangles.Length)
                        {
                            hash = ((hash << 5) + hash) + triangles[idx];
                        }
                    }
                }

                return hash;
            }

            /// <summary>
            /// 计算Mesh的完整哈希（更精确但更慢）
            /// </summary>
            public static string ComputeMeshHash(Mesh mesh)
            {
                if (mesh == null) return "null";

                using (var md5 = MD5.Create())
                {
                    var hashBuilder = new StringBuilder();

                    // 1. 顶点数据
                    hashBuilder.Append("Vertices:");
                    var vertices = mesh.vertices;
                    for (int i = 0; i < Mathf.Min(vertices.Length, 50); i++)
                    {
                        hashBuilder.Append($"{vertices[i].x:F3},{vertices[i].y:F3},{vertices[i].z:F3};");
                    }

                    // 2. 三角形数据
                    hashBuilder.Append("Triangles:");
                    var triangles = mesh.triangles;
                    for (int i = 0; i < Mathf.Min(triangles.Length, 100); i++)
                    {
                        hashBuilder.Append($"{triangles[i]},");
                    }

                    // 3. 拓扑信息
                    hashBuilder.Append($"VertexCount:{mesh.vertexCount};");
                    hashBuilder.Append($"SubMeshCount:{mesh.subMeshCount};");
                    hashBuilder.Append($"TriangleCount:{triangles.Length / 3};");

                    // 4. 边界信息
                    hashBuilder.Append($"Bounds:{mesh.bounds};");

                    // 5. 其他属性存在性
                    hashBuilder.Append($"HasNormals:{mesh.normals.Length > 0};");
                    hashBuilder.Append($"HasTangents:{mesh.tangents.Length > 0};");
                    hashBuilder.Append($"HasColors:{mesh.colors.Length > 0};");
                    hashBuilder.Append($"HasUV:{mesh.uv.Length > 0};");
                    hashBuilder.Append($"HasUV2:{mesh.uv2.Length > 0};");
                    hashBuilder.Append($"HasUV3:{mesh.uv3.Length > 0};");
                    hashBuilder.Append($"HasUV4:{mesh.uv4.Length > 0};");

                    byte[] inputBytes = Encoding.UTF8.GetBytes(hashBuilder.ToString());
                    byte[] hashBytes = md5.ComputeHash(inputBytes);

                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }

            /// <summary>
            /// 比较两个Mesh是否几何相等
            /// </summary>
            public static bool AreMeshesGeometricallyEqual(Mesh mesh1, Mesh mesh2, float tolerance = 0.001f)
            {
                if (mesh1 == mesh2) return true;
                if (mesh1 == null || mesh2 == null) return false;

                // 快速检查
                if (mesh1.vertexCount != mesh2.vertexCount) return false;
                if (mesh1.subMeshCount != mesh2.subMeshCount) return false;
                if (mesh1.triangles.Length != mesh2.triangles.Length) return false;

                // 检查边界框（快速排除）
                if (!AreBoundsApproximatelyEqual(mesh1.bounds, mesh2.bounds, tolerance))
                    return false;

                // 基础几何与拓扑
                return AreVertexArraysEqual(mesh1.vertices, mesh2.vertices, tolerance) &&
                       AreSubMeshesEqual(mesh1, mesh2) &&
                       // 关键附加属性差异比较
                       AreOptionalVector3ArraysEqual(mesh1.normals, mesh2.normals, tolerance) &&
                       AreOptionalVector4ArraysEqual(mesh1.tangents, mesh2.tangents, tolerance) &&
                       AreOptionalVector2ArraysEqual(mesh1.uv, mesh2.uv, tolerance) &&
                       AreOptionalVector2ArraysEqual(mesh1.uv2, mesh2.uv2, tolerance) &&
                       AreOptionalVector2ArraysEqual(mesh1.uv3, mesh2.uv3, tolerance) &&
                       AreOptionalVector2ArraysEqual(mesh1.uv4, mesh2.uv4, tolerance) &&
                       AreOptionalColorArraysEqual(mesh1.colors, mesh2.colors, tolerance);
            }

            #region 私有辅助方法

            private static long Fnv1aHash64(long hash, int data)
            {
                const long fnvPrime = 1099511628211L;
                hash ^= data;
                hash *= fnvPrime;
                return hash;
            }

            private static long Fnv1aHash64(long hash, long data)
            {
                const long fnvPrime = 1099511628211L;
                hash ^= data;
                hash *= fnvPrime;
                return hash;
            }

            private static void HashVertexData(Mesh mesh, ref long hash)
            {
                if (mesh.vertexCount == 0) return;

                var vertices = mesh.vertices;

                // 采样策略：取首、中、尾三部分的顶点
                int sampleCount = Mathf.Min(vertices.Length, 50);
                int step = Mathf.Max(1, vertices.Length / sampleCount);

                for (int i = 0; i < vertices.Length; i += step)
                {
                    // 对浮点数进行量化以减少微小差异的影响
                    int quantizedX = (int) (vertices[i].x * 1000);
                    int quantizedY = (int) (vertices[i].y * 1000);
                    int quantizedZ = (int) (vertices[i].z * 1000);

                    hash = Fnv1aHash64(hash, quantizedX);
                    hash = Fnv1aHash64(hash, quantizedY);
                    hash = Fnv1aHash64(hash, quantizedZ);
                }
            }

            private static void HashTriangleData(Mesh mesh, ref long hash)
            {
                var triangles = mesh.triangles;
                if (triangles.Length == 0) return;

                // 计算三角形索引的校验和
                long triangleChecksum = 0;
                for (int i = 0; i < triangles.Length; i++)
                {
                    triangleChecksum = (triangleChecksum * 31) + triangles[i];
                }

                hash = Fnv1aHash64(hash, triangleChecksum);

                // 也采样部分三角形数据
                int sampleCount = Mathf.Min(triangles.Length, 30);
                int step = Mathf.Max(1, triangles.Length / sampleCount);

                for (int i = 0; i < triangles.Length; i += step)
                {
                    hash = Fnv1aHash64(hash, triangles[i]);
                }
            }

            private static void HashSubMeshData(Mesh mesh, ref long hash)
            {
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    var triangles = mesh.GetTriangles(i);
                    if (triangles.Length > 0)
                    {
                        hash = Fnv1aHash64(hash, triangles.Length);

                        // 采样子网格的三角形
                        int sampleCount = Mathf.Min(triangles.Length, 10);
                        int step = Mathf.Max(1, triangles.Length / sampleCount);

                        for (int j = 0; j < triangles.Length; j += step)
                        {
                            hash = Fnv1aHash64(hash, triangles[j]);
                        }
                    }
                }
            }

            private static void HashAdditionalAttributes(Mesh mesh, ref long hash)
            {
                // 哈希法线
                if (mesh.normals != null && mesh.normals.Length > 0)
                {
                    hash = Fnv1aHash64(hash, mesh.normals.Length);

                    // 采样法线数据
                    int sampleCount = Mathf.Min(mesh.normals.Length, 20);
                    int step = Mathf.Max(1, mesh.normals.Length / sampleCount);

                    for (int i = 0; i < mesh.normals.Length; i += step)
                    {
                        var normal = mesh.normals[i];
                        hash = Fnv1aHash64(hash, (int) (normal.x * 1000));
                        hash = Fnv1aHash64(hash, (int) (normal.y * 1000));
                        hash = Fnv1aHash64(hash, (int) (normal.z * 1000));
                    }
                }

                // 哈希UV
                if (mesh.uv != null && mesh.uv.Length > 0)
                {
                    hash = Fnv1aHash64(hash, mesh.uv.Length);

                    int sampleCount = Mathf.Min(mesh.uv.Length, 20);
                    int step = Mathf.Max(1, mesh.uv.Length / sampleCount);

                    for (int i = 0; i < mesh.uv.Length; i += step)
                    {
                        var uv = mesh.uv[i];
                        hash = Fnv1aHash64(hash, (int) (uv.x * 1000));
                        hash = Fnv1aHash64(hash, (int) (uv.y * 1000));
                    }
                }

                // 哈希颜色
                if (mesh.colors != null && mesh.colors.Length > 0)
                {
                    hash = Fnv1aHash64(hash, mesh.colors.Length);
                }

                // 哈希切线
                if (mesh.tangents != null && mesh.tangents.Length > 0)
                {
                    hash = Fnv1aHash64(hash, mesh.tangents.Length);
                }
            }

            private static bool AreBoundsApproximatelyEqual(Bounds b1, Bounds b2, float tolerance)
            {
                return Vector3.Distance(b1.center, b2.center) < tolerance &&
                       Vector3.Distance(b1.size, b2.size) < tolerance;
            }

            private static bool AreVertexArraysEqual(Vector3[] v1, Vector3[] v2, float tolerance)
            {
                if (v1.Length != v2.Length) return false;

                for (int i = 0; i < v1.Length; i++)
                {
                    if (Vector3.Distance(v1[i], v2[i]) > tolerance)
                        return false;
                }

                return true;
            }

            private static bool AreTriangleArraysEqual(int[] t1, int[] t2)
            {
                if (t1.Length != t2.Length) return false;

                for (int i = 0; i < t1.Length; i++)
                {
                    if (t1[i] != t2[i]) return false;
                }

                return true;
            }

            private static bool AreSubMeshesEqual(Mesh mesh1, Mesh mesh2)
            {
                if (mesh1.subMeshCount != mesh2.subMeshCount) return false;

                for (int i = 0; i < mesh1.subMeshCount; i++)
                {
                    if (mesh1.GetTopology(i) != mesh2.GetTopology(i)) return false;
                    if (!AreTriangleArraysEqual(mesh1.GetTriangles(i), mesh2.GetTriangles(i))) return false;
                }

                return true;
            }

            private static bool AreOptionalVector2ArraysEqual(Vector2[] v1, Vector2[] v2, float tolerance)
            {
                bool empty1 = v1 == null || v1.Length == 0;
                bool empty2 = v2 == null || v2.Length == 0;
                if (empty1 || empty2) return empty1 == empty2;
                if (v1.Length != v2.Length) return false;

                for (int i = 0; i < v1.Length; i++)
                {
                    if (Vector2.Distance(v1[i], v2[i]) > tolerance)
                        return false;
                }

                return true;
            }

            private static bool AreOptionalVector3ArraysEqual(Vector3[] v1, Vector3[] v2, float tolerance)
            {
                bool empty1 = v1 == null || v1.Length == 0;
                bool empty2 = v2 == null || v2.Length == 0;
                if (empty1 || empty2) return empty1 == empty2;
                return AreVertexArraysEqual(v1, v2, tolerance);
            }

            private static bool AreOptionalVector4ArraysEqual(Vector4[] v1, Vector4[] v2, float tolerance)
            {
                bool empty1 = v1 == null || v1.Length == 0;
                bool empty2 = v2 == null || v2.Length == 0;
                if (empty1 || empty2) return empty1 == empty2;
                if (v1.Length != v2.Length) return false;

                for (int i = 0; i < v1.Length; i++)
                {
                    if (Vector4.Distance(v1[i], v2[i]) > tolerance)
                        return false;
                }

                return true;
            }

            private static bool AreOptionalColorArraysEqual(Color[] c1, Color[] c2, float tolerance)
            {
                bool empty1 = c1 == null || c1.Length == 0;
                bool empty2 = c2 == null || c2.Length == 0;
                if (empty1 || empty2) return empty1 == empty2;
                if (c1.Length != c2.Length) return false;

                for (int i = 0; i < c1.Length; i++)
                {
                    if (Mathf.Abs(c1[i].r - c2[i].r) > tolerance
                        || Mathf.Abs(c1[i].g - c2[i].g) > tolerance
                        || Mathf.Abs(c1[i].b - c2[i].b) > tolerance
                        || Mathf.Abs(c1[i].a - c2[i].a) > tolerance)
                        return false;
                }

                return true;
            }

            #endregion

            #region 扩展方法

            #endregion
        }
    }
}