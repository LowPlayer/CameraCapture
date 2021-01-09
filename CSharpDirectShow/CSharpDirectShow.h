#pragma once

#include <dshow.h>

using namespace System;
using namespace System::Runtime::InteropServices;

namespace CSharpDirectShow {
	/// <summary>
	/// 视频采集参数
	/// </summary>
	public ref struct VideoParams
	{
		/// <summary>
		/// 像素宽度
		/// </summary>
		Int32 FrameWidth;
		/// <summary>
		/// 像素高度
		/// </summary>
		Int32 FrameHeight;
		/// <summary>
		/// 1秒平均帧数
		/// </summary>
		Int32 AverageFrameRate;
	};
	/// <summary>
	/// 音频采集参数
	/// </summary>
	public ref struct AudioParams
	{
		/// <summary>
		/// 音频格式 0:Unknown|1:WAVE_FORMAT_PCM|2:WAVE_FORMAT_ADPCM|3:WAVE_FORMAT_IEEE_FLOAT|...
		/// </summary>
		Int32 Format;
		/// <summary>
		/// 通道数
		/// </summary>
		Int32 Channels;
		/// <summary>
		/// 采样速率
		/// </summary>
		Int32 SampleRate;
		/// <summary>
		/// 块对齐
		/// </summary>
		Int32 BlockAlign;
		/// <summary>
		/// 位数
		/// </summary>
		Int32 BitsPerSample;
	};
	/// <summary>
	/// 视频输入设备
	/// </summary>
	public ref struct VideoInputDsDevice
	{
		/// <summary>
		/// 设备友好名称
		/// </summary>
		String^ FriendlyName;
		/// <summary>
		/// 设备Monitor名称（标识唯一）
		/// </summary>
		String^ MonikerName;
		/// <summary>
		/// 设备支持的参数
		/// </summary>
		array<VideoParams^>^ Params;
	};
	/// <summary>
	/// 音频输入设备
	/// </summary>
	public ref struct AudioInputDsDevice
	{
		/// <summary>
		/// 设备友好名称
		/// </summary>
		String^ FriendlyName;
		/// <summary>
		/// 设备Monitor名称（标识唯一）
		/// </summary>
		String^ MonikerName;
		/// <summary>
		/// 设备支持的参数
		/// </summary>
		array<AudioParams^>^ Params;
	};

	public ref class DirectShow
	{
	public:
		/// <summary>
		/// 在当前线程上初始化COM库，并将并发模型标识为单线程单元（STA）
		/// </summary>
		/// <returns></returns>
		static Boolean ComInit();
		/// <summary>
		/// 关闭当前线程上的COM库，卸载该线程加载的所有DLL，释放该线程维护的所有其他资源，并强制关闭该线程上的所有RPC连接
		/// </summary>
		static void ComUinit();
		/// <summary>
		/// 获取视频输入设备
		/// </summary>
		/// <param name="devices"></param>
		/// <returns></returns>
		static Int32 GetVideoInputDevices([Out]array<VideoInputDsDevice^>^% devices);
		/// <summary>
		/// 获取音频输入设备
		/// </summary>
		/// <param name="device"></param>
		/// <returns></returns>
		static Int32 GetAudioInputDevices([Out]array<AudioInputDsDevice^>^% device);
	private:
		static HRESULT EnumerateDevices(REFGUID category, IEnumMoniker** ppEnum);
		static HRESULT EnumerateVideoInputDevices(IEnumMoniker* pEnum, [Out]array<VideoInputDsDevice^>^% devices);
		static HRESULT EnumerateVideoParams(IMoniker* pMoniker, [Out]array<VideoParams^>^% params);
		static HRESULT EnumerateAudioInputDevices(IEnumMoniker* pEnum, [Out]array<AudioInputDsDevice^>^% devices);
		static HRESULT EnumerateAudioParams(IMoniker* pMoniker, [Out]array<AudioParams^>^% params);
	};
}
