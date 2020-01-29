using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;

namespace CUDAtrace4.Models
{
    public static class Extensions
    {
        public static Vector3 PointOnHemisphere(this XorShift64Star random, Vector3 n)
        {
            float azimuthal = random.NextFloat() * XMath.PI * 2f;
            float z = random.NextFloat();
            float xyproj = XMath.Sqrt(1 - z * z);
            Vector3 v = new Vector3(xyproj * XMath.Cos(azimuthal), xyproj * XMath.Sin(azimuthal), z);
            if (Vector3.Dot(v, n) < 0)
                return v * -1;
            else 
                return v;
        }
    }
}
