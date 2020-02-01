using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using CUDAtrace.EditorViewport;
using CUDAtrace.Models;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTkControl;
using Vector3 = System.Numerics.Vector3;

namespace CUDAtrace
{
    class EditorViewportManager
    {
        private readonly OpenTkControlBase control;
        private Stopwatch deltaTimeStopwatch;
        private List<EditorObject> editorObjects = new List<EditorObject>();
        private Shader mainShader;
        private bool initialized = false;
        private JsonScene currentScene;
        private Matrix4 viewMatrix;
        private Matrix4 projectionMatrix;
        private float time;
        private Vector3 lightDirection;
        private Vector4 lightPosition;
        private Vector4 lightColor;

        public EditorViewportManager(OpenTkControlBase control)
        {
            this.control = control;

            control.FrameRateLimit = 60f;
            control.Continuous = true;
            control.GlRender += RenderCallback;

            deltaTimeStopwatch = new Stopwatch();
            deltaTimeStopwatch.Start();
        }

        private void Initialize()
        {
            initialized = true;

            string shadersFolder = Path.Combine(Environment.CurrentDirectory, "Shaders");
            mainShader = new Shader(Path.Combine(shadersFolder, "shader.vert"), Path.Combine(shadersFolder, "shader.frag"));

            GL.Enable(EnableCap.DepthTest);

            editorObjects.ForEach(e => e.Initialize());
        }

        private void RenderCallback(object? sender, OpenTkControlBase.GlRenderEventArgs e)
        {
            if (!initialized)
                Initialize();

            if (e.Resized)
            {
                GL.Viewport(0, 0, (int)control.ActualWidth, (int)control.ActualHeight);
            }

            deltaTimeStopwatch.Stop();
            float deltaTime = deltaTimeStopwatch.ElapsedMilliseconds / 1000f;
            deltaTimeStopwatch.Reset();
            deltaTimeStopwatch.Start();

            Render(deltaTime);
        }

        private void Render(float deltaTime)
        {
            if (control.Visibility == Visibility.Visible)
            {
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.ClearColor(0f, 0f, 0f, 1f);
                SetupCamera();
                time += deltaTime;

                int lightDirectionLocation = GL.GetUniformLocation(mainShader.Handle, "lightDirection");
                int lightPositionLocation = GL.GetUniformLocation(mainShader.Handle, "lightPosition");
                int lightColorLocation = GL.GetUniformLocation(mainShader.Handle, "lightColor");
                GL.Uniform3(lightDirectionLocation, lightDirection.X, lightDirection.Y, lightDirection.Z);
                GL.Uniform4(lightPositionLocation, lightPosition.X, lightPosition.Y, lightPosition.Z, lightPosition.W);
                GL.Uniform4(lightColorLocation, lightColor.X, lightColor.Y, lightColor.Z, lightColor.W);

                foreach (EditorObject editorObject in editorObjects)
                {
                    editorObject.Shader = mainShader;
                    editorObject.Render(deltaTime, viewMatrix, projectionMatrix);
                }
            }
        }

        public void LoadScene(JsonScene scene)
        {
            currentScene = scene;

            editorObjects.Clear();

            foreach (JsonGeometry geometry in scene.Geometry)
            {
                JsonMaterial material = scene.Materials.SingleOrDefault(m => m.ID == geometry.Material);
                switch (geometry.Type)
                {
                    case "plane":
                    {
                        editorObjects.Add(new Plane(geometry.Position, geometry.Normal, material));
                        break;
                    }
                    case "sphere":
                    {
                        editorObjects.Add(new Sphere(geometry.Position, geometry.Radius, material));
                        break;
                    }
                    case "disc":
                    {
                        editorObjects.Add(new Disc(geometry.Position, geometry.Normal, geometry.Radius, material));
                        break;
                    }
                }
            }

            foreach (JsonLight light in scene.Lights)
            {
                switch (light.Type)
                {
                    case "disc":
                    {
                        editorObjects.Add(new Disc(light.Position, light.Normal, light.Radius, new JsonMaterial(){Emission = new JsonEmissionSettings()
                        {
                            Color = light.Color,
                            Brightness = light.Brightness
                        }}));
                        lightPosition = new Vector4(light.Position.X, light.Position.Y, light.Position.Z, light.Radius);
                        lightDirection = Vector3.Normalize(light.Normal);
                        lightColor = new Vector4(light.Color.X, light.Color.Y, light.Color.Z, light.Brightness);
                        break;
                    }
                }
            }

            if (initialized)
            {
                editorObjects.ForEach(o => o.Initialize());
            }
        }

        private void SetupCamera()
        {
            viewMatrix = Matrix4.LookAt(currentScene.Camera.Position.X, currentScene.Camera.Position.Y,
                currentScene.Camera.Position.Z, currentScene.Camera.LookAt.X, currentScene.Camera.LookAt.Y,
                currentScene.Camera.LookAt.Z, 0f, 1f, 0f);

            float fov = MathF.Atan((0.5f * 36f) / currentScene.Camera.FocalLength) * 2f;

            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(fov,
                (float)(control.ActualWidth / control.ActualHeight), 0.01f, 1000f);
        }
    }
}
