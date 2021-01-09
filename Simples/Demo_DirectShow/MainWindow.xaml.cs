using CSharpDirectShow;
using Demo_DirectShow.Model;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Demo_DirectShow
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (DirectShow.ComInit())
            {
                var videoDevices = EnumerateVideoDevices();
                this.cb_devices.ItemsSource = videoDevices;

                this.Closed += delegate { DirectShow.ComUinit(); };
            }
        }

        private IEnumerable<VideoDevice> EnumerateVideoDevices()
        {
            var ret = DirectShow.GetVideoInputDevices(out VideoInputDsDevice[] devices);

            if (ret == 0)
            {
                foreach (var device in devices)
                {
                    yield return new VideoDevice
                    {
                        FriendlyName = device.FriendlyName,
                        MonikerName = device.MonikerName,
                        VideoCapabilities = device.Params.Select(q => new VideoCapabilities
                        {
                            FrameWidth = q.FrameWidth,
                            FrameHeight = q.FrameHeight,
                            AverageFrameRate = q.AverageFrameRate
                        })
                    };
                }
            }
        }

        private void btn_start_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_take_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_record_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_rect_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
