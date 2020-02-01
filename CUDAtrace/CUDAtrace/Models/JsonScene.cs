using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CUDAtrace.Models
{
    public class JsonScene
    {
        public static JsonScene FromFile(string path) => JsonConvert.DeserializeObject<JsonScene>(File.ReadAllText(path), new VectorConverter());

        public string Name { get; set; }
        public JsonCamera Camera { get; set; }
        public JsonSkylight Skylight { get; set; }
        public JsonMaterial[] Materials { get; set; }
        public JsonGeometry[] Geometry { get; set; }
        public JsonLight[] Lights { get; set; }
    }

    public class JsonCamera
    {
        public Vector3 Position { get; set; }
        public Vector3 LookAt { get; set; }
        public float FocalLength { get; set; }
    }

    public class JsonSkylight
    {
        public Vector3 Color { get; set; }
        public float Brightness { get; set; }
    }

    public class JsonMaterial
    {
        public string ID { get; set; }
        public JsonDiffuseSettings Diffuse { get; set; }
        public JsonEmissionSettings Emission { get; set; }
        public JsonReflectionSettings Reflection { get; set; }
    }

    public class JsonDiffuseSettings
    {
        public Vector3 Color { get; set; }
        public float Albedo { get; set; }
    }

    public class JsonEmissionSettings
    {
        public Vector3 Color { get; set; }
        public float Brightness { get; set; }
    }

    public class JsonReflectionSettings
    {
        public Vector3 Color { get; set; }
        public float Reflectivity { get; set; }
        public float IOR { get; set; }
    }

    public class JsonGeometry
    {
        public string Type { get; set; }
        public string Material { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public float Radius { get; set; }
        public float Height { get; set; }
    }

    public class JsonLight {
        public string Type { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public Vector3 Color { get; set; }
        public float Radius { get; set; }
        public float Brightness { get; set; }
    }
}
