using DotInjector_CSGO_injector.Injector;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DotInjector_CSGO_injector
{
    public partial class MainWindow : Window
    {
        readonly Storyboard DropStrokeStoryboard = new Storyboard();
        readonly RotateTransform AwaitCsProcessTransform = new RotateTransform();

        Process CsProcess = null;
        bool Find = false;
        string DllPath;

        public MainWindow()
        {
            InitializeComponent();
            var doubleAnimation = new DoubleAnimation(-360, 0, new Duration(TimeSpan.FromSeconds(1.5)));
            waitCsGo.RenderTransform = AwaitCsProcessTransform;
            waitCsGo.RenderTransformOrigin = new Point(0.5, 0.5);
            doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
            AwaitCsProcessTransform.BeginAnimation(RotateTransform.AngleProperty, doubleAnimation);


            var colorAnim = new ColorAnimation(Colors.GreenYellow, new Duration(TimeSpan.FromSeconds(1.2)))
            {
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true
            };
            DropStrokeStoryboard.Children.Add(colorAnim);
            Storyboard.SetTarget(colorAnim, dropZone);
            Storyboard.SetTargetProperty(colorAnim, new PropertyPath("(Rectangle.Stroke).(SolidColorBrush.Color)"));
        }


        private void Path_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Close_Click(object sender, MouseButtonEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void Minimize_Click(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            DragTitle.Text = "Release the mouse button";
            DropStrokeStoryboard.Begin();
        }

        private void Grid_DragLeave(object sender, DragEventArgs e)
        {
            DragTitle.Text = "Drag .dll file to inject";
            DropStrokeStoryboard.Stop();
        }

        private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Dynamic link library (.dll)|*.dll"
            };
            if (dialog.ShowDialog() != true)
                return;

            DllPath = dialog.FileName;
            LoadedChangeProperty(System.IO.Path.GetFileName(DllPath));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                while(true)
                {
                    Thread.Sleep(1000);
                    CsProcess = Process.GetProcessesByName("csgo")?.FirstOrDefault();
                    if (CsProcess != null && !Find)
                    {
                        Find = true;
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            findCsGoPanel.Visibility = Visibility.Visible;
                            waitCsGoPanel.Visibility = Visibility.Collapsed;
                            findCsGoTitle.Text = $"CS:GO process found | PID:{CsProcess.Id}";
                            waitCsGo.RenderTransform = null;
                        });
                    }
                    else if(CsProcess == null && Find)
                    {
                        Find = false;
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            findCsGoPanel.Visibility = Visibility.Collapsed;
                            waitCsGoPanel.Visibility = Visibility.Visible;
                            waitCsGo.RenderTransform = AwaitCsProcessTransform;
                        });
                    }
                        
                }
            });
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            DllPath = ((string[])e.Data.GetData(DataFormats.FileDrop)).FirstOrDefault();
            if(DllPath == null || !DllPath.EndsWith(".dll"))
            {
                DllPath = null;
                DragTitle.Text = "Drag .dll file to inject";
                DropStrokeStoryboard.Stop();
                return;
            }

            LoadedChangeProperty(System.IO.Path.GetFileName(DllPath));
            DropStrokeStoryboard.Stop();

        }

        private void LoadedChangeProperty(string dllName)
        {
            LoadedDllTitle.Text = "Loaded: " + dllName;
            ActionArea.Visibility = Visibility.Visible;
            DropArea.Visibility = Visibility.Collapsed;
        }

        private void UnloadDll_Click(object sender, RoutedEventArgs e)
        {
            ErrTitle.Text = String.Empty;
            ActionArea.Visibility = Visibility.Collapsed;
            DropArea.Visibility = Visibility.Visible;
            DllPath = null;
        }

        private async void Inject_Click(object sender, RoutedEventArgs e)
        {
            ErrTitle.Text = String.Empty;
            if(!Find)
            {
                var brush = new BrushConverter().ConvertFromString("#415672") as Brush;

                for (int i = 0; i < 2; i++)
                {
                    WaitingCsGoTitle.Foreground = Brushes.PaleVioletRed;
                    await Task.Delay(300);
                    WaitingCsGoTitle.Foreground = brush;
                    await Task.Delay(300);
                }
                return;
            }

            switch(Hook.Inject(CsProcess, DllPath))
            {
                case InjectResponse.InvalidPath:
                    ErrTitle.Text = "Error! Invalid path";
                    break;
                case InjectResponse.DllNotExist:
                    ErrTitle.Text = "Error! Dll was not found";
                    break;
                case InjectResponse.InvalidProcess:
                    ErrTitle.Text = "Error! Invalid process";
                    break;
                case InjectResponse.BypassRestoreHookError:
                    ErrTitle.Text = "VAC Bypass error";
                    break;
                case InjectResponse.BypassUnhookError:
                    ErrTitle.Text = "VAC Bypass restore error";
                    break;
                case InjectResponse.Not32xDll:
                    ErrTitle.Text = "Injection error! 64x bit dll";
                    break;
                case InjectResponse.OK:
                    ErrTitle.Text = "Successfully injected!";
                    ErrTitle.Foreground = Brushes.LightGreen;
                    await Task.Delay(1000);
                    ErrTitle.Foreground = Brushes.PaleVioletRed;
                    App.Current.Shutdown();
                    break;

            }
        }
    }
}
