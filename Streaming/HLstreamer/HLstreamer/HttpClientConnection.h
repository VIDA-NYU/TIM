#pragma once
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Web.Http.h>
#include <winrt/Windows.Storage.Streams.h>
#include "FrameHeader.h"
#include <vector>

class HttpClientConnection
{
public:
	void ConnectTo(winrt::hstring string);
	winrt::Windows::Foundation::IAsyncAction StoreAsync();
	bool IsReadyToSendAndLock();
	void WriteHeader(FrameHeader& header);
	template<typename T>
	void WriteVector(std::vector<T>& vec);

	winrt::Windows::Storage::Streams::DataWriter m_writer = nullptr;
	winrt::Windows::Foundation::Uri m_uri = nullptr;
private:
	winrt::Windows::Web::Http::HttpClient m_httpclient = nullptr;
	bool m_readyToSend = false;
};

template<typename T>
inline void HttpClientConnection::WriteVector(std::vector<T>& vec)
{
	m_writer.WriteBytes({ (byte*)vec.data() , (uint32_t)vec.size() * sizeof(T) });
}
