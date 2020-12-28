using Accord.Video;
using Accord.Video.DirectShow;
using Accord.Video.FFMPEG;
using Demo_Accord.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VideoCapabilities = Demo_Accord.Model.VideoCapabilities;

namespace Demo_Accord
{
    /// <summary>
    /// 视频采集接口
    /// </summary>
    public interface IVideoCapture : IDisposable
    {
        /// <summary>
        /// 开始采集
        /// </summary>
        Boolean Start(out String errMsg);
        /// <summary>
        /// 停止采集
        /// </summary>
        Boolean Stop(out String errMsg);

        /// <summary>
        /// 拍照
        /// </summary>
        /// <param name="photoFile">照片存储路径</param>
        Boolean TakePhoto(String photoFile, out String errMsg);

        /// <summary>
        /// 开始录像
        /// </summary>
        /// <param name="videoFile">录像文件保存路径</param>
        Boolean BeginRecord(String videoFile, out String errMsg);

        /// <summary>
        /// 停止录像
        /// </summary>
        Boolean EndRecord(out String videoFile, out String errMsg);

        /// <summary>
        /// 设置相对渲染区域
        /// 相对原始图像，宽高最大为1
        /// </summary>
        Boolean SetRenderRect(Rect rect, out String errMsg);

        /// <summary>
        /// 设备名称
        /// </summary>
        String Name { get; }

        /// <summary>
        /// 当前图像大小，单位px
        /// </summary>
        Size ImageSize { get; }

        /// <summary>
        /// 获取相对渲染区域
        /// </summary>
        Rect RenderRect { get; }

        /// <summary>
        /// 是否开始采集
        /// </summary>
        Boolean IsStarted { get; }

        /// <summary>
        /// 是否正在录像
        /// </summary>
        Boolean IsRecording { get; }

        /// <summary>
        /// 切换图像源回调事件
        /// </summary>
        Action<ImageSource> ImageSourceChanged { get; set; }
    }

    /// <summary>
    /// 视频采集实现类
    /// </summary>
    public sealed class VideoCapture : IVideoCapture
    {
        /// <summary>
        /// 枚举视频设备
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<VideoDevice> EnumerateVideoDevices()
        {
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            foreach (var videoDevice in videoDevices)
            {
                var deviceName = videoDevice.Name;
                var videoCaptureDevice = new VideoCaptureDevice(videoDevice.MonikerString);

                yield return new VideoDevice
                {
                    FriendlyName = videoDevice.Name,
                    MonikerName = videoDevice.MonikerString,
                    VideoCapabilities = videoCaptureDevice.VideoCapabilities.Select(q => new VideoCapabilities
                    {
                        FrameWidth = q.FrameSize.Width,
                        FrameHeight = q.FrameSize.Height,
                        AverageFrameRate = q.AverageFrameRate
                    })
                };
            }
        }

