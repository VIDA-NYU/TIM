#include "HttpClientConnection.h"
#include "utils.hpp"
#include <winrt/Windows.Web.h>
#include <winrt/Windows.Web.Http.Headers.h>
#include <winrt/Windows.Storage.Streams.h>

using namespace winrt;
using namespace winrt::Windows::Web::Http;
using namespace winrt::Windows::Web::Http::Headers;
using namespace winrt::Windows::Storage::Streams;
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Web;

namespace winrt {
	hstring to_hstring(WebErrorStatus value)
	{
		switch (value)
		{
		case WebErrorStatus::Unknown: return L"Unknown";
		case WebErrorStatus::CertificateCommonNameIsIncorrect: return L"CertificateCommonNameIsIncorrect";
		case WebErrorStatus::CertificateExpired: return L"CertificateExpired";
		case WebErrorStatus::CertificateContainsErrors: return L"CertificateContainsErrors";
		case WebErrorStatus::CertificateRevoked: return L"CertificateRevoked";
		case WebErrorStatus::CertificateIsInvalid: return L"CertificateIsInvalid";
		case WebErrorStatus::ServerUnreachable: return L"ServerUnreachable";
		case WebErrorStatus::Timeout: return L"Timeout";
		case WebErrorStatus::ErrorHttpInvalidServerResponse: return L"ErrorHttpInvalidServerResponse";
		case WebErrorStatus::ConnectionAborted: return L"ConnectionAborted";
		case WebErrorStatus::ConnectionReset: return L"ConnectionReset";
		case WebErrorStatus::Disconnected: return L"Disconnected";
		case WebErrorStatus::HttpToHttpsOnRedirection: return L"HttpToHttpsOnRedirection";
		case WebErrorStatus::HttpsToHttpOnRedirection: return L"HttpsToHttpOnRedirection";
		case WebErrorStatus::CannotConnect: return L"CannotConnect";
		case WebErrorStatus::HostNameNotResolved: return L"HostNameNotResolved";
		case WebErrorStatus::OperationCanceled: return L"OperationCanceled";
		case WebErrorStatus::RedirectFailed: return L"RedirectFailed";
		case WebErrorStatus::UnexpectedStatusCode: return L"UnexpectedStatusCode";
		case WebErrorStatus::UnexpectedRedirection: return L"UnexpectedRedirection";
		case WebErrorStatus::UnexpectedClientError: return L"UnexpectedClientError";
		case WebErrorStatus::UnexpectedServerError: return L"UnexpectedServerError";
		case WebErrorStatus::InsufficientRangeSupport: return L"InsufficientRangeSupport";
		case WebErrorStatus::MissingContentLengthSupport: return L"MissingContentLengthSupport";
		}
		return to_hstring(static_cast<int32_t>(value));
	}
}

void HttpClientConnection::ConnectTo(winrt::hstring string)
{
	m_httpclient = HttpClient();
	m_httpclient.DefaultRequestHeaders().Authorization(HttpCredentialsHeaderValue{ L"Bearer", L"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJzb25pYSIsImV4cCI6MTY2NDA1NTg1N30.LyyYwW1zxZnLAW0RfLlNyxpY1UPLwM7aoMkL2PPPQkw" });

	m_uri = Uri{ string };

	m_writer = winrt::Windows::Storage::Streams::DataWriter();
	m_writer.ByteOrder(winrt::Windows::Storage::Streams::ByteOrder::LittleEndian);
	m_readyToSend = true;
}

winrt::Windows::Foundation::IAsyncAction HttpClientConnection::StoreAsync()
{
	IBuffer buffer = m_writer.DetachBuffer();

	HttpMultipartFormDataContent form;
	form.Add(HttpBufferContent(buffer), L"entries", L"bin");

	HttpRequestResult result = co_await m_httpclient.TryPostAsync(m_uri, form);
	if (!result.Succeeded()) {
		WebErrorStatus webErrorStatus = WebError::GetStatus(result.ExtendedError());
		if (webErrorStatus == WebErrorStatus::Unknown)
		{
			DebugPrint(L"Unknown Error: " + hresult_error(result.ExtendedError()).message());
		}
		else
		{
			DebugPrint(L"Web Error: " + to_hstring(webErrorStatus) + L" - " + m_uri.ToString());
		}
	}

	m_readyToSend = true;
}

bool HttpClientConnection::IsReadyToSendAndLock()
{
	if (m_httpclient == nullptr || !m_readyToSend) return false;

	m_readyToSend = false;
	return true;
}

void HttpClientConnection::WriteHeader(FrameHeader& header)
{
	m_writer.WriteByte(header.VersionMajor);
	m_writer.WriteByte((uint8_t)header.FrameType);
	m_writer.WriteUInt64(header.Timestamp);
	m_writer.WriteUInt32(header.ImageWidth);
	m_writer.WriteUInt32(header.ImageHeight);
	m_writer.WriteUInt32(header.PixelStride);
	m_writer.WriteUInt32(header.ExtraInfoSize);
}
