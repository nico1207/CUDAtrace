using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ILGPU.Algorithms;

namespace CUDAtrace.Models
{
    public struct Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 LookAt { get; set; }
        public float FocalLength { get; set; }
        public float ScreenWidth { get; set; }
        public float ScreenHeight { get; set; }

        private Vector3 sphericalPosition;

        public Camera(Vector3 position, Vector3 lookAt, float focalLength, float screenWidth, float screenHeight)
        {
            Position = position;
            LookAt = lookAt;
            FocalLength = focalLength;
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;

            Vector3 relPos = position - lookAt;
            float dist = Vector3.Distance(position, lookAt);
            sphericalPosition = new Vector3(dist, XMath.Acos(relPos.Y / dist), XMath.Atan2(relPos.Z, relPos.X));
        }

        public Ray GetRaySimple(float x, float y)
        {
            x = (x / (ScreenWidth - 1f) - 0.5f) * (ScreenWidth / ScreenHeight);
            y = (1f - y / (ScreenHeight - 1f)) - 0.5f;

            Vector3 direction = Vector3.Normalize(new Vector3(x * 36f, y * 36f, FocalLength));

            float angle = -sphericalPosition.Y + XMath.PIHalf;
            float newY = direction.Y * XMath.Cos(angle) - direction.Z * XMath.Sin(angle);
            float newZ = direction.Z * XMath.Cos(angle) - direction.Y * XMath.Sin(angle);
            direction.Y = newY;
            direction.Z = newZ;

            angle = sphericalPosition.Z + XMath.PIHalf;
            float newX = direction.X * XMath.Cos(angle) - direction.Z * XMath.Sin(angle);
            newZ = direction.Z * XMath.Cos(angle) + direction.X * XMath.Sin(angle);
            direction.X = newX;
            direction.Z = newZ;

            return new Ray(Position, direction);
        }
    }
}
