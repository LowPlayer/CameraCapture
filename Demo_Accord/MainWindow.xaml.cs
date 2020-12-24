using Demo_Accord.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Demo_Accord
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private VideoCapture videoCapture;

        public MainWindow()
        {
            InitializeComponent();

            var videoDevices = VideoCapture.EnumerateVideoDevices();
            this.cb_devices.ItemsSource = videoDevices;

            Closed += delegate { videoCapture?.Dispose(); };
        }

        private void btn_start_Click(object sender, RoutedEventArgs e)
        {
            if ((String)btn_start.Content == "开始采集")
            {
                if (cb_devices.SelectedItem == null)
                {
                    MessageBox.Show("请选择视频设备");
                    return;
                }

                if (cb_capabilities.SelectedItem == null)
                {
                    MessageBox.Show("请选择采集参数");
                    return;
                }

                var device = (VideoDevice)cb_devices.SelectedItem;
                var capabilities = (VideoCapabilities)cb_capabilities.SelectedItem;

                videoCapture = new VideoCapture(device, capabilities);
                videoCapture.ImageSourceChanged = n => { img.Source = n; };

                if (!videoCapture.Start(out String errMsg))
                {
                    MessageBox.Show(errMsg);
                    return;
                }

                btn_start.Content = "停止采集";
                btn_take.IsEnabled = btn_record.IsEnabled = btn_rect.IsEnabled = true;
            }
            else
            {
                videoCapture.Dispose();
                videoCapture = null;

                btn_start.Content = "开始采集";
                btn_take.IsEnabled = btn_record.IsEnabled = btn_rect.IsEnabled = false;
            }
        }

        private void btn_rect_Click(object sender, RoutedEventArgs e)
        {
            var input = input_rect.Text;

            if (String.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("请输入用\",\"分割的四位小数");
                return;
            }

            var numStrs = input.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (numStrs.Length != 4)
            {
                MessageBox.Show("请输入用\",\"分割的四位小数");
                return;
            }

            try
            {
                var x = Double.Parse(numStrs[0]);
                var y = Double.Parse(numStrs[1]);
                var w = Double.Parse(numStrs[2]);
                var h = Double.Parse(numStrs[3]);

                if (!videoCapture.SetRenderRect(new Rect(x, y, w, h), out String errMsg))
                    MessageBox.Show(errMsg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("解析出错：" + ex.GetBaseException().Message);
            }
        }

        private void btn_take_Click(object sender, RoutedEventArgs e)
        {
            var photoFile = Path.Combine(Environment.CurrentDirectory, "photos", DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".jpg");

            if (videoCapture.TakePhoto(photoFile, out String errMsg))
                Process.Start(photoFile);
            else
                MessageBox.Show(errMsg);
        }

        private void btn_record_Click(object sender, RoutedEventArgs e)
        {
            if ((String)btn_record.Content == "开始录像")
            {
                var videoFile = Path.Combine(Environment.CurrentDirectory, "videos", DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".mp4");

                if (videoCapture.BeginRecord(videoFile, out String errMsg))
                    btn_record.Content = "停止录像";
                else
                    MessageBox.Show(errMsg);
            }
            else
            {
                if (videoCapture.EndRecord(out String videoFile, out String errMsg))
                {
                    btn_record.Content = "开始录像";
                    Process.Start(videoFile);
                }
                else
                    MessageBox.Show(errMsg);
            }
        }
    }
}
