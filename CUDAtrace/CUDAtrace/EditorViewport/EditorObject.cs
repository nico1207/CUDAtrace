using System;
using System.Collections.Generic;
using System.Text;
using CUDAtrace.Models;
using OpenTK;
using Vector3 = System.Numerics.Vector3;

namespace CUDAtrace.EditorViewport
{
    public abstract class EditorObject
    {
        public Shader Shader { get; set; }

        protected JsonMaterial material;

        private Vector3 position = Vector3.Zero;
        public Vector3 Position
        {
            get { return position; }
            set
            {
                position = value;
                UpdateTransformMatrix();
            }
        }

        private Vector3 rotation = Vector3.Zero;
        public Vector3 Rotation
        {
            get { return rotation; }
            set
            {
                rotation = value;
                UpdateTransformMatrix();
            }
        }

        private Vector3 scale = Vector3.One;
        public Vector3 Scale
        {
            get { return scale; }
            set
            {
                scale = value;
                UpdateTransformMatrix();
            }
        }

        protected Matrix4 transformMatrix;

        protected void UpdateTransformMatrix()
        {
            transformMatrix = Matrix4.CreateScale(scale.X, scale.Y, scale.Z) * Matrix4.CreateRotationX(rotation.X) *
                              Matrix4.CreateRotationY(rotation.Y) * Matrix4.CreateRotationZ(rotation.Z) *
                              Matrix4.CreateTranslation(position.X, position.Y, position.Z) * Matrix4.CreateScale(-1f, 1f, 1f);
        }

        public abstract void Initialize();

        public abstract void Render(float deltaTime, Matrix4 viewMatrix, Matrix4 projectionMatrix);
    }
}
