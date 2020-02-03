using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Windows.Threading;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;

namespace CUDAtrace.Models
{
    public struct Material
    {
        public Vector3 DiffuseColor { get; set; }
        public float Diffuse { get; set; }
        public Vector3 ReflectionColor { get; set; }
        public float Reflectivity { get; set; }
        public float ReflectionIOR { get; set; }
        public Vector3 RefractionColor { get; set; }
        public float Refractivity { get; set; }
        public float RefractionIOR { get; set; }
        public float Dispersion { get; set; }
        public Vector3 EmissionColor { get; set; }
        public float Emission { get; set; }

        public Material Create()
        {
            ReflectionIOR = 1f;

            return this;
        }

        public Material SetDiffuse(Vector3 color, float diffuse)
        {
            Diffuse = diffuse;
            DiffuseColor = color;

            return this;
        }

        public Material SetReflection(Vector3 color, float reflectivity, float ior)
        {
            Reflectivity = reflectivity;
            ReflectionColor = color;
            ReflectionIOR = ior;

            return this;
        }

        public Material SetRefraction(Vector3 color, float refractivity, float ior, float dispersion)
        {
            Refractivity = refractivity;
            RefractionColor = color;
            RefractionIOR = ior;
            Dispersion = dispersion;

            return this;
        }

        public Material SetEmission(Vector3 color, float emission)
        {
            Emission = emission;
            EmissionColor = color;

            return this;
        }

        public Vector3 GetShaded(XorShift64Star random, Scene scene, Vector3 position, Vector3 direction, Vector3 normal)
        {
            Vector3 shaded = new Vector3();

            float fresnel = Helper.Schlick(normal, direction, 1f, ReflectionIOR);

            if (Diffuse > 0)
            {
                shaded += GetDirectDiffuse(random, scene, position, normal);// * (1f - fresnel);
                shaded += GetIndirectDiffuse(random, scene, position, normal);// * (1f - fresnel);
            }

            if (Reflectivity > 0)
            {
                shaded += GetReflection(random, scene, position, direction, normal) * fresnel;
                shaded += GetDirectReflection(random, scene, position, direction, normal) * fresnel;
            }


            shaded += GetEmissive(direction, normal);

            return shaded;
        }

        public Vector3 GetDirectReflection(XorShift64Star random, Scene scene, Vector3 position, Vector3 direction,
            Vector3 normal)
        {
            float roughness = 0.01f;

            Vector3 lightAccumulator = new Vector3();
            for (int i = 0; i < scene.SceneLights.Length; i++)
            {
                random.NextFloat();
                Light l = scene.SceneLights[i];
                Vector3 ldir = l.GetPoint(random) - position;
                ldir /= XMath.Sqrt(Vector3.Dot(ldir, ldir));

                Ray r = new Ray(position + ldir * 0.002f, ldir);
                Intersection intersection = scene.TraceScene(r);
                if (!intersection.HitLight) continue;



                float probability = Helper.ggxNormalDistribution(Vector3.Dot(Vector3.Reflect(-ldir, normal), -direction), roughness);

                Vector3 reflectionColor = intersection.HitLightObject.GetColor(ldir);

                lightAccumulator += reflectionColor * probability;
            }

            return lightAccumulator;
        }

        public Vector3 GetReflection(XorShift64Star random, Scene scene, Vector3 position, Vector3 direction,
            Vector3 normal)
        {
            float roughness = 0.01f;
            Vector3 accu = Vector3.Zero;

            for (int i = 0; i < 2; i++)
            {
                random.NextFloat();
                Vector3 microfacetNormal = Helper.GetGGXMicrofacet(random, roughness, normal);
                Vector3 reflectedDir = Vector3.Reflect(direction, microfacetNormal);

                Ray r = new Ray(position + reflectedDir * 0.002f, reflectedDir);
                Intersection intersection = scene.TraceScene(r);
                if (!intersection.Hit) continue;

                Material mat = intersection.HitObject.Material;
                Vector3 reflectionColor = mat.GetShaded2(random, scene, intersection.HitPosition, reflectedDir,
                    intersection.HitNormal);

                accu += reflectionColor;
            }

            return accu / 2f;
        }

        public Vector3 GetReflection2(XorShift64Star random, Scene scene, Vector3 position, Vector3 direction,
            Vector3 normal)
        {
            float roughness = 0.01f;
            Vector3 accu = Vector3.Zero;

            for (int i = 0; i < 2; i++)
            {
                random.NextFloat();
                Vector3 microfacetNormal = Helper.GetGGXMicrofacet(random, roughness, normal);
                Vector3 reflectedDir = Vector3.Reflect(direction, microfacetNormal);

                Ray r = new Ray(position + reflectedDir * 0.002f, reflectedDir);
                Intersection intersection = scene.TraceScene(r);
                if (!intersection.Hit) continue;

                Material mat = intersection.HitObject.Material;
                Vector3 reflectionColor = mat.GetShaded3(random, scene, intersection.HitPosition, reflectedDir,
                    intersection.HitNormal);

                accu += reflectionColor;
            }

            return accu / 2f;
        }

