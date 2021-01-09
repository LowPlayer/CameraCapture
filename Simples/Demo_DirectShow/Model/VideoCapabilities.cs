using System;

namespace Demo_DirectShow.Model
{
    /// <summary>
    /// 视频设备的采集参数
    /// </summary>
    public sealed class VideoCapabilities
    {
        /// <summary>
        /// 视频设备支持的帧宽
        /// </summary>
        public Int32 FrameWidth { get; set; }

        /// <summary>
        /// 视频设备支持的帧高
        /// </summary>
        public Int32 FrameHeight { get; set; }

        /// <summary>
        /// 对应帧大小的视频设备的平均帧速率
        /// </summary>
        public Int32 AverageFrameRate { get; set; }

        public override string ToString()
        {
            return $"{FrameWidth}x{FrameHeight}, {AverageFrameRate}fps";
        }
    }
}
