using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace CUDAtrace.Models
{
    public struct Intersection
    {
        public bool Hit { get; set; }
        public bool HitLight { get; set; }
        public float IntersectionLength { get; set; }
        public Vector3 HitPosition { get; set; }
        public Vector3 HitNormal { get; set; }
        public Geometry HitObject { get; set; }
        public Light HitLightObject { get; set; }
    }
}
