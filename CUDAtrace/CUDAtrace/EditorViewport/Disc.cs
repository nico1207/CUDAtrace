﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using CUDAtrace.Models;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Vector3 = System.Numerics.Vector3;

namespace CUDAtrace.EditorViewport
{
    public class Disc : EditorObject
    {
        private int vertexBuffer;
        private int vertexArray;
        private int elementBuffer;

        private Vector3[] vertices;
        private uint[] indices;
        private float time;

        private Vector3 normal;

        public Disc(Vector3 position, Vector3 normal, float radius, JsonMaterial material)
        {
            Position = position;
            Scale = new Vector3(radius);
            this.normal = Vector3.Normalize(normal);
            this.material = material;
        }

        private const int steps = 32;

        public override void Initialize()
        {
            //Vector3 v1 = Helper.GetPerpendicularVector(normal);
            //Vector3 v2 = Vector3.Cross(v1, normal);
            //v1 *= 0.5f;
            //v2 *= 0.5f;
            //vertices = new Vector3[] // Position | Normal
            //{
            //    -v1 - v2, normal,
            //    -v1 + v2, normal,
            //    v1 - v2, normal,
            //    v1 + v2, normal,
            //};
            //
            //indices = new uint[]
            //{
            //    0, 2, 1,
            //    2, 3, 1
            //};

            List<Vector3> vertexList = new List<Vector3>();
            List<uint> triangleList = new List<uint>();
            void AddTriangle(uint a, uint b, uint c)
            {
                triangleList.Add(a);
                triangleList.Add(b);
                triangleList.Add(c);
            }

            Vector3 v1 = Helper.GetPerpendicularVector(normal);
            Vector3 v2 = Vector3.Cross(v1, normal);

            vertexList.Add(new Vector3(0f, 0f, 0f));
            for (int i = 0; i < steps; ++i)
            {
                float angle = ((float)i / steps) * MathF.PI * 2f;
                vertexList.Add(v1 * MathF.Cos(angle) + v2 * MathF.Sin(angle));
            }

            for (uint i = 1; i < steps - 1; ++i)
            {
                AddTriangle(0, i, i + 1);
            }
            AddTriangle(0, steps - 1, 1);

            // Add normals to vertex list
            for (int i = vertexList.Count - 1; i >= 0; --i)
            {
                Vector3 vert = vertexList[i];
                vertexList.Insert(i + 1, normal);
            }

            vertices = vertexList.ToArray();
            indices = triangleList.ToArray();

            vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(vertexArray);

            vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float) * 3, vertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);

            elementBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            UpdateTransformMatrix();
        }

        public override void Render(float deltaTime, Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            time += deltaTime;

            //Rotation = new Vector3(0, time, 0);

            Shader.Use();
            int modelMatrixLocation = GL.GetUniformLocation(Shader.Handle, "model");
            int viewMatrixLocation = GL.GetUniformLocation(Shader.Handle, "view");
            int projectionMatrixLocation = GL.GetUniformLocation(Shader.Handle, "projection");
            int normalMatrixLocation = GL.GetUniformLocation(Shader.Handle, "normal_mat");
            Matrix4 transposedInvertedModelView = Matrix4.Transpose(Matrix4.Invert(transformMatrix * viewMatrix));
            GL.UniformMatrix4(modelMatrixLocation, false, ref transformMatrix);
            GL.UniformMatrix4(viewMatrixLocation, false, ref viewMatrix);
            GL.UniformMatrix4(projectionMatrixLocation, false, ref projectionMatrix);
            GL.UniformMatrix4(normalMatrixLocation, false, ref transposedInvertedModelView);

            int diffuseColorLocation = GL.GetUniformLocation(Shader.Handle, "diffuse");
            if (material.Diffuse != null)
            {
                GL.Uniform4(diffuseColorLocation, material.Diffuse.Color.X, material.Diffuse.Color.Y, material.Diffuse.Color.Z, material.Diffuse.Albedo);
            }
            else
            {
                GL.Uniform4(diffuseColorLocation, 0f, 0f, 0f, 0f);
            }

            int emissionColorLocation = GL.GetUniformLocation(Shader.Handle, "emission");
            if (material.Emission != null)
            {
                GL.Uniform4(emissionColorLocation, material.Emission.Color.X, material.Emission.Color.Y, material.Emission.Color.Z, material.Emission.Brightness);
            }
            else
            {
                GL.Uniform4(emissionColorLocation, 0f, 0f, 0f, 0f);
            }

            GL.BindVertexArray(vertexArray);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
        }
    }
}