        public Vector3 GetDirectDiffuse(XorShift64Star random, Scene scene, Vector3 position, Vector3 normal)
        {
            Vector3 lightAccumulator = new Vector3();
            for (int i = 0; i < scene.SceneLights.Length; i++)
            {
                Light l = scene.SceneLights[i];
                Vector3 dir = Vector3.Normalize(l.GetPoint(random) - position);
                if (Vector3.Dot(normal, dir) <= 0) continue;
                Ray r = new Ray(position + dir * 0.002f, dir);
                Intersection intersection = scene.TraceScene(r);
                if (!intersection.HitLight) continue;
                Vector3 lightC = l.GetColor(dir) * Vector3.Dot(dir, normal) * l.GetAttenuation(intersection.IntersectionLength, dir);
                lightAccumulator += Vector3.Multiply(lightC, DiffuseColor * Diffuse);
            }

            return lightAccumulator;
        }

        public Vector3 GetIndirectDiffuse(XorShift64Star random, Scene scene, Vector3 position, Vector3 normal)
        {
            Vector3 lightAccumulator = new Vector3();
            int samples = 4;
            for (int i = 0; i < samples; i++)
            {
                float azimuthal = random.NextFloat() * XMath.PI * 2f;
                float z = random.NextFloat();
                float xyproj = XMath.Sqrt(1 - z * z);
                Vector3 dir = new Vector3(xyproj * XMath.Cos(azimuthal), xyproj * XMath.Sin(azimuthal), z);
                if (Vector3.Dot(dir, normal) < 0)
                    dir *= -1;
                Ray ray = new Ray(position + dir * 0.002f, dir);
                Intersection intersection = scene.TraceScene(ray);
                if (!intersection.Hit && !intersection.HitLight)
                {
                    lightAccumulator += Vector3.Multiply(scene.SkylightColor * scene.SkylightBrightness * Vector3.Dot(dir, normal), DiffuseColor) * Diffuse;
                    continue;
                }
                else if (!intersection.Hit || intersection.HitLight) continue;
                Material hitMaterial = intersection.HitObject.Material;
                Vector3 lightC = hitMaterial.GetShaded2(random, scene, intersection.HitPosition, dir, intersection.HitNormal);
                lightC *= Vector3.Dot(dir, normal);
                lightAccumulator += Vector3.Multiply(lightC, DiffuseColor) * Diffuse;
            }

            lightAccumulator /= (float)samples;

            return lightAccumulator;
        }

        public Vector3 GetShaded2(XorShift64Star random, Scene scene, Vector3 position, Vector3 direction, Vector3 normal)
        {
            Vector3 shaded = new Vector3();

            float fresnel = Helper.Schlick(normal, direction, 1f, ReflectionIOR);

            if (Diffuse > 0)
            {
                shaded += GetDirectDiffuse(random, scene, position, normal) * (1f - fresnel);
                shaded += GetIndirectDiffuse2(random, scene, position, normal) * (1f - fresnel);
            }

            if (Reflectivity > 0)
            {
                shaded += GetReflection2(random, scene, position, direction, normal) * fresnel;
            }


            shaded += GetEmissive(direction, normal);

            return shaded;
        }

        public Vector3 GetIndirectDiffuse2(XorShift64Star random, Scene scene, Vector3 position, Vector3 normal)
        {
            Vector3 lightAccumulator = new Vector3();
            int samples = 4;
            for (int i = 0; i < samples; i++)
            {
                float azimuthal = random.NextFloat() * XMath.PI * 2f;
                float z = random.NextFloat();
                float xyproj = XMath.Sqrt(1 - z * z);
                Vector3 dir = new Vector3(xyproj * XMath.Cos(azimuthal), xyproj * XMath.Sin(azimuthal), z);
                if (Vector3.Dot(dir, normal) < 0)
                    dir *= -1;
                Ray ray = new Ray(position + dir * 0.002f, dir);
                Intersection intersection = scene.TraceScene(ray);
                if (!intersection.Hit && !intersection.HitLight)
                {
                    lightAccumulator += Vector3.Multiply(scene.SkylightColor * scene.SkylightBrightness * Vector3.Dot(dir, normal), DiffuseColor) * Diffuse;
                    continue;
                }
                else if (!intersection.Hit || intersection.HitLight) continue;
                Material hitMaterial = intersection.HitObject.Material;
                Vector3 lightC = hitMaterial.GetShaded3(random, scene, intersection.HitPosition, dir, intersection.HitNormal);
                lightC *= Vector3.Dot(dir, normal);
                lightAccumulator += Vector3.Multiply(lightC, DiffuseColor) * Diffuse;
            }

            lightAccumulator /= (float)samples;

            return lightAccumulator;
        }

        public Vector3 GetShaded3(XorShift64Star random, Scene scene, Vector3 position, Vector3 direction, Vector3 normal)
        {
            Vector3 shaded = new Vector3();

            float fresnel = Helper.Schlick(normal, direction, 1f, ReflectionIOR);

            if (Diffuse > 0)
            {
                shaded += GetDirectDiffuse(random, scene, position, normal) * (1f - fresnel);
                shaded += GetIndirectDiffuse3(random, scene, position, normal) * (1f - fresnel);
            }

            shaded += GetEmissive(direction, normal);

            return shaded;
        }

        public Vector3 GetIndirectDiffuse3(XorShift64Star random, Scene scene, Vector3 position, Vector3 normal)
        {
            return GetDirectDiffuse(random, scene, position, normal);
        }

        public Vector3 GetEmissive(Vector3 direction, Vector3 normal)
        {
            if (Vector3.Dot(normal, direction) > 0) return Vector3.Zero;
            else return EmissionColor * Emission;
        }
    }
}
