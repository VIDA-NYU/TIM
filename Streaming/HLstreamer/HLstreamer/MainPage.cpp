#include "pch.h"
#include "MainPage.h"
#include "MainPage.g.cpp"

namespace winrt::HLstreamer::implementation
{
    MainPage::MainPage()
    {
        InitializeComponent();

		HLStreamer.Initialize();
    }
}


void winrt::HLstreamer::implementation::MainPage::Start_Click(winrt::Windows::Foundation::IInspectable const& sender, winrt::Windows::UI::Xaml::RoutedEventArgs const& e)
{
	HLStreamer.StartStreaming();

	StartBtn().IsEnabled(false);
	StopBtn().IsEnabled(true);
}


void winrt::HLstreamer::implementation::MainPage::Stop_Click(winrt::Windows::Foundation::IInspectable const& sender, winrt::Windows::UI::Xaml::RoutedEventArgs const& e)
{
	HLStreamer.StopStreaming();

	StartBtn().IsEnabled(true);
	StopBtn().IsEnabled(false);
}
