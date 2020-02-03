using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CUDAtrace.Models;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using Microsoft.Win32;
using Geometry = CUDAtrace.Models.Geometry;

namespace CUDAtrace
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int RenderWidth = 3840;
        public int RenderHeight = 2160;

        private Context context;
        private Accelerator accelerator;
        private Vector3[] colorBuffer;
        private int passes = 0;
        private int updateTimer = 5;
        private DateTime renderStartTime;
        private Task renderTask;
        private bool rendering = false;
        private Geometry[] sceneGeometry;
        private Light[] sceneLights;
        private Scene scene;
        private EditorViewportManager editorViewport;
        private ViewportMode currentViewportMode;

        public MainWindow()
        {
            InitializeComponent();
            editorViewport = new EditorViewportManager(openTkControl);

            //outputImage.Width = RenderWidth;
            //outputImage.Height = RenderHeight;
            RenderOptions.SetBitmapScalingMode(outputImage, BitmapScalingMode.HighQuality);

            //GPUlabel.Content = $"GPU: {accelerator.Name}";
            SetResolution();
            LoadScene(Path.Combine(Environment.CurrentDirectory, "Scenes", "TestScene.json"));

            SetEditorViewportMode();
        }

        private void SetResolution()
        {
            RenderWidth = int.Parse(widthTextbox.Text);
            RenderHeight = int.Parse(heightTextbox.Text);
            colorBuffer = new Vector3[RenderWidth * RenderHeight];
            passes = 0;
            UpdateBitmap();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (startRenderButton.Content.ToString() == "Start Rendering")
            {
                denoiseButton.IsEnabled = false;
                SetRenderingViewportMode();
                renderStartTime = DateTime.Now;
                int device = deviceComboBox.SelectedIndex;
                renderTask = Task.Run(() =>
                {
                    context = new Context(ContextFlags.FastMath | ContextFlags.AggressiveInlining);
                    context.EnableAlgorithms();
                    accelerator = device switch
                    {
                        0 => new CudaAccelerator(context),
                        1 => new CPUAccelerator(context),
                        2 => new CLAccelerator(context, CLAccelerator.AllCLAccelerators[0]),
                        _ => accelerator
                    };

                    Dispatcher.Invoke(() => { startRenderButton.Content = "Stop Rendering"; });

                    rendering = true; 
                    Render();

                    accelerator.Dispose();
                    context.Dispose();

                    Dispatcher.Invoke(UpdateBitmap);
                });
            }
            else
            {
                denoiseButton.IsEnabled = true;
                rendering = false;
                startRenderButton.Content = "Start Rendering";
            }
        }

        private void UpdateBitmap()
        {
            var bitmap = new WriteableBitmap(RenderWidth, RenderHeight, 1.0, 1.0, PixelFormats.Rgb24, null);
            outputImage.Source = bitmap;

            bitmap.Lock();
            IntPtr buffer = bitmap.BackBuffer;
            unsafe
            {
                for (int y = 0; y < RenderHeight; y++)
                {
                    for (int x = 0; x < RenderWidth; x++)
                    {
                        Vector3 col = colorBuffer[x + RenderWidth * y] / passes;
                        col = Aces(col, 2f);
                        //byte* colPtr = (byte*)&col;
                        *(byte*)(buffer) = (byte)(col.X * 255);
                        *(byte*)(buffer + 1) = (byte)(col.Y * 255);
                        *(byte*)(buffer + 2) = (byte)(col.Z * 255);

                        buffer += 3;
                    }
                }
            }
            bitmap.Unlock();
            bitmap.Freeze();
        }

        private static Vector3 Aces(Vector3 col, float exposure)
        {
            col *= exposure;
            float a = 2.51f;
            float b = 0.03f;
            float c = 2.43f;
            float d = 0.59f;
            float e = 0.14f;

            return Vector3.Clamp((col * (a * col + new Vector3(b))) / (col * (c * col + new Vector3(d)) + new Vector3(e)), Vector3.Zero, Vector3.One);
        }

        private void MainWindow_OnClosed(object? sender, EventArgs e)
        {
            if (accelerator != null)
            {
                accelerator.Dispose();
                context.Dispose();
            }
        }

        private void Render()
        {
            Vector3[] output = new Vector3[RenderWidth * RenderHeight];

            var myKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, ArrayView2D<Vector3>, Scene, ArrayView<Models.Geometry>, ArrayView<Light>, int>(Raytracer.Kernel);

            using (var gpuBuffer = accelerator.Allocate<Vector3>(new Index2(RenderWidth, RenderHeight)))
            {
                using var geometryBuffer = accelerator.Allocate<Geometry>(sceneGeometry.Length);
                geometryBuffer.CopyFrom(accelerator.DefaultStream, sceneGeometry, 0, 0, sceneGeometry.Length);
                using var lightBuffer = accelerator.Allocate<Light>(sceneLights.Length);
                lightBuffer.CopyFrom(accelerator.DefaultStream, sceneLights, 0, 0, sceneLights.Length);
                gpuBuffer.CopyFrom(accelerator.DefaultStream, colorBuffer, 0, Index2.Zero, RenderWidth * RenderHeight);
                accelerator.DefaultStream.Synchronize();

                scene.Camera = new Camera(scene.Camera.Position, scene.Camera.LookAt, scene.Camera.FocalLength, RenderWidth, RenderHeight);

                while (rendering)
                {
                    myKernel(gpuBuffer.Extent, gpuBuffer.View, scene, geometryBuffer.View, lightBuffer.View, new Random().Next());
                    accelerator.Synchronize();

                    passes += 1;
                    Dispatcher?.InvokeAsync(() => { statusLabel.Content = $"Passes: {passes}    Elapsed: {(DateTime.Now - renderStartTime).ToString(@"hh\:mm\:ss")}    Passes/s: {passes / (float)(DateTime.Now - renderStartTime).TotalSeconds}"; });

                    if ((passes - 1) % 20 == 0)
                    {
                        gpuBuffer.CopyTo(colorBuffer, Index2.Zero, 0, new Index2(RenderWidth, RenderHeight));
                        Dispatcher?.InvokeAsync(UpdateBitmap);
                    }
                } 
                
                gpuBuffer.CopyTo(colorBuffer, Index2.Zero, 0, new Index2(RenderWidth, RenderHeight));
                Dispatcher?.InvokeAsync(UpdateBitmap);
            }

            
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "PNG File|*.png"
            };
            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                using FileStream stream = File.Create(dialog.FileName);
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)outputImage.Source));
                encoder.Save(stream);
            }
        }

        private void DenoiseButton_Click(object sender, RoutedEventArgs e)
        {
            unsafe
            {
                denoiseProgressBar.Visibility = Visibility.Visible;

                IProgress<double> progress = new Progress<double>(d => { denoiseProgressBar.Value = d; });
                Task.Run(() =>
                {
                    IntPtr device = Denoiser.CreateDevice(OIDNDeviceType.OIDN_DEVICE_TYPE_DEFAULT);
                    Denoiser.Commit(device);

                    IntPtr filter = Denoiser.CreateFilter(device, "RT");
                    fixed (void* colBuffer = colorBuffer)
                    {
                        IntPtr bufferPtr = new IntPtr(colBuffer);
                        Denoiser.SetFilterImage(filter, "color", bufferPtr, OIDNFormat.OIDN_FORMAT_FLOAT3, (ulong)RenderWidth, (ulong)RenderHeight, 0, 0, 0);
                        Denoiser.SetFilterImage(filter, "output", bufferPtr, OIDNFormat.OIDN_FORMAT_FLOAT3, (ulong)RenderWidth, (ulong)RenderHeight, 0, 0, 0);
                        Denoiser.SetFilterBoolean(filter, "hdr", true);
                        Denoiser.SetFilterProgressMonitorFunction(filter, (ptr, d) =>
                        {
                            progress.Report(d);
                            return true;
                        }, IntPtr.Zero);
                        Denoiser.CommitFilter(filter);
                        Denoiser.ExecuteFilter(filter);

                        Denoiser.ReleaseFilter(filter);
                        Denoiser.ReleaseDevice(device);
                    }

                    Dispatcher?.Invoke(() =>
                    {
                        UpdateBitmap();
                        denoiseProgressBar.Visibility = Visibility.Collapsed;
                    });
                });
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SetResolution();
        }

        private void OpenSceneButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog {Filter = "Json Scene|*.json", InitialDirectory = Path.Combine(Environment.CurrentDirectory, "Scenes")};
            bool? result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                LoadScene(dialog.FileName);
            }
        }

        private void LoadScene(string filename)
        {
            JsonScene jsonScene = JsonScene.FromFile(filename);
            sceneNameLabel.Content = $"Scene: {jsonScene.Name}";

            editorViewport.LoadScene(jsonScene);

            Dictionary<string, Material> materials = jsonScene.Materials.ToDictionary(material => material.ID, material =>
            {
                Material mat = new Material().Create();
                if (material.Diffuse != null)
                    mat.SetDiffuse(material.Diffuse.Color, material.Diffuse.Albedo);
                if (material.Emission != null)
                    mat.SetEmission(material.Emission.Color, material.Emission.Brightness);
                if (material.Reflection != null)
                    mat.SetReflection(material.Reflection.Color, material.Reflection.Reflectivity, material.Reflection.IOR);
                return mat;
            });
            sceneGeometry = jsonScene.Geometry.Select(j =>
            {
                switch (j.Type)
                {
                    case "sphere": return Geometry.CreateSphere(j.Position, j.Radius, materials[j.Material]);
                    case "disc": return Geometry.CreateDisc(j.Position, j.Normal, j.Radius, materials[j.Material]);
                    case "plane": return Geometry.CreatePlane(j.Position, j.Normal, materials[j.Material]);
                    case "cylinder": return Geometry.CreateCylinder(j.Position, j.Normal, j.Radius, j.Height, materials[j.Material]);
                    default: return new Geometry();
                }
            }).ToArray();
            if (jsonScene.Lights.Length > 0)
            {
                sceneLights = jsonScene.Lights.Select(j =>
                {
                    switch (j.Type)
                    {
                        case "disc": return Light.CreateDisc(j.Position, j.Normal, j.Radius, j.Color, j.Brightness);
                        default: return new Light();
                    }
                }).ToArray();
            }
            else
            {
                sceneLights = new Light[] { new Light() };
            }
            
            scene = new Scene()
            {
                Camera = new Camera(jsonScene.Camera.Position, jsonScene.Camera.LookAt, jsonScene.Camera.FocalLength, RenderWidth, RenderHeight),
                SkylightColor = jsonScene.Skylight.Color,
                SkylightBrightness = jsonScene.Skylight.Brightness
            };
            SetResolution();
        }

        private void SetEditorViewportMode()
        {
            currentViewportMode = ViewportMode.Editor;
            openTkControl.Visibility = Visibility.Visible;
            outputImage.Visibility = Visibility.Hidden;
            toggleViewportModeButton.Content = "Render View";
        }

        private void SetRenderingViewportMode()
        {
            currentViewportMode = ViewportMode.Rendering;
            openTkControl.Visibility = Visibility.Hidden;
            outputImage.Visibility = Visibility.Visible;
            toggleViewportModeButton.Content = "Editor View";
        }

        private enum ViewportMode
        {
            Rendering,
            Editor
        }

        private void ToggleViewportModeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (currentViewportMode == ViewportMode.Editor)
                SetRenderingViewportMode();
            else
                SetEditorViewportMode();
        }
    }
}
