using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
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
        public const int WIDTH = 3840;
        public const int HEIGHT = 2160;

        private Context context;
        private Accelerator accelerator;
        private Vector3[] colorBuffer;
        private int passes = 0;
        private int updateTimer = 5;
        private DateTime renderStartTime;
        private Task renderTask;
        private bool rendering = false;

        public MainWindow()
        {
            InitializeComponent();
            //outputImage.Width = WIDTH;
            //outputImage.Height = HEIGHT;
            RenderOptions.SetBitmapScalingMode(outputImage, BitmapScalingMode.HighQuality);

            //GPUlabel.Content = $"GPU: {accelerator.Name}";
            colorBuffer = new Vector3[WIDTH * HEIGHT];
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (startRenderButton.Content.ToString() == "Start Rendering")
            {
                denoiseButton.IsEnabled = false;
                renderStartTime = DateTime.Now;
                int device = deviceComboBox.SelectedIndex;
                renderTask = Task.Run(() =>
                {
                    context = new Context();
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
                    while (rendering)
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
            var bitmap = new WriteableBitmap(WIDTH, HEIGHT, 1.0, 1.0, PixelFormats.Rgb24, null);
            outputImage.Source = bitmap;

            bitmap.Lock();
            IntPtr buffer = bitmap.BackBuffer;
            unsafe
            {
                for (int y = 0; y < HEIGHT; y++)
                {
                    for (int x = 0; x < WIDTH; x++)
                    {
                        Vector3 col = colorBuffer[x + WIDTH * y] / passes;
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
            Vector3[] output = new Vector3[WIDTH * HEIGHT];

            var myKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, ArrayView2D<Vector3>, Scene, ArrayView<Models.Geometry>, ArrayView<Light>, int>(Raytracer.Kernel);

            using (var gpuBuffer = accelerator.Allocate<Vector3>(new Index2(WIDTH, HEIGHT)))
            {
                System.Random random = new System.Random();
                Material lightMaterial = new Material().Create().SetEmission(new Vector3(1f, 1f, 1f), 40f);
                Material whiteMaterial = new Material().Create().SetDiffuse(new Vector3(1f, 1f, 1f), 1f);
                Material redMaterial = new Material().Create().SetDiffuse(new Vector3(1f, 0.2f, 0.2f), 1f);
                Material greenMaterial = new Material().Create().SetDiffuse(new Vector3(0.2f, 1f, 0.2f), 1f);
                Models.Geometry[] sceneGeometry = {
                    Geometry.CreateSphere(new Vector3(0f, -20f, -10f), 10f, whiteMaterial),
                    //Geometry.CreateCylinder(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.2f, -0.2f), 2.5f, 100f, whiteMaterial), 
                    Geometry.CreatePlane(new Vector3(0f, -30f, 0f), new Vector3(0f, 1f, 0f), whiteMaterial),
                    Geometry.CreatePlane(new Vector3(0f, 30f, 0f), new Vector3(0f, -1f, 0f), whiteMaterial),
                    Geometry.CreatePlane(new Vector3(-20f, 0f, 0f), new Vector3(1f, 0f, 0f), redMaterial),
                    Geometry.CreatePlane(new Vector3(20f, 0f, 0f), new Vector3(-1f, 0f, 0f), greenMaterial),
                    Geometry.CreatePlane(new Vector3(0f, 0f, 20f), new Vector3(0f, 0f, -1f), whiteMaterial),
                    //Geometry.CreateDisc(new Vector3(0f, 29.9f, 0f), new Vector3(0f, -1f, 0f), 7.5f, lightMaterial),
                };
                Light[] sceneLights =
                {
                    Light.CreateDisc(new Vector3(0f, 29.9f, 0f), new Vector3(0f, -1f, 0f), 7.5f, Vector3.One, 30f),
                };

                using var geometryBuffer = accelerator.Allocate<Geometry>(sceneGeometry.Length);
                geometryBuffer.CopyFrom(accelerator.DefaultStream, sceneGeometry, 0, 0, sceneGeometry.Length);
                using var lightBuffer = accelerator.Allocate<Light>(sceneLights.Length);
                lightBuffer.CopyFrom(accelerator.DefaultStream, sceneLights, 0, 0, sceneLights.Length);
                gpuBuffer.CopyFrom(accelerator.DefaultStream, colorBuffer, 0, Index2.Zero, WIDTH * HEIGHT);
                accelerator.DefaultStream.Synchronize();

                Scene scene = new Scene
                {
                    Camera = new Camera(new Vector3(0f, 0f, -100f), Vector3.Zero, 36f, WIDTH, HEIGHT)
                };

                myKernel(gpuBuffer.Extent, gpuBuffer.View, scene, geometryBuffer.View, lightBuffer.View, new System.Random().Next());
                accelerator.Synchronize();

                gpuBuffer.CopyTo(colorBuffer, Index2.Zero, 0, new Index2(WIDTH, HEIGHT));
                passes += 1;

                Dispatcher?.InvokeAsync(() => { statusLabel.Content = $"Passes: {passes}    Elapsed: {(DateTime.Now - renderStartTime).ToString(@"hh\:mm\:ss")}    Passes/s: {passes / (float)(DateTime.Now - renderStartTime).TotalSeconds}"; });
            }

            if ((passes - 1) % 5 == 0)
                Dispatcher?.InvokeAsync(UpdateBitmap);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "PNG File|*.png";
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
                    IntPtr device = Denoiser.CreateDevice(OIDNDeviceType.OIDN_DEVICE_TYPE_CPU);
                    Denoiser.Commit(device);

                    IntPtr filter = Denoiser.CreateFilter(device, "RT");
                    fixed (void* colBuffer = colorBuffer)
                    {
                        IntPtr bufferPtr = new IntPtr(colBuffer);
                        Denoiser.SetFilterImage(filter, "color", bufferPtr, OIDNFormat.OIDN_FORMAT_FLOAT3, WIDTH, HEIGHT, 0, 0, 0);
                        Denoiser.SetFilterImage(filter, "output", bufferPtr, OIDNFormat.OIDN_FORMAT_FLOAT3, WIDTH, HEIGHT, 0, 0, 0);
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
    }
}
