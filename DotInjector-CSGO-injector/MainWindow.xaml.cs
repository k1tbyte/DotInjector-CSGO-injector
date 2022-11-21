using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DotInjector_CSGO_injector
{
    public partial class MainWindow : Window
    {
        Process CsProcess = null;
        bool Find = false;
        string DllPath;

        public MainWindow()
        {
            InitializeComponent();
            var doubleAnimation = new DoubleAnimation(-360, 0, new Duration(TimeSpan.FromSeconds(1.5)));
            var rotateTransform = new RotateTransform();
            waitCsGo.RenderTransform = rotateTransform;
            waitCsGo.RenderTransformOrigin = new Point(0.5, 0.5);
            doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, doubleAnimation);
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
        }

        private void Grid_DragLeave(object sender, DragEventArgs e)
        {
            DragTitle.Text = "Drag .dll file to inject";
        }

        private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "Dynamic link library (.dll)|*.dll"
            };
            if(dialog.ShowDialog() == true)
            {
                System.Windows.Forms.MessageBox.Show(dialog.FileName);
            }
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
                        });
                    }
                    else if(CsProcess == null && Find)
                    {
                        Find = false;
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            findCsGoPanel.Visibility = Visibility.Collapsed;
                            waitCsGoPanel.Visibility = Visibility.Visible;
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
                return;
            }

            DragTitle.Text = "Loaded: " + System.IO.Path.GetFileName(DllPath);




        }
    }
}
