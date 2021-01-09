using System;
using System.Collections.Generic;

namespace Demo_Accord.Model
{
    /// <summary>
    /// 视频设备
    /// </summary>
    public sealed class VideoDevice
    {
        /// <summary>
        /// 设备友好名称
        /// </summary>
        public String FriendlyName { get; set; }

        /// <summary>
        /// 设备Monitor名称（标识唯一）
        /// </summary>
        public String MonikerName { get; set; }

        /// <summary>
        /// 设备的采集参数
        /// </summary>
        public IEnumerable<VideoCapabilities> VideoCapabilities { get; set; }
    }
}
