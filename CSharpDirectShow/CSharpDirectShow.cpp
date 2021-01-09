#include "pch.h"

#include "CSharpDirectShow.h"
#include <dvdmedia.h>
#include <mmreg.h>

#define MAX_MONIKER_NAME_LENGTH 256

using namespace CSharpDirectShow;
using namespace System::Collections::Generic;

Boolean DirectShow::ComInit()
{
	HRESULT hr = CoInitialize(NULL);
	return hr == S_OK || hr == S_FALSE;
}

void DirectShow::ComUinit()
{
	CoUninitialize();
}

HRESULT DirectShow::EnumerateDevices(REFGUID category, IEnumMoniker** ppEnum)
{
	// 创建系统设备枚举器
	ICreateDevEnum* pDevEnum;
	HRESULT hr = CoCreateInstance(CLSID_SystemDeviceEnum, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&pDevEnum));

	if (SUCCEEDED(hr))
	{
		// 为类别创建枚举数.
		hr = pDevEnum->CreateClassEnumerator(category, ppEnum, 0);

		if (hr == S_FALSE)
			hr = VFW_E_NOT_FOUND;  // 类别是空的，视为错误

		pDevEnum->Release();
	}
	return hr;
}

HRESULT DirectShow::EnumerateVideoParams(IMoniker* pMoniker, [Out]array<VideoParams^>^% params)
{
	params = nullptr;
	IBaseFilter* pFilter;
	HRESULT	hr = pMoniker->BindToObject(NULL, NULL, IID_IBaseFilter, (void**)&pFilter);

	if (FAILED(hr))
		return hr;

	IEnumPins* pinEnum;
	hr = pFilter->EnumPins(&pinEnum);

	if (FAILED(hr)) {
		pFilter->Release();
		return hr;
	}

	List<VideoParams^>^ list = gcnew List<VideoParams^>();
	IPin* pPins;
	while (pinEnum->Next(1, &pPins, NULL) == S_OK)
	{
		PIN_INFO pinInfo;
		hr = pPins->QueryPinInfo(&pinInfo);

		if (FAILED(hr) || pinInfo.dir != PINDIR_OUTPUT)
		{
			pPins->Release();
			continue;
		}

		IEnumMediaTypes* mtEnum;
		hr = pPins->EnumMediaTypes(&mtEnum);

		if (FAILED(hr))
		{
			pPins->Release();
			continue;
		}

		AM_MEDIA_TYPE* mt;
		while (mtEnum->Next(1, &mt, NULL) == S_OK)
		{
			VideoParams^ param = nullptr;
			if (mt->formattype == FORMAT_VideoInfo)
			{
				VIDEOINFOHEADER* pVih = reinterpret_cast<VIDEOINFOHEADER*>(mt->pbFormat);
				param = gcnew VideoParams();
				param->FrameWidth = pVih->bmiHeader.biWidth;
				param->FrameHeight = pVih->bmiHeader.biHeight;
				param->AverageFrameRate = pVih->AvgTimePerFrame == 0 ? 0 : 10000000 / pVih->AvgTimePerFrame;
			}
			else if (mt->formattype == FORMAT_VideoInfo2) {
				VIDEOINFOHEADER2* pVih = reinterpret_cast<VIDEOINFOHEADER2*>(mt->pbFormat);
				param = gcnew VideoParams();
				param->FrameWidth = pVih->bmiHeader.biWidth;
				param->FrameHeight = pVih->bmiHeader.biHeight;
				param->AverageFrameRate = pVih->AvgTimePerFrame == 0 ? 0 : 10000000 / pVih->AvgTimePerFrame;
			}

			if (param && param->AverageFrameRate > 1)
			{
				Boolean isExit = false;
				for each (VideoParams ^ item in list)
				{
					if (item->FrameWidth == param->FrameWidth && item->FrameHeight == param->FrameHeight && item->AverageFrameRate == param->AverageFrameRate)
					{
						isExit = true;
						break;
					}
				}
				if (!isExit)
					list->Add(param);
			}
		}
		pPins->Release();
	}

	pFilter->Release();

	params = list->ToArray();
	return S_OK;
}

HRESULT DirectShow::EnumerateVideoInputDevices(IEnumMoniker* pEnum, [Out]array<VideoInputDsDevice^>^% devices)
{
	HRESULT hr = S_FALSE;
	devices = nullptr;
	List<VideoInputDsDevice^>^ list = gcnew List<VideoInputDsDevice^>();

	IMoniker* pMoniker;
	while (pEnum->Next(1, &pMoniker, NULL) == S_OK)
	{
		IPropertyBag* pPropBag;
		hr = pMoniker->BindToStorage(0, 0, IID_PPV_ARGS(&pPropBag));
		if (FAILED(hr))
		{
			pMoniker->Release();
			continue;
		}

		VideoInputDsDevice^ device = gcnew VideoInputDsDevice();

		VARIANT var;
		VariantInit(&var);

		// 获取设备友好名
		hr = pPropBag->Read(L"FriendlyName", &var, 0);

		if (FAILED(hr))
			hr = pPropBag->Read(L"Description", &var, 0);

		if (SUCCEEDED(hr))
		{
			device->FriendlyName = System::String(var.bstrVal).ToString();
			VariantClear(&var);
		}

		// 获取设备Moniker名
		LPOLESTR pOleDisplayName = reinterpret_cast<LPOLESTR>(CoTaskMemAlloc(MAX_MONIKER_NAME_LENGTH * 2));
		hr = pMoniker->GetDisplayName(NULL, NULL, &pOleDisplayName);

		if (SUCCEEDED(hr))
		{
			device->MonikerName = System::String(pOleDisplayName).ToString();
			array<VideoParams^>^ params;
			hr = EnumerateVideoParams(pMoniker, params);	// 枚举设备的采集参数
			if (SUCCEEDED(hr))
				device->Params = params;
		}

		CoTaskMemFree(pOleDisplayName);

		list->Add(device);
		pPropBag->Release();
		pMoniker->Release();
	}

	devices = list->ToArray();
	return S_OK;
}

