using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fix.Editor;
using MantisLOD;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MantisLODEditor
{
    public class MantisLODMeshCompressor : IFixMeshCompressor
    {
        private Mesh m;
        private Impl impl;

        public string Name => nameof(MantisLODEditor);
        public string Desc => "比较专业的工具";
        public float DefaultQuality => 0.65f;

        public void Init(Mesh mesh)
        {
            m = MakeMeshReadable(mesh);
            impl = new Impl();
            impl.target = m;
            impl.Start();
        }

        public void Release()
        {
            try
            {
                impl.End();
            }
            finally
            {
                impl = null;
                Object.DestroyImmediate(m);
            }
        }

        public Mesh Compress(float quality)
        {
            impl.quality = quality * 100;
            return impl.Compress();
        }

        public Mesh Preview(float quality)
        {
            return null;
        }

        public void Dispose()
        {
        }

        private class Impl
        {
            public Mesh target;

            public void Start()
            {
                init_all();
            }

            public void End()
            {
                clean_all();
            }

            /*
             * If you want to simplify meshes at runtime, please use the managed plugin.
             * The managed plugin is provided as c# script, which can run on all platforms,
             * but it is slower than the native plugin.
             * To use the managed plugin, You just need to add the prefix of
             * 'MantisLODSimpler.' before the native APIs.
             */

            public float quality = 100.0f;
            private float preQuality = 100.0f;
            private bool protect_boundary = true;
            private bool protect_detail = false;
            private bool protect_symmetry = false;
            private bool protect_normal = true;
            private bool protect_shape = true;
            private Mantis_Mesh Mantis_Mesh = null;
            private bool optimized = false;

            public Mesh Compress()
            {
                if (!optimized)
                {
                    optimize();
                    optimized = true;
                }

                if (!Mathf.Approximately(preQuality, quality))
                {
                    // get triangle list by quality value
                    if (Mantis_Mesh.index != -1 && (MantisLODSimpler.get_triangle_list(Mantis_Mesh.index, quality, Mantis_Mesh.out_triangles,
                            ref Mantis_Mesh.out_count)) == 1)
                    {
                        if (Mantis_Mesh.out_count > 0)
                        {
                            int counter = 0;
                            int mat = 0;
                            while (counter < Mantis_Mesh.out_count)
                            {
                                int len = Mantis_Mesh.out_triangles[counter];
                                counter++;
                                if (len > 0)
                                {
                                    int[] new_triangles = new int[len];
                                    Array.Copy(Mantis_Mesh.out_triangles, counter, new_triangles, 0, len);
                                    Mantis_Mesh.mesh.SetTriangles(new_triangles, mat);
                                    counter += len;
                                }
                                else
                                {
                                    Mantis_Mesh.mesh.SetTriangles((int[]) null, mat);
                                }

                                mat++;
                            }

                            EditorUtility.SetDirty(target);
                        }
                    }

                    preQuality = quality;
                }

                Mesh new_mesh = (Mesh) Object.Instantiate(Mantis_Mesh.mesh);
                // remove unused vertices
                if (new_mesh.blendShapeCount == 0)
                {
                    shrink_mesh(new_mesh);
                }

                MeshUtility.Optimize(new_mesh);
                new_mesh.RecalculateBounds();
                new_mesh.RecalculateNormals();
                new_mesh.RecalculateTangents();
                return new_mesh;
            }

            private void shrink_mesh(Mesh mesh)
            {
                // get all origin data
                Vector3[] origin_vertices = mesh.vertices;
                Vector3[] vertices = null;
                if (origin_vertices != null && origin_vertices.Length > 0)
                    vertices = new Vector3[origin_vertices.Length];
                BoneWeight[] origin_boneWeights = mesh.boneWeights;
                BoneWeight[] boneWeights = null;
                if (origin_boneWeights != null && origin_boneWeights.Length > 0)
                    boneWeights = new BoneWeight[origin_boneWeights.Length];
                Color[] origin_colors = mesh.colors;
                Color[] colors = null;
                if (origin_colors != null && origin_colors.Length > 0) colors = new Color[origin_colors.Length];
                Color32[] origin_colors32 = mesh.colors32;
                Color32[] colors32 = null;
                if (origin_colors32 != null && origin_colors32.Length > 0)
                    colors32 = new Color32[origin_colors32.Length];
                Vector4[] origin_tangents = mesh.tangents;
                Vector4[] tangents = null;
                if (origin_tangents != null && origin_tangents.Length > 0)
                    tangents = new Vector4[origin_tangents.Length];
                Vector3[] origin_normals = mesh.normals;
                Vector3[] normals = null;
                if (origin_normals != null && origin_normals.Length > 0) normals = new Vector3[origin_normals.Length];
                Vector2[] origin_uv = mesh.uv;
                Vector2[] uv = null;
                if (origin_uv != null && origin_uv.Length > 0) uv = new Vector2[origin_uv.Length];
                Vector2[] origin_uv2 = mesh.uv2;
                Vector2[] uv2 = null;
                if (origin_uv2 != null && origin_uv2.Length > 0) uv2 = new Vector2[origin_uv2.Length];
                int[][] origin_triangles = new int[mesh.subMeshCount][];
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    origin_triangles[i] = mesh.GetTriangles(i);
                }

                // 	make permutation
                Dictionary<int, int> imap = new Dictionary<int, int>();
                int vertex_count = 0;
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    int[] triangles = mesh.GetTriangles(i);
                    for (int j = 0; j < triangles.Length; j += 3)
                    {
                        if (!imap.ContainsKey(triangles[j]))
                        {
                            if (vertices != null) vertices[vertex_count] = origin_vertices[triangles[j]];
                            if (boneWeights != null) boneWeights[vertex_count] = origin_boneWeights[triangles[j]];
                            if (colors != null) colors[vertex_count] = origin_colors[triangles[j]];
                            if (colors32 != null) colors32[vertex_count] = origin_colors32[triangles[j]];
                            if (tangents != null) tangents[vertex_count] = origin_tangents[triangles[j]];
                            if (normals != null) normals[vertex_count] = origin_normals[triangles[j]];
                            if (uv != null) uv[vertex_count] = origin_uv[triangles[j]];
                            if (uv2 != null) uv2[vertex_count] = origin_uv2[triangles[j]];
                            imap.Add(triangles[j], vertex_count);
                            vertex_count++;
                        }

                        if (!imap.ContainsKey(triangles[j + 1]))
                        {
                            if (vertices != null) vertices[vertex_count] = origin_vertices[triangles[j + 1]];
                            if (boneWeights != null) boneWeights[vertex_count] = origin_boneWeights[triangles[j + 1]];
                            if (colors != null) colors[vertex_count] = origin_colors[triangles[j + 1]];
                            if (colors32 != null) colors32[vertex_count] = origin_colors32[triangles[j + 1]];
                            if (tangents != null) tangents[vertex_count] = origin_tangents[triangles[j + 1]];
                            if (normals != null) normals[vertex_count] = origin_normals[triangles[j + 1]];
                            if (uv != null) uv[vertex_count] = origin_uv[triangles[j + 1]];
                            if (uv2 != null) uv2[vertex_count] = origin_uv2[triangles[j + 1]];
                            imap.Add(triangles[j + 1], vertex_count);
                            vertex_count++;
                        }

                        if (!imap.ContainsKey(triangles[j + 2]))
                        {
                            if (vertices != null) vertices[vertex_count] = origin_vertices[triangles[j + 2]];
                            if (boneWeights != null) boneWeights[vertex_count] = origin_boneWeights[triangles[j + 2]];
                            if (colors != null) colors[vertex_count] = origin_colors[triangles[j + 2]];
                            if (colors32 != null) colors32[vertex_count] = origin_colors32[triangles[j + 2]];
                            if (tangents != null) tangents[vertex_count] = origin_tangents[triangles[j + 2]];
                            if (normals != null) normals[vertex_count] = origin_normals[triangles[j + 2]];
                            if (uv != null) uv[vertex_count] = origin_uv[triangles[j + 2]];
                            if (uv2 != null) uv2[vertex_count] = origin_uv2[triangles[j + 2]];
                            imap.Add(triangles[j + 2], vertex_count);
                            vertex_count++;
                        }
                    }
                }

                // set data back to mesh
                mesh.Clear(false);
                if (vertices != null)
                {
                    Vector3[] new_vertices = new Vector3[vertex_count];
                    Array.Copy(vertices, new_vertices, vertex_count);
                    mesh.vertices = new_vertices;
                }

                if (boneWeights != null)
                {
                    BoneWeight[] new_boneWeights = new BoneWeight[vertex_count];
                    Array.Copy(boneWeights, new_boneWeights, vertex_count);
                    mesh.boneWeights = new_boneWeights;
                }

                if (colors != null)
                {
                    Color[] new_colors = new Color[vertex_count];
                    Array.Copy(colors, new_colors, vertex_count);
                    mesh.colors = new_colors;
                }

                if (colors32 != null)
                {
                    Color32[] new_colors32 = new Color32[vertex_count];
                    Array.Copy(colors32, new_colors32, vertex_count);
                    mesh.colors32 = new_colors32;
                }

                if (tangents != null)
                {
                    Vector4[] new_tangents = new Vector4[vertex_count];
                    Array.Copy(tangents, new_tangents, vertex_count);
                    mesh.tangents = new_tangents;
                }

                if (normals != null)
                {
                    Vector3[] new_normals = new Vector3[vertex_count];
                    Array.Copy(normals, new_normals, vertex_count);
                    mesh.normals = new_normals;
                }

                if (uv != null)
                {
                    Vector2[] new_uv = new Vector2[vertex_count];
                    Array.Copy(uv, new_uv, vertex_count);
                    mesh.uv = new_uv;
                }

                if (uv2 != null)
                {
                    Vector2[] new_uv2 = new Vector2[vertex_count];
                    Array.Copy(uv2, new_uv2, vertex_count);
                    mesh.uv2 = new_uv2;
                }

                mesh.subMeshCount = origin_triangles.Length;
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    int[] new_triangles = new int[origin_triangles[i].Length];
                    for (int j = 0; j < new_triangles.Length; j += 3)
                    {
                        new_triangles[j] = (int) imap[origin_triangles[i][j]];
                        new_triangles[j + 1] = (int) imap[origin_triangles[i][j + 1]];
                        new_triangles[j + 2] = (int) imap[origin_triangles[i][j + 2]];
                    }

                    mesh.SetTriangles(new_triangles, i);
                }

                // refresh normals and bounds
                //mesh.RecalculateNormals();
                //mesh.RecalculateBounds();
            }


            private void get_all_meshes()
            {
                Mantis_Mesh = new Mantis_Mesh();
                Mantis_Mesh.mesh = target;
            }

            private void init_all()
            {
                if (Mantis_Mesh == null)
                {
                    if (target)
                    {
                        get_all_meshes();
                        if (Mantis_Mesh != null)
                        {
                            int triangle_number = Mantis_Mesh.mesh.triangles.Length;
                            Mantis_Mesh.origin_triangles = new int[Mantis_Mesh.mesh.subMeshCount][];
                            // out data is large than origin data
                            Mantis_Mesh.out_triangles = new int[triangle_number + Mantis_Mesh.mesh.subMeshCount];
                            for (int i = 0; i < Mantis_Mesh.mesh.subMeshCount; i++)
                            {
                                int[] sub_triangles = Mantis_Mesh.mesh.GetTriangles(i);
                                // save origin triangle list
                                Mantis_Mesh.origin_triangles[i] = new int[sub_triangles.Length];
                                Array.Copy(sub_triangles, Mantis_Mesh.origin_triangles[i], sub_triangles.Length);
                            }

                            Mantis_Mesh.index = -1;
                        }
                    }
                }
            }

            private void optimize()
            {
                if (target)
                {
                    if (Mantis_Mesh != null)
                    {
                        int triangle_number = Mantis_Mesh.mesh.triangles.Length;
                        Vector3[] vertices = Mantis_Mesh.mesh.vertices;
                        // in data is large than origin data
                        int[] triangles = new int[triangle_number + Mantis_Mesh.mesh.subMeshCount];
                        // we need normal data to protect normal boundary
                        Vector3[] normals = Mantis_Mesh.mesh.normals;
                        // we need color data to protect color boundary
                        Color[] colors = Mantis_Mesh.mesh.colors;
                        // we need uv data to protect uv boundary
                        Vector2[] uvs = Mantis_Mesh.mesh.uv;
                        int counter = 0;
                        for (int i = 0; i < Mantis_Mesh.mesh.subMeshCount; i++)
                        {
                            int[] sub_triangles = Mantis_Mesh.mesh.GetTriangles(i);
                            triangles[counter] = sub_triangles.Length;
                            counter++;
                            Array.Copy(sub_triangles, 0, triangles, counter, sub_triangles.Length);
                            counter += sub_triangles.Length;
                        }

                        // create progressive mesh
                        Mantis_Mesh.index = MantisLODSimpler.create_progressive_mesh(vertices, vertices.Length,
                            triangles,
                            counter, normals, normals.Length, colors, colors.Length, uvs, uvs.Length,
                            protect_boundary ? 1 : 0, protect_detail ? 1 : 0, protect_symmetry ? 1 : 0,
                            protect_normal ? 1 : 0, protect_shape ? 1 : 0);
                    }
                }
            }

            private void clean_all()
            {
                // restore triangle list
                if (Mantis_Mesh != null)
                {
                    if (target)
                    {
                        if (Mantis_Mesh.index != -1)
                        {
                            for (int i = 0; i < Mantis_Mesh.mesh.subMeshCount; i++)
                            {
                                Mantis_Mesh.mesh.SetTriangles(Mantis_Mesh.origin_triangles[i], i);
                            }

                            //child.mesh.RecalculateNormals();
                            //child.mesh.RecalculateBounds();
                            // do not need it
                            MantisLODSimpler.delete_progressive_mesh(Mantis_Mesh.index);


                            Mantis_Mesh.index = -1;
                        }
                    }

                    Mantis_Mesh = null;
                }
            }
        }

        /// <summary>
        /// 复制Mesh使其在运行时可读写
        /// </summary>
        public static Mesh MakeMeshReadable(Mesh originalMesh)
        {
            if (originalMesh == null) return null;

            Mesh newMesh = new Mesh();

            // 复制所有顶点数据
            newMesh.vertices = originalMesh.vertices;
            newMesh.normals = originalMesh.normals;
            newMesh.tangents = originalMesh.tangents;
            newMesh.uv = originalMesh.uv;
            newMesh.uv2 = originalMesh.uv2;
            newMesh.uv3 = originalMesh.uv3;
            newMesh.uv4 = originalMesh.uv4;
            newMesh.colors = originalMesh.colors;
            newMesh.colors32 = originalMesh.colors32;

            // 复制三角形数据
            newMesh.triangles = originalMesh.triangles;

            // 复制子网格信息（如果有多个材质）
            newMesh.subMeshCount = originalMesh.subMeshCount;
            for (int i = 0; i < originalMesh.subMeshCount; i++)
            {
                newMesh.SetTriangles(originalMesh.GetTriangles(i), i);
            }

            // 复制边界和骨骼权重（如果有）
            newMesh.bounds = originalMesh.bounds;
            newMesh.bindposes = originalMesh.bindposes;
            newMesh.boneWeights = originalMesh.boneWeights;

            newMesh.name = originalMesh.name + "_Readable";

            return newMesh;
        }
    }
}