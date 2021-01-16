using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RealStopWatch = System.Diagnostics.Stopwatch;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace Stopwatch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RealStopWatch sw;
        private DispatcherTimer timer;
        private IntPtr _windowHandle;
        private HwndSource _source;
        private Storyboard startStoryboard;
        private Storyboard stopStoryboard;

        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint KEY_Q = 0x51;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifier, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public MainWindow()
        {
            InitializeComponent();

            sw = new RealStopWatch();
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(dispatcherTimer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
        }

        private void start()
        {
            sw.Start();
            timer.Start();
            this.Title = "Stopwatch (Running)";
            lblTime.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("white"));

            winBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Green"));
            startStoryboard.Begin();
        }

        private void stop()
        {
            sw.Stop();
            timer.Stop();
            this.Title = "Stopwatch (Stopped)";
            lblTime.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("gray"));

            winBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Red"));
            stopStoryboard.Begin();
        }

        private void StartStop()
        {
            if (sw.IsRunning)
            {
                stop();
            } else
            {
                start();
            }
        }

        private void reset()
        {
            sw.Reset();
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                StartStop();
            }
            else if (e.Key == Key.Escape)
            {
                MessageBoxResult res = MessageBox.Show(
                    this,
                    "Reset the stopwatch?", 
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (res == MessageBoxResult.Yes)
                {
                    stop();
                    reset();
                    redrawTime();
                }
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            redrawTime();
        }

        private void redrawTime()
        {
            TimeSpan ts = sw.Elapsed;
            lblTime.Content = String.Format("{0:00}:{1:00}:{2:00}",
                ts.Hours, ts.Minutes, ts.Seconds);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            bool success = RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, KEY_Q);

            if (!success)
            {
                MessageBox.Show(
                    "Could not bind Ctrl+Shift+Q; binding may already be in use.",
                    "Unable to bind key.",
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }

            startStoryboard = (Storyboard) this.FindResource("StartStoryboard");
            stopStoryboard = (Storyboard) this.FindResource("StopStoryboard");

            if ((bool) App.Current.Properties["StartAtStart"])
            {
                start();
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == KEY_Q)
                            {
                                //MessageBox.Show("I have received the keypress");
                                StartStop();
                            }
                            handled = true;
                            break;
                    }
                    break;
            }

            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            base.OnClosed(e);
        }
    }
}
