using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace CUDAtrace.Models
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
    }
}
