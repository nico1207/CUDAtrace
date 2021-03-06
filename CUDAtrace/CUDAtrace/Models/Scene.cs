﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ILGPU;

namespace CUDAtrace.Models
{
    public struct Scene
    {
        public Camera Camera { get; set; }
        public ArrayView<Geometry> SceneGeometry { get; set; }
        public ArrayView<Light> SceneLights { get; set; }
        public Vector3 SkylightColor { get; set; }
        public float SkylightBrightness { get; set; }

        public Intersection TraceScene(Ray ray)
        {
            Intersection closestIntersection = new Intersection();
            Intersection closestLightIntersection = new Intersection();

            for (int i = 0; i < SceneGeometry.Length; i++)
            {
                Intersection intersection = SceneGeometry[i].Trace(ray);
                if (intersection.Hit && (!closestIntersection.Hit ||
                                         intersection.IntersectionLength < closestIntersection.IntersectionLength))
                    closestIntersection = intersection;
            }
            for (int i = 0; i < SceneLights.Length; i++)
            {
                Intersection intersection = SceneLights[i].Trace(ray);
                if (intersection.HitLight && (!closestLightIntersection.HitLight || 
                                         intersection.IntersectionLength < closestLightIntersection.IntersectionLength))
                    closestLightIntersection = intersection;
            }

            if (!closestIntersection.Hit || (closestIntersection.Hit && closestLightIntersection.HitLight &&
                                                                   closestLightIntersection.IntersectionLength <
                                                                   closestIntersection.IntersectionLength))
            {
                closestIntersection = closestLightIntersection;
            }

            return closestIntersection;
        }
    }
}