Int32 DirectShow::GetVideoInputDevices([Out]array<VideoInputDsDevice^>^% devices)
{
	devices = nullptr;

	IEnumMoniker* pEnum;
	HRESULT hr = EnumerateDevices(CLSID_VideoInputDeviceCategory, &pEnum);		// 创建视频输入设备枚举器

	if (FAILED(hr))
		return hr;

	hr = EnumerateVideoInputDevices(pEnum, devices);	// 枚举视频输入设备
	pEnum->Release();
	return hr;
}

HRESULT DirectShow::EnumerateAudioParams(IMoniker* pMoniker, [Out]array<AudioParams^>^% params)
{
	params = nullptr;
	IBaseFilter* pFilter;
	HRESULT	hr = pMoniker->BindToObject(NULL, NULL, IID_IBaseFilter, (void**)&pFilter);

	if (FAILED(hr))
		return hr;

	IEnumPins* pinEnum;
	hr = pFilter->EnumPins(&pinEnum);

	if (FAILED(hr)) {
		pFilter->Release();
		return hr;
	}

	List<AudioParams^>^ list = gcnew List<AudioParams^>();
	IPin* pPins;
	while (pinEnum->Next(1, &pPins, NULL) == S_OK)
	{
		PIN_INFO pinInfo;
		hr = pPins->QueryPinInfo(&pinInfo);

		if (FAILED(hr) || pinInfo.dir != PINDIR_OUTPUT)
		{
			pPins->Release();
			continue;
		}

		IEnumMediaTypes* mtEnum;
		hr = pPins->EnumMediaTypes(&mtEnum);

		if (FAILED(hr))
		{
			pPins->Release();
			continue;
		}

		AM_MEDIA_TYPE* mt;
		while (mtEnum->Next(1, &mt, NULL) == S_OK)
		{
			if (mt->formattype != FORMAT_WaveFormatEx)
				continue;

			WAVEFORMATEX* pVih = reinterpret_cast<WAVEFORMATEX*>(mt->pbFormat);
			AudioParams^ param = gcnew AudioParams();
			param->Format = pVih->wFormatTag;
			param->Channels = pVih->nChannels;
			param->SampleRate = pVih->nSamplesPerSec;
			param->BlockAlign = pVih->nBlockAlign;
			param->BitsPerSample = pVih->wBitsPerSample;

			Boolean isExit = false;
			for each (AudioParams ^ item in list)
			{
				if (item->Format == param->Format && item->Channels == param->Channels && item->SampleRate == param->SampleRate && item->BlockAlign == param->BlockAlign && item->BitsPerSample == param->BitsPerSample)
				{
					isExit = true;
					break;
				}
			}
			if (!isExit)
				list->Add(param);
		}
		pPins->Release();
	}

	pFilter->Release();

	params = list->ToArray();
	return S_OK;
}

HRESULT DirectShow::EnumerateAudioInputDevices(IEnumMoniker* pEnum, [Out]array<AudioInputDsDevice^>^% devices)
{
	HRESULT hr = S_FALSE;
	devices = nullptr;
	List<AudioInputDsDevice^>^ list = gcnew List<AudioInputDsDevice^>();

	IMoniker* pMoniker;
	while (pEnum->Next(1, &pMoniker, NULL) == S_OK)
	{
		IPropertyBag* pPropBag;
		hr = pMoniker->BindToStorage(0, 0, IID_PPV_ARGS(&pPropBag));
		if (FAILED(hr))
		{
			pMoniker->Release();
			continue;
		}

		AudioInputDsDevice^ device = gcnew AudioInputDsDevice();

		VARIANT var;
		VariantInit(&var);

		// 获取设备友好名
		hr = pPropBag->Read(L"FriendlyName", &var, 0);

		if (FAILED(hr))
			hr = pPropBag->Read(L"Description", &var, 0);

		if (SUCCEEDED(hr))
		{
			device->FriendlyName = System::String(var.bstrVal).ToString();
			VariantClear(&var);
		}

		// 获取设备Moniker名
		LPOLESTR pOleDisplayName = reinterpret_cast<LPOLESTR>(CoTaskMemAlloc(MAX_MONIKER_NAME_LENGTH * 2));
		hr = pMoniker->GetDisplayName(NULL, NULL, &pOleDisplayName);

		if (SUCCEEDED(hr))
		{
			device->MonikerName = System::String(pOleDisplayName).ToString();
			array<AudioParams^>^ params;
			hr = EnumerateAudioParams(pMoniker, params);
			if (SUCCEEDED(hr))
				device->Params = params;
		}

		CoTaskMemFree(pOleDisplayName);

		list->Add(device);
		pPropBag->Release();
		pMoniker->Release();
	}

	devices = list->ToArray();
	return S_OK;
}

Int32 DirectShow::GetAudioInputDevices([Out]array<AudioInputDsDevice^>^% devices)
{
	devices = nullptr;

	IEnumMoniker* pEnum;
	HRESULT hr = EnumerateDevices(CLSID_AudioInputDeviceCategory, &pEnum);		// 创建音频输入枚举器

	if (FAILED(hr))
		return hr;

	hr = EnumerateAudioInputDevices(pEnum, devices);	// 枚举音频输入设备
	pEnum->Release();
	return hr;
}

