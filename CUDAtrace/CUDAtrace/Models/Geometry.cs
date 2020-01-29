using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ILGPU.Algorithms;

namespace CUDAtrace.Models
{
    public struct Geometry
    {
        public int GeometryType { get; set; } // 1: Sphere, 2: Plane, 3: Disc, 4: Cylinder
        public Vector3 Position { get; set; }
        public Material Material { get; set; }
        public float Radius { get; set; } // Used for sphere, cylinder and disc
        public Vector3 Normal { get; set; } // Used for plane and cylinder
        public float Height { get; set; } // Used for cylinder

        public static Geometry CreateSphere(Vector3 position, float radius, Material material)
        {
            return new Geometry()
            {
                GeometryType = 1,
                Position = position,
                Radius = radius,
                Material = material
            };
        }

        public static Geometry CreatePlane(Vector3 position, Vector3 normal, Material material)
        {
            return new Geometry()
            {
                GeometryType = 2,
                Position = position,
                Normal = Vector3.Normalize(normal),
                Material = material
            };
        }

        public static Geometry CreateDisc(Vector3 position, Vector3 normal, float radius, Material material)
        {
            return new Geometry()
            {
                GeometryType = 3,
                Position = position,
                Normal = Vector3.Normalize(normal),
                Radius = radius,
                Material = material
            };
        }

        public static Geometry CreateCylinder(Vector3 position, Vector3 direction, float radius, float height, Material material)
        {
            return new Geometry()
            {
                GeometryType = 4,
                Position = position,
                Normal = Vector3.Normalize(direction),
                Radius = radius,
                Height = height,
                Material = material
            };
        }

        public Intersection Trace(Ray ray)
        {
            return GeometryType switch
            {
                1 => TraceSphere(ray),
                2 => TracePlane(ray),
                3 => TraceDisc(ray),
                4 => TraceCylinder(ray),
                _ => new Intersection(),
            };
        }

        public Intersection TraceSphere(Ray ray)
        {
            Intersection result = new Intersection();

            Vector3 l = Position - ray.Origin;
            float dt = Vector3.Dot(l, ray.Direction);
            float r2 = Radius * Radius;

            float ct2 = Vector3.Dot(l, l) - dt * dt;

            if (ct2 > r2) return result;

            float at = XMath.Sqrt(r2 - ct2);

            float i1 = dt - at;
            float i2 = dt + at;
            result.HitObject = this;
            if (i1 <= 0)
            {
                Vector3 p = ray.Origin + ray.Direction * i2;
                Vector3 n = Vector3.Normalize(p - Position);
                result.IntersectionLength = i2;
                result.HitPosition = p;
                result.HitNormal = n;
            }
            else
            {
                Vector3 p = ray.Origin + ray.Direction * i1;
                Vector3 n = Vector3.Normalize(p - Position);
                result.IntersectionLength = i1;
                result.HitPosition = p;
                result.HitNormal = n;
            }

            result.Hit = i1 >= 0 || i2 >= 0;
            return result;
        }

        public Intersection TracePlane(Ray ray)
        {
            Intersection result = new Intersection();

            float denom = Vector3.Dot(Normal, ray.Direction);
            if (XMath.Abs(denom) > 1e-6)
            {
                float t = Vector3.Dot(Normal, Position - ray.Origin) / denom;
                if (t >= 0)
                {
                    result.Hit = true;
                    result.IntersectionLength = t;
                    result.HitPosition = ray.Origin + ray.Direction * t;
                    result.HitNormal = Normal;
                    result.HitObject = this;
                }
            }

            return result;
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
                    result.Hit = true;
                    result.IntersectionLength = t;
                    result.HitPosition = p;
                    result.HitNormal = Normal;
                    result.HitObject = this;
                }
            }

            return result;
        }

        public Intersection TraceCylinder(Ray ray)
        {
            Intersection result = new Intersection();
            
            Vector3 p0 = ray.Origin - Position;
            float dot = Vector3.Dot(Normal, ray.Direction);
            float dot2 = Vector3.Dot(Normal, p0);
            float a = Vector3.Dot(ray.Direction, ray.Direction) - dot * dot;
            float b = Vector3.Dot(ray.Direction, p0) - dot * dot2;
            float c = Vector3.Dot(p0, p0) - dot2 * dot2 - Radius * Radius;
            float delta = b * b - a * c;
            if (delta < 1e-6) return result;
            float sqrtDelta = XMath.Sqrt(delta);
            float i1 = (-b - sqrtDelta) / a;
            float i2 = (-b + sqrtDelta) / a;
            if (i1 < 0 && i2 < 0) return result;
            Vector3 p1 = ray.Origin + ray.Direction * i1;
            Vector3 p2 = ray.Origin + ray.Direction * i2;
            float y1 = Vector3.Dot(p1 - Position, Normal);
            float y2 = Vector3.Dot(p2 - Position, Normal);
            Vector3 n1 = p1 - (Position + Normal * y1);
            n1 /= XMath.Sqrt(Vector3.Dot(n1, n1));
            Vector3 n2 = p2 - (Position + Normal * y2);
            n2 /= XMath.Sqrt(Vector3.Dot(n2, n2));
            result.Hit = true;
            result.HitObject = this;
            result.IntersectionLength = i1;
            result.HitPosition = p1;
            result.HitNormal = n1;
            
            Geometry cap1 = CreateDisc(Position + Normal * Height * 0.5f, Normal, Radius, Material);
            Geometry cap2 = CreateDisc(Position - Normal * Height * 0.5f, -Normal, Radius, Material);
            
            if (y1 > Height * 0.5f || y1 < -Height * 0.5f || y2 > Height * 0.5f || y2 < -Height * 0.5f)
            {
                Intersection cap1Intersection = cap1.TraceDisc(ray);
                Intersection cap2Intersection = cap2.TraceDisc(ray);
                Intersection nearest;
                Intersection furthest;
                if (cap1Intersection.Hit && cap2Intersection.Hit)
                {
                    if (cap1Intersection.IntersectionLength < cap2Intersection.IntersectionLength)
                    {
                        nearest = cap1Intersection;
                        furthest = cap2Intersection;
                    }
                    else
                    {
                        nearest = cap2Intersection;
                        furthest = cap1Intersection;
                    }
                }
                else if (cap1Intersection.Hit && !cap2Intersection.Hit)
                {
                    nearest = cap1Intersection;
                    furthest = cap1Intersection;
                }
                else
                {
                    nearest = cap2Intersection;
                    furthest = cap2Intersection;
                }
            
                if (y1 > Height * 0.5f || y1 < -Height * 0.5f)
                {
                    result.Hit = nearest.Hit;
                    result.IntersectionLength = nearest.IntersectionLength;
                    result.HitPosition = nearest.HitPosition;
                    result.HitNormal = nearest.HitNormal;
                }
                if ((y2 > Height * 0.5f || y2 < -Height * 0.5f) && result.IntersectionLength < 0)
                {
                    result.Hit = furthest.Hit;
                    result.IntersectionLength = furthest.IntersectionLength;
                    result.HitPosition = furthest.HitPosition;
                    result.HitNormal = furthest.HitNormal;
                }
            }

            return result;
        }
    }
}
