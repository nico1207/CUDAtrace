using ILGPU.Algorithms.Random;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ILGPU.Algorithms;

namespace CUDAtrace
{
    public static class Helper
    {
        public static Vector3 GetPerpendicularVector(Vector3 v)
        {
            Vector3 v2;
            if (v.X != 0 && v.Y == 0 && v.Z == 0) v2 = new Vector3(0, 0, 1);
            else if (v.X == 0 && v.Y != 0 && v.Z == 0) v2 = new Vector3(1, 0, 0);
            else if (v.X == 0 && v.Y == 0 && v.Z != 0) v2 = new Vector3(0, 1, 0);
            else v2 = new Vector3(1, 0, 0);
            return Vector3.Normalize(Vector3.Cross(v2, v));
        }

        public static Vector3 GetGGXMicrofacet(XorShift64Star random, float roughness, Vector3 normal)
        {
            float rand1 = random.NextFloat();
            float rand2 = random.NextFloat();

            Vector3 B = GetPerpendicularVector(normal);
            Vector3 T = Vector3.Cross(B, normal);

            float a2 = roughness * roughness;
            float cosThetaH = XMath.Sqrt(XMath.Max(0f, (1f - rand1) / ((a2 - 1f) * rand1 + 1f)));
            float sinThetaH = XMath.Sqrt(XMath.Max(0f, 1f - cosThetaH * cosThetaH));
            float phiH = rand2 * XMath.PI * 2f;

            return T * (sinThetaH * XMath.Cos(phiH)) +
                   B * (sinThetaH * XMath.Sin(phiH)) +
                   normal * cosThetaH;
        }

        public static float Schlick(Vector3 nrm, Vector3 dir, float n1, float n2)
        {
            float r0 = (n1 - n2) / (n1 + n2);
            r0 *= r0;
            float cosI = XMath.Max(Vector3.Dot(-nrm, dir), 0f);
            if (n1 > n2)
            {
                float n = n1 / n2;
                float sinT2 = n * n * (1f - cosI * cosI);
                if (sinT2 > 1.0) return 1f;
                cosI = XMath.Sqrt(1f - sinT2);
            }
            float x = 1f - cosI;
            return r0 + (1f - r0) * x * x * x * x * x;
        }

        public static float ggxNormalDistribution(float NdotH, float roughness)
        {
            float a2 = roughness * roughness;
            float d = ((NdotH * a2 - NdotH) * NdotH + 1);
            return a2 / (d * d * XMath.PI);
        }
    }
}
