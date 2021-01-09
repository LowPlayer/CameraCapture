using CSharpDirectShow;
using System;

namespace Console_DirectShow
{
    class Program
    {
        static void Main(String[] args)
        {
            //var ret = DirectShow.GetVideoInputDevices(out VideoInputDsDevice[] videoInputDevices);

            //if (ret == 0)
            //{
            //    Console.WriteLine("视频输入设备：");

            //    foreach (var videoInputDevice in videoInputDevices)
            //    {
            //        Console.WriteLine($"{videoInputDevice.FriendlyName}\t{videoInputDevice.MonikerName}");

            //        if (videoInputDevice.Params.Length > 0)
            //        {
            //            Console.WriteLine("像素宽度\t像素高度\t1秒平均帧数");

            //            foreach (var param in videoInputDevice.Params)
            //            {
            //                Console.WriteLine($"{param.FrameWidth}\t{param.FrameHeight}\t{param.AverageFrameRate}");
            //            }
            //        }

            //        Console.WriteLine();
            //    }
            //}

            var ret = DirectShow.GetAudioInputDevices(out AudioInputDsDevice[] audioInputDevices);

            if (ret == 0)
            {
                Console.WriteLine("音频输入设备：");

                foreach (var audioInputDevice in audioInputDevices)
                {
                    Console.WriteLine($"{audioInputDevice.FriendlyName}\t{audioInputDevice.MonikerName}");

                    if (audioInputDevice.Params.Length > 0)
                    {
                        Console.WriteLine("音频格式\t通道数\t采样速率\t块对齐\t位数");

                        foreach (var param in audioInputDevice.Params)
                        {
                            Console.WriteLine($"{param.Format}\t{param.Channels}\t{param.SampleRate}\t{param.BlockAlign}\t{param.BitsPerSample}");
                        }
                    }

                    Console.WriteLine();
                }
            }

            Console.WriteLine("按任意键退出");
            Console.ReadKey();
        }
    }
}
