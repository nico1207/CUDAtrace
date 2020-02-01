using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using CUDAtrace.Models;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Vector3 = System.Numerics.Vector3;

namespace CUDAtrace.EditorViewport
{
    public class Sphere : EditorObject
    {
        private int vertexBuffer;
        private int vertexArray;
        private int elementBuffer;

        private Vector3[] vertices;
        private uint[] indices;
        private float time;


        public Sphere(Vector3 position, float radius, JsonMaterial material)
        {
            Position = position;
            Scale = new Vector3(radius);
            this.material = material;
        }

        private const int parallels = 32;
        private const int meridians = 32;

        public override void Initialize()
        {
            List<Vector3> vertexList = new List<Vector3>();
            List<uint> triangleList = new List<uint>();

            void AddQuad(uint a, uint b, uint c, uint d)
            {
                triangleList.Add(a);
                triangleList.Add(b);
                triangleList.Add(c);
                triangleList.Add(a);
                triangleList.Add(c);
                triangleList.Add(d);
            }
            void AddTriangle(uint a, uint b, uint c)
            {
                triangleList.Add(a);
                triangleList.Add(b);
                triangleList.Add(c);
            }

            vertexList.Add(new Vector3(0f, 1f, 0f));
            for (int j = 0; j < parallels - 1; ++j)
            {
                float polar = MathF.PI * ((float) j + 1) / parallels;
                float sp = MathF.Sin(polar);
                float cp = MathF.Cos(polar);
                for (int i = 0; i < meridians; ++i)
                {
                    float azimuth = 2f * MathF.PI * ((float) i / meridians);
                    float sa = MathF.Sin(azimuth);
                    float ca = MathF.Cos(azimuth);
                    float x = sp * ca;
                    float y = cp;
                    float z = sp * sa;
                    vertexList.Add(new Vector3(x, y, z));
                }
            }
            vertexList.Add(new Vector3(0f, -1f, 0f));

            for (uint i = 0; i < meridians; ++i)
            {
                uint a = i + 1;
                uint b = (i + 1) % meridians + 1;
                AddTriangle(0, b, a);
            }

            for (uint j = 0; j < parallels - 2; ++j)
            {
                uint aStart = j * meridians + 1;
                uint bStart = (j + 1) * meridians + 1;
                for (uint i = 0; i < meridians; ++i)
                {
                    uint a = aStart + i;
                    uint a1 = aStart + (i + 1) % meridians;
                    uint b = bStart + i;
                    uint b1 = bStart + (i + 1) % meridians;
                    AddQuad(a, a1, b1, b);
                }
            }

            for (uint i = 0; i < meridians; ++i)
            {
                uint a = i + meridians * (parallels - 2) + 1;
                uint b = (i + 1) % meridians + meridians * (parallels - 2) + 1;
                AddTriangle((uint)vertexList.Count - 1, a, b);
            }

            for (int i = vertexList.Count - 1; i >= 0; --i)
            {
                Vector3 vert = vertexList[i];
                vertexList.Insert(i, vert);
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
