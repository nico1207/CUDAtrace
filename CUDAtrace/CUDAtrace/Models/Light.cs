using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;

namespace CUDAtrace.Models
{
    public struct Light
    {
        public int LightType { get; set; } // 1: Sphere, 2: Disc, 3: Cylinder
        public Vector3 Position { get; set; }
        public float Radius { get; set; } // Used for sphere, cylinder and disc
        public Vector3 Normal { get; set; } // Used for cylinder
        public float Height { get; set; } // Used for cylinder
        public Vector3 Color { get; set; }
        public float Brightness { get; set; }

        public static Light CreateDisc(Vector3 position, Vector3 normal, float radius, Vector3 color, float brightness)
        {
            return new Light()
            {
                LightType = 2,
                Position = position,
                Normal = Vector3.Normalize(normal),
                Radius = radius,
                Color = color,
                Brightness = brightness
            };
        }

        public Intersection Trace(Ray ray)
        {
            return LightType switch
            {
                2 => TraceDisc(ray),
                _ => new Intersection(),
            };
        }

        public Intersection TraceDisc(Ray ray)
        {
            Intersection result = new Intersection();

            float denom = Vector3.Dot(Normal, ray.Direction);
            if (XMath.Abs(denom) > 1e-6)
            {
                float t = Vector3.Dot(Normal, Position - ray.Origin) / denom;
                Vector3 p = ray.Origin + ray.Direction * t;
                if (t >= 0 && Vector3.DistanceSquared(Position, p) <= Radius * Radius)
                {
                    result.HitLight = true;
                    result.IntersectionLength = t;
                    result.HitPosition = p;
                    result.HitNormal = Normal;
                    result.HitLightObject = this;
                }
            }

            return result;
        }

        public Vector3 GetColor(Vector3 dir)
        {
            if (Vector3.Dot(Normal, dir) > 0) return new Vector3();
            return Color * Brightness;
        }

        public float GetAttenuation(float distance, Vector3 dir)
        {
            switch (LightType)
            {
                case 2: // Disc
                {
                    if (-Vector3.Dot(Normal, dir) <= 0) return 0;
                    return 1f / ((distance * distance) / (XMath.Max(-Vector3.Dot(Normal, dir), 0) * Radius * Radius * XMath.PI)) / XMath.PI;
                }
            }

            return 0f;
        }

        public Vector3 GetPoint(XorShift64Star random)
        {
            switch (LightType)
            {
                case 2:
                {
                    Vector3 v1 = Helper.GetPerpendicularVector(Normal);
                    Vector3 v2 = Vector3.Cross(v1, Normal);
                    float angle = random.NextFloat() * XMath.PI * 2f;
                    float l = XMath.Sqrt(random.NextFloat()) * Radius;
                    return v1 * XMath.Cos(angle) * l + v2 * XMath.Sin(angle) * l + Position;
                }
            }
            
            return Vector3.Zero;
        }
    }
}