        /// <summary>
        /// 复制指针数据到另一个指针
        /// </summary>
        /// <param name="dest">目标指针</param>
        /// <param name="source">源指针</param>
        /// <param name="length">字符长度</param>
        [DllImport("ntdll.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Memcpy(IntPtr dest, IntPtr source, Int32 length);

        public VideoCapture(VideoDevice device, VideoCapabilities videoCapabilities)
        {
            this.device = device;
            this.videoCapabilities = videoCapabilities;

            Name = device.FriendlyName;
            videoCaptureDevice = new VideoCaptureDevice(device.MonikerName);

            var capabilities = videoCaptureDevice.VideoCapabilities.FirstOrDefault(q => q.FrameSize.Width == videoCapabilities.FrameWidth
            && q.FrameSize.Height == videoCapabilities.FrameHeight && q.AverageFrameRate == videoCapabilities.AverageFrameRate);

            if (capabilities != null)
                videoCaptureDevice.VideoResolution = capabilities;

            videoCaptureDevice.NewFrame += OnNewFrame;

            relativeRect = bmp_relative_rect = new Rect(new Size(1, 1));

            bmp_absolute_rect.Width = frameWidth = videoCapabilities.FrameWidth;
            bmp_absolute_rect.Height = frameHeight = videoCapabilities.FrameHeight;
        }

        #region Public Methods

        public Boolean Start(out String errMsg)
        {
            errMsg = null;

            if (!videoCaptureDevice.IsRunning)
                videoCaptureDevice.Start();

            return true;
        }

        public Boolean Stop(out String errMsg)
        {
            errMsg = null;

            if (videoCaptureDevice.IsRunning)
                videoCaptureDevice.Stop();

            return true;
        }

        public Boolean TakePhoto(String photoFile, out String errMsg)
        {
            errMsg = null;

            if (writeableBitmap == null)
            {
                SpinWait.SpinUntil(() => { return writeableBitmap != null || !IsStarted; });

                if (writeableBitmap == null || IsStarted)
                    return false;
            }

            try
            {
                var renderTargetBitmap = new RenderTargetBitmap(writeableBitmap.PixelWidth, writeableBitmap.PixelHeight, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Default);

                DrawingVisual drawingVisual = new DrawingVisual();

                using (var dc = drawingVisual.RenderOpen())
                {
                    dc.DrawImage(writeableBitmap, new Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
                }

                renderTargetBitmap.Render(drawingVisual);

                JpegBitmapEncoder bitmapEncoder = new JpegBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                var folder = Path.GetDirectoryName(photoFile);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                using (var fs = File.OpenWrite(photoFile))
                {
                    bitmapEncoder.Save(fs);
                }

                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.GetBaseException().Message;
                return false;
            }
        }

        public Boolean BeginRecord(String videoFile, out String errMsg)
        {
            errMsg = null;
            this.videoFile = null;

            if (IsRecording)
                return true;

            try
            {
                var folder = Path.GetDirectoryName(videoFile);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                videoFileWriter = new VideoFileWriter();
                videoFileWriter.Open(videoFile, bmp_absolute_rect.Width, bmp_absolute_rect.Height, videoCapabilities.AverageFrameRate, VideoCodec.MPEG4);

                if (videoFileWriter.IsOpen)
                {
                    this.perTicks = 1000 / videoCapabilities.AverageFrameRate;
                    this.videoFile = videoFile;

                    if (this.stopwatch == null)
                        this.stopwatch = new Stopwatch();
                }

                return IsRecording;
            }
            catch (Exception ex)
            {
                errMsg = ex.GetBaseException().Message;
                return false;
            }
        }

        public Boolean EndRecord(out String videoFile, out String errMsg)
        {
            errMsg = null;
            videoFile = null;

            if (!IsRecording)
                return true;

            videoFile = this.videoFile;
            this.videoFile = null;

            videoFileWriter.Close();
            videoFileWriter.Dispose();
            videoFileWriter = null;

            this.stopwatch.Reset();

            return true;
        }

        public Boolean SetRenderRect(Rect rect, out String errMsg)
        {
            errMsg = null;

            if (IsRecording)
            {
                errMsg = "录像期间不允许修改裁剪区域";
                return false;
            }

            // 验证数据合理性
            if (rect.X < 0 || rect.Y < 0 || rect.X > 1 || rect.Y > 1 || rect.X + rect.Width > 1 || rect.Y + rect.Height > 1)
            {
                errMsg = "裁剪区域超出有效范围";
                return false;
            }

            this.relativeRect = rect;

            return true;
        }

        public void Dispose()
        {
            if (IsRecording)
                EndRecord(out _, out _);

            if (IsStarted)
                Stop(out _);

            writeableBitmap = null;
            ImageSourceChanged = null;
        }

        #endregion

        #region Private Methods

        private void OnNewFrame(Object sender, NewFrameEventArgs e)
        {
            var bmp = e.Frame;

            if (writeableBitmap == null || bmp.Width != frameWidth || bmp.Height != frameHeight || relativeRect != bmp_relative_rect)
            {
                // 创建新的WriteableBitmap
                frameWidth = bmp.Width;
                frameHeight = bmp.Height;
                frameRect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
                bmp_relative_rect = relativeRect;
                bmp_absolute_rect.X = (Int32)(bmp.Width * relativeRect.X);
                bmp_absolute_rect.Y = (Int32)(bmp.Height * relativeRect.Y);
                bmp_absolute_rect.Width = (Int32)(bmp.Width * relativeRect.Width);
                bmp_absolute_rect.Height = (Int32)(bmp.Height * relativeRect.Height);

                context.Send(n =>
                {
                    writeableBitmap = new WriteableBitmap(bmp_absolute_rect.Width, bmp_absolute_rect.Height, 96, 96, PixelFormats.Bgr24, null);
                    bmp_stride = writeableBitmap.BackBufferStride;
                    bmp_length = bmp_stride * bmp_absolute_rect.Height;
                    bmp_backBuffer = writeableBitmap.BackBuffer;

                    if (IsRecording)
                    {
                        // 创建新的录像
                        if (EndRecord(out String videoFile, out _))
                            BeginRecord(videoFile, out _);
                    }
                }, null);
            }

            var bmpData = bmp.LockBits(bmp_absolute_rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            var bmpDataPtr = bmpData.Scan0;
            var bmpDataStride = bmpData.Stride;

            if (bmp_stride == bmpDataStride)
                Memcpy(bmp_backBuffer, bmpDataPtr, bmp_length);
            else
            {
                // 逐行复制
                var targetPtr = bmp_backBuffer;
                var yPtr = bmpDataPtr;  // 指向每一行的开始
                var length = Math.Min(bmp_stride, bmpDataStride);

                for (var i = 0; i < bmpData.Height; i++)
                {
                    Memcpy(targetPtr, yPtr, length);
                    yPtr += bmpDataStride;
                    targetPtr += bmp_stride;
                }
            }

            if (IsRecording)
            {
                if (!stopwatch.IsRunning)
                {
                    stopwatch.Start();
                    videoFileWriter.WriteVideoFrame(bmpData);
                }
                else
                {
                    var frameIndex = stopwatch.ElapsedMilliseconds / this.perTicks;
                    videoFileWriter.WriteVideoFrame(bmpData, (UInt32)frameIndex);
                }
            }

            bmp.UnlockBits(bmpData);
            bmp.Dispose();

            Interlocked.Exchange(ref newFrame, 1);
        }

        private void OnRender(Object sender, EventArgs e)
        {
            var curRenderingTime = ((RenderingEventArgs)e).RenderingTime;

            if (curRenderingTime == lastRenderingTime)
                return;

            lastRenderingTime = curRenderingTime;

            if (Interlocked.CompareExchange(ref newFrame, 0, 1) != 1)
                return;

            var bmp = this.writeableBitmap;
            bmp.Lock();
            bmp.AddDirtyRect(new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
            bmp.Unlock();
        }

        #endregion

        #region Properties

        public String Name { get; }

        public Size ImageSize => new Size(bmp_absolute_rect.Width, bmp_absolute_rect.Height);

        public Rect RenderRect => relativeRect;

        public Boolean IsStarted => videoCaptureDevice.IsRunning;

        public Boolean IsRecording => videoFileWriter != null && videoFileWriter.IsOpen;

        public Action<ImageSource> ImageSourceChanged { get; set; }

        #endregion

        #region Fields

        private SynchronizationContext context = SynchronizationContext.Current;    // 线程同步上下文
        private VideoDevice device;                         // 视频设备
        private VideoCapabilities videoCapabilities;        // 视频设备采集参数
        private VideoCaptureDevice videoCaptureDevice;      // 视频采集设备

        private WriteableBitmap _writeableBitmap;
        private WriteableBitmap writeableBitmap
        {
            get => this._writeableBitmap;
            set
            {
                if (this._writeableBitmap == value)
                    return;

                if (this._writeableBitmap == null)
                    CompositionTarget.Rendering += OnRender;
                else if (value == null)
                    CompositionTarget.Rendering -= OnRender;

                this._writeableBitmap = value;
                this.ImageSourceChanged?.Invoke(value);
            }
        }

        private Int32 newFrame;     // 标识是否有未渲染的帧
        private TimeSpan lastRenderingTime;     // 上一次渲染时间
        private Rect relativeRect;        // 相对渲染区域
        private Rect bmp_relative_rect;          // 正在应用的相对渲染区域
        private System.Drawing.Rectangle bmp_absolute_rect;      // 正在应用的绝对渲染区域
        private Int32 frameWidth;       // 一帧的原始宽度
        private Int32 frameHeight;      // 一帧的原始高度
        private System.Drawing.Rectangle frameRect;     // 一帧的渲染区域
        private Int32 bmp_stride;       // 一行像素占多少字节
        private Int32 bmp_length;       // 一帧图像占多少字节
        private IntPtr bmp_backBuffer;  // WriteableBitmap的后台缓冲指针
        private VideoFileWriter videoFileWriter;    // 视频写入文件
        private String videoFile;                   // 正在写入的视频文件
        private Stopwatch stopwatch;
        private Int64 perTicks;

        #endregion
    }
}
