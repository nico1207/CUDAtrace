using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using CUDAtrace.Models;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;

namespace CUDAtrace
{
    public class Raytracer
    {
        public static void Kernel(Index2 index, ArrayView2D<Vector3> dataView, Scene scene, ArrayView<Geometry> sceneGeometry, ArrayView<Light> sceneLights, int seed)
        {
            Vector3 outputColor = Vector3.Zero;
            scene.SceneGeometry = sceneGeometry;
            scene.SceneLights = sceneLights;
            //Models.Random random = new Models.Random((uint) (index.X * index.Y + seed));
            XorShift64Star random = new XorShift64Star((ulong)index.X + (ulong)scene.Camera.ScreenWidth * (ulong)index.Y + (ulong)seed);

            float xOffset = random.NextFloat();
            random.NextFloat();
            float yOffset = random.NextFloat();

            Ray ray = scene.Camera.GetRaySimple(index.X + (xOffset * 2f - 1f) * 0.5f, index.Y + (yOffset * 2f - 1f) * 0.5f);

            Intersection intersection = scene.TraceScene(ray);
            if (intersection.Hit)
            {
                outputColor = intersection.HitObject.Material.GetShaded(random, scene, intersection.HitPosition, ray.Direction, intersection.HitNormal, 2, 2, 2);
            }
            else if (intersection.HitLight)
            {
                outputColor = intersection.HitLightObject.GetColor(ray.Direction);
            }

            dataView[index] += outputColor;
        }
    }
}
