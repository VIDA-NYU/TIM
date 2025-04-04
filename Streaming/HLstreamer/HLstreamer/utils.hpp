#pragma once
#include <sstream>
#include <windows.h>
#include <winrt/Windows.Foundation.h>

inline void DebugPrint_ModuleMessage() {
	OutputDebugStringA("HLStreamer - ");
}

inline void DebugPrint_impl(std::stringstream& ss) {
	ss << std::endl;
}

template<typename T, typename... Targs>
inline void DebugPrint_impl(std::stringstream& ss, T arg, Targs... args) {
	ss << arg << ' ';
	DebugPrint_impl(ss, args...);
}

template<typename... Targs>
inline void DebugPrint(Targs... args) {
	DebugPrint_ModuleMessage();
	std::stringstream ss;
	DebugPrint_impl(ss, args...);
	OutputDebugStringA(ss.str().c_str());
}

inline void DebugPrint(winrt::hstring str) {
	DebugPrint_ModuleMessage();
	OutputDebugString(str.c_str());
	OutputDebugString(L"\n");
}
