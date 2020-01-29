using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace CUDAtrace4.Models
{
    public struct Ray
    {
        public Vector3 Origin;
        public Vector3 Direction;

        public Ray(Vector3 origin, Vector3 direction)
        {
            this.Origin = origin;
            this.Direction = direction;
        }
    }
}
