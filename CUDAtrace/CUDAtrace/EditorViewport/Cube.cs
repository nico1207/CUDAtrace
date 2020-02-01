using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Vector3 = System.Numerics.Vector3;

namespace CUDAtrace.EditorViewport
{
    public class Cube : EditorObject
    {
        private int vertexBuffer;
        private int vertexArray;
        private int elementBuffer;

        private Vector3[] vertices;
        private uint[] indices;
        private float time;

        public Cube(Vector3 pos)
        {
            Position = pos;
            Scale = new Vector3(10f);
        }

        public override void Initialize()
        {
            vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f,  -0.5f),
                new Vector3(0.5f, -0.5f,  -0.5f),
                new Vector3(0.5f, 0.5f,  -0.5f),
                new Vector3(-0.5f, 0.5f,  -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3(0.5f, -0.5f,  0.5f),
                new Vector3(0.5f, 0.5f,  0.5f),
                new Vector3(-0.5f, 0.5f,  0.5f),
            };

            indices = new uint[]
            {
                0, 2, 1,
                0, 3, 2,
                //back
                1, 2, 6,
                6, 5, 1,
                //right
                4, 5, 6,
                6, 7, 4,
                //top
                2, 3, 6,
                6, 3, 7,
                //front
                0, 7, 3,
                0, 4, 7,
                //bottom
                0, 1, 5,
                0, 5, 4
            };

            vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(vertexArray);

            vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float) * 3, vertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

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
            int transformMatrixLocation = GL.GetUniformLocation(Shader.Handle, "transform");
            Matrix4 mvpMatrix = transformMatrix * viewMatrix * projectionMatrix;
            GL.UniformMatrix4(transformMatrixLocation, false, ref mvpMatrix);

            GL.BindVertexArray(vertexArray);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
        }
    }
}
