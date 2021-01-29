using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Grafika4a
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int width = 600;
        int height = 480;

        Device mainDevice;
        Device orthoDevice1;
        Device orthoDevice2;
        Device orthoDevice3;
        Scene scene;

        Camera mainCamera;
        Camera orthoCamera1;
        Camera orthoCamera2;
        Camera orthoCamera3;

        DateTime lastTime;
        int framesRendered;
        int fps;

        public MainWindow()
        {
            InitializeComponent();
            
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WriteableBitmap mainBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);//PixelFormats.Bgra32
            WriteableBitmap orthoBitmap1 = new WriteableBitmap(600, 480, 96, 96, PixelFormats.Bgr32, null);
            WriteableBitmap orthoBitmap2 = new WriteableBitmap(600, 480, 96, 96, PixelFormats.Bgr32, null);
            WriteableBitmap orthoBitmap3 = new WriteableBitmap(600, 480, 96, 96, PixelFormats.Bgr32, null);

            mainCamera = new Camera(new Vector3(0, 0, -10.0f), Vector3.Zero);
            orthoCamera1 = new Camera(new Vector3(0, 0, -10), Vector3.Zero);          
            orthoCamera3 = new Camera(new Vector3(10, 0, 0), Vector3.Zero);
            orthoCamera2 = new Camera(new Vector3(0, 10, 0), Vector3.Zero);

            //Mesh[] meshes1 = await Device.LoadJSONFileAsync(@"D:\DATA\Programming\Grafika\monkey.babylon");
            //Mesh[] meshes2 = await Device.LoadJSONFileAsync(@"D:\DATA\Programming\Grafika\Sphere.babylon");
            //Mesh[] meshes1 = await Device.LoadJSONFileAsync(@"D:\DATA\Programming\Grafika\FullScene.babylon");
            Mesh[] meshes1 = await Device.LoadJSONFileAsync(@"D:\DATA\Programming\Grafika\texturedScene.babylon");

            //meshes[0].Position = new Vector3(0, 0, 10f);
            meshes1[0].material = new Material(new Color4(1f, 1f, 0, 1), new Color4(1f, 1f, 1f, 1), new Color4(1f, 1f, 1f, 1));
            meshes1[1].material = new Material(new Color4(0.6f, 0f, 1f, 1), new Color4(1f, 1f, 1f, 1), new Color4(1f, 1f, 1f, 1));
            //meshes2[0].material = new Material(new Color4(1f, 1f, 0, 1), new Color4(1f, 1f, 1f, 1), new Color4(1f, 1f, 1f, 1));
            meshes1[0].Rotation = new Vector3(meshes1[0].Rotation.X + 0.01f, meshes1[0].Rotation.Y + 0.01f, meshes1[0].Rotation.Z);
            meshes1[1].Rotation = new Vector3(meshes1[1].Rotation.X + 0.01f, meshes1[1].Rotation.Y + 0.01f, meshes1[1].Rotation.Z);

            orthoCamera1.Position = new Vector3(orthoCamera1.Position.X + 0.005f, orthoCamera1.Position.Y + 0.005f, orthoCamera1.Position.Z + 0.005f);
            orthoCamera2.Position = new Vector3(orthoCamera2.Position.X + 0.005f, orthoCamera2.Position.Y + 0.005f, orthoCamera2.Position.Z + 0.005f);
            orthoCamera3.Position = new Vector3(orthoCamera3.Position.X + 0.005f, orthoCamera3.Position.Y + 0.005f, orthoCamera3.Position.Z + 0.005f);

            scene = new Scene(mainCamera, new Color4(.1f, .1f, .1f, 0));
            foreach (var mesh in meshes1)
                scene.meshes.Add(mesh);


            scene.pointLights.Add(new PointLight(new Vector3(-10, 10, -10f), new Color4(1, 1, 1, 1), new Color4(0.1f, 0.1f, 0.1f, 1), new Color4(1, 1, 1, 1)));
            scene.pointLights.Add(new PointLight(new Vector3(10, 10, 10), new Color4(1, 1, 1, 1), new Color4(0.1f, 0.1f, 0.1f, 1), new Color4(1, 1, 1, 1)));

            mainDevice = new Device(mainBitmap, false, scene, mainCamera);
            RenderImage.Source = mainBitmap;
            orthoDevice1 = new Device(orthoBitmap1, true, scene, orthoCamera1);
            Ortho1.Source = orthoBitmap1;
            orthoDevice2 = new Device(orthoBitmap2, true, scene, orthoCamera2);
            Ortho2.Source = orthoBitmap2;
            orthoDevice3 = new Device(orthoBitmap3, true, scene, orthoCamera3);
            Ortho3.Source = orthoBitmap3;
                
            CompositionTarget.Rendering += CompositionTarget_Rendering;// Registering to the XAML rendering loop 
            FOVSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(OnFOVSliderChanged);
            RenderModeComboBox.SelectionChanged += new SelectionChangedEventHandler(ComboBox_SelectionChanged);
        }

        // Rendering loop handler
        bool everySecond = true;
        void CompositionTarget_Rendering(object sender, object e)
        {
            framesRendered++;
            if ((DateTime.Now - lastTime).TotalSeconds >= 1)
            {
                fps = framesRendered;
                framesRendered = 0;
                lastTime = DateTime.Now;
            }
            FPSTextBlock.Text = string.Format("{0:0.00} fps", fps);

            mainDevice.Clear(0, 0, 0, 255);
            orthoDevice1.Clear(0, 0, 0, 255);
            orthoDevice2.Clear(0, 0, 0, 255);
            orthoDevice3.Clear(0, 0, 0, 255);

            foreach (var mesh in scene.meshes)
            {
                //mesh.Rotation = new Vector3(mesh.Rotation.X + 0.01f, mesh.Rotation.Y + 0.01f, mesh.Rotation.Z);
                //mesh.Position = new Vector3(mesh.Position.X + 0.0001f, mesh.Position.Y + 0.0001f, mesh.Position.Z);
            }    
            if(everySecond)
            {
                Vector3 vec = mainCamera.Target - mainCamera.Position;
                vec.Normalize();
                if(vec != new Vector3(0.0f, 1.0f, 0.0f))
                {
                    mainDevice.Render(new Vector3(0.0f, 1.0f, 0.0f));
                    mainDevice.Present();
                }              
                orthoDevice1.Render(Vector3.UnitY);
                orthoDevice1.Present();
                orthoDevice2.Render(Vector3.UnitZ);
                orthoDevice2.Present();
                orthoDevice3.Render(Vector3.UnitY);
                orthoDevice3.Present();
            }
            everySecond = !everySecond;
        }

 

        private void Ortho3MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point p = e.GetPosition(Ortho3);
                p.X = (float)((300 - p.X) / 20f);
                p.Y = (float)((240 - p.Y) / 20f);
                if (Vector3.DistanceSquared(mainCamera.Position, new Vector3(mainCamera.Position.X, (float)p.Y, -(float)p.X)) < 1.5f * 1.5f)
                    mainCamera.Position = new Vector3(mainCamera.Position.X, (float)p.Y, -(float)p.X);
                else if (Vector3.DistanceSquared(mainCamera.Target, new Vector3(mainCamera.Target.X, (float)p.Y, -(float)p.X)) < 1.5f * 1.5f)
                    mainCamera.Target = new Vector3(mainCamera.Target.X, (float)p.Y, -(float)p.X);
            }           
        }

        private void Ortho2MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point p = e.GetPosition(Ortho2);
                p.X = (float)((300 - p.X) / 20f);
                p.Y = (float)((240 - p.Y) / 20f);
                if (Vector3.DistanceSquared(mainCamera.Position, new Vector3(-(float)p.X, mainCamera.Position.Y, (float)p.Y)) < 1.5f * 1.5f)
                    mainCamera.Position = new Vector3(-(float)p.X, mainCamera.Position.Y, (float)p.Y);
                else if (Vector3.DistanceSquared(mainCamera.Target, new Vector3(-(float)p.X, mainCamera.Target.Y, (float)p.Y)) < 1.5f * 1.5f)
                    mainCamera.Target = new Vector3(-(float)p.X, mainCamera.Target.Y, (float)p.Y);
            }

        }

        private void Ortho1MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point p = e.GetPosition(Ortho1);
                p.X = (float)((300 - p.X) / 20f);
                p.Y = (float)((240 - p.Y) / 20f);
                if (Vector3.DistanceSquared(mainCamera.Position, new Vector3(-(float)p.X, (float)p.Y, mainCamera.Position.Z)) < 1.5f * 1.5f)
                    mainCamera.Position = new Vector3(-(float)p.X, (float)p.Y, mainCamera.Position.Z);
                else if (Vector3.DistanceSquared(scene.mainCamera.Target, new Vector3(-(float)p.X, (float)p.Y, scene.mainCamera.Target.Z)) < 1.5f * 1.5f)
                    scene.mainCamera.Target = new Vector3(-(float)p.X, (float)p.Y, scene.mainCamera.Target.Z);                
            }
        }

        private void OnFOVSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float val = (float)((Math.PI / 180) * e.NewValue);
            mainDevice.fov = val;
            mainCamera.fov = val;
        }

        private void BackfaceCullingToggleClick(object sender, RoutedEventArgs e)
        {
            mainDevice.reverseBackFace = (bool)CullingToggle.IsChecked;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RenderMode renderMode = (RenderMode)RenderModeComboBox.SelectedIndex;
            mainDevice.renderMode = renderMode;
            orthoDevice1.renderMode = renderMode;
            orthoDevice2.renderMode = renderMode;
            orthoDevice3.renderMode = renderMode;
        }
    }
}
