#pragma once

#include "MainPage.g.h"
#include "HLstreamer.h"

namespace winrt::HLstreamer::implementation
{
    struct MainPage : MainPageT<MainPage>
    {
        MainPage();

        void Start_Click(winrt::Windows::Foundation::IInspectable const& sender, winrt::Windows::UI::Xaml::RoutedEventArgs const& e);
        void Stop_Click(winrt::Windows::Foundation::IInspectable const& sender, winrt::Windows::UI::Xaml::RoutedEventArgs const& e);

        HLStreamer HLStreamer;
    };
}

namespace winrt::HLstreamer::factory_implementation
{
    struct MainPage : MainPageT<MainPage, implementation::MainPage>
    {
    };
}
