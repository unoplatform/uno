// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayoutTrace.h, commit b8cfb8490

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

static partial class LinedFlowLayoutTrace
{
	// TODO Uno: Uno has no TraceLogging providers, OutputDebugStringW backend, or
	// MUXControlsTestHooks logging bridge. The complete header implementation remains
	// commented original source so its switches, formatting, and backend behavior are not lost.
	// Original C++:
	// #pragma once
	//
	// #include "common.h"
	// #include "MuxcTraceLogging.h"
	// #include "Utils.h"
	// #include "MUXControlsTestHooks.h"
	//
	// inline bool IsLinedFlowLayoutTracingEnabled()
	// {
	//     return g_IsLoggingProviderEnabled &&
	//         g_LoggingProviderLevel >= WINEVENT_LEVEL_INFO &&
	//         (g_LoggingProviderMatchAnyKeyword & KEYWORD_LINEDFLOWLAYOUT || g_LoggingProviderMatchAnyKeyword == 0);
	// }
	//
	// inline bool IsLinedFlowLayoutVerboseTracingEnabled()
	// {
	//     return g_IsLoggingProviderEnabled &&
	//         g_LoggingProviderLevel >= WINEVENT_LEVEL_VERBOSE &&
	//         (g_LoggingProviderMatchAnyKeyword & KEYWORD_LINEDFLOWLAYOUT || g_LoggingProviderMatchAnyKeyword == 0);
	// }
	//
	// inline bool IsLinedFlowLayoutPerfTracingEnabled()
	// {
	//     return g_IsPerfProviderEnabled &&
	//         g_PerfProviderLevel >= WINEVENT_LEVEL_INFO &&
	//         (g_PerfProviderMatchAnyKeyword & KEYWORD_LINEDFLOWLAYOUT || g_PerfProviderMatchAnyKeyword == 0);
	// }
	//
	// #define LINEDFLOWLAYOUT_TRACE_INFO_ENABLED(includeTraceLogging, sender, message, ...) \
	// LinedFlowLayoutTrace::TraceInfo(includeTraceLogging, sender, message, __VA_ARGS__); \
	//
	// #define LINEDFLOWLAYOUT_TRACE_INFO(sender, message, ...) \
	// if (IsLinedFlowLayoutTracingEnabled()) \
	// { \
	//     LINEDFLOWLAYOUT_TRACE_INFO_ENABLED(true /*includeTraceLogging*/, sender, message, __VA_ARGS__); \
	// } \
	// else if (LinedFlowLayoutTrace::s_IsDebugOutputEnabled || LinedFlowLayoutTrace::s_IsVerboseDebugOutputEnabled) \
	// { \
	//     LINEDFLOWLAYOUT_TRACE_INFO_ENABLED(false /*includeTraceLogging*/, sender, message, __VA_ARGS__); \
	// } \
	//
	// #define LINEDFLOWLAYOUT_TRACE_VERBOSE_ENABLED(includeTraceLogging, sender, message, ...) \
	// LinedFlowLayoutTrace::TraceVerbose(includeTraceLogging, sender, message, __VA_ARGS__); \
	//
	// #define LINEDFLOWLAYOUT_TRACE_VERBOSE(sender, message, ...) \
	// if (IsLinedFlowLayoutVerboseTracingEnabled()) \
	// { \
	//     LINEDFLOWLAYOUT_TRACE_VERBOSE_ENABLED(true /*includeTraceLogging*/, sender, message, __VA_ARGS__); \
	// } \
	// else if (LinedFlowLayoutTrace::s_IsVerboseDebugOutputEnabled) \
	// { \
	//     LINEDFLOWLAYOUT_TRACE_VERBOSE_ENABLED(false /*includeTraceLogging*/, sender, message, __VA_ARGS__); \
	// } \
	//
	// #define LINEDFLOWLAYOUT_TRACE_PERF(info) \
	// if (IsLinedFlowLayoutPerfTracingEnabled()) \
	// { \
	//     LINEDFLOWLAYOUTTrace::TracePerfInfo(info); \
	// } \
	//
	// #ifdef DBG
	// #define LINEDFLOWLAYOUT_TRACE_INFO_DBG(sender, message, ...)    LINEDFLOWLAYOUT_TRACE_INFO(sender, message, __VA_ARGS__)
	// #define LINEDFLOWLAYOUT_TRACE_VERBOSE_DBG(sender, message, ...) LINEDFLOWLAYOUT_TRACE_VERBOSE(sender, message, __VA_ARGS__)
	// #define LINEDFLOWLAYOUT_TRACE_PERF_DBG(info)                    LINEDFLOWLAYOUT_TRACE_PERF(info)
	// #else
	// #define LINEDFLOWLAYOUT_TRACE_INFO_DBG(sender, message, ...)
	// #define LINEDFLOWLAYOUT_TRACE_VERBOSE_DBG(sender, message, ...)
	// #define LINEDFLOWLAYOUT_TRACE_PERF_DBG(info)
	// #endif // DBG
	//
	// class LinedFlowLayoutTrace
	// {
	// public:
	//     static bool s_IsDebugOutputEnabled;
	//     static bool s_IsVerboseDebugOutputEnabled;
	//
	//     static void TraceInfo(bool includeTraceLogging, const winrt::IInspectable& sender, PCWSTR message, ...) noexcept
	//     {
	//         va_list args;
	//         va_start(args, message);
	//         WCHAR buffer[384]{};
	//         if (SUCCEEDED(StringCchVPrintfW(buffer, ARRAYSIZE(buffer), message, args)))
	//         {
	//             if (includeTraceLogging)
	//             {
	//                 // TraceViewers
	//                 // http://toolbox/pef
	//                 // http://fastetw/index.aspx
	//                 TraceLoggingWrite(
	//                     g_hLoggingProvider,
	//                     "LinedFlowLayoutInfo" /* eventName */,
	//                     TraceLoggingLevel(WINEVENT_LEVEL_INFO),
	//                     TraceLoggingKeyword(KEYWORD_LINEDFLOWLAYOUT),
	//                     TraceLoggingWideString(buffer, "Message"));
	//             }
	//
	//             if (s_IsDebugOutputEnabled)
	//             {
	//                 OutputDebugStringW(buffer);
	//             }
	//
	//             com_ptr<MUXControlsTestHooks> globalTestHooks = MUXControlsTestHooks::GetGlobalTestHooks();
	//
	//             if (globalTestHooks &&
	//                 (globalTestHooks->GetLoggingLevelForType(L"LinedFlowLayout") >= WINEVENT_LEVEL_INFO || globalTestHooks->GetLoggingLevelForInstance(sender) >= WINEVENT_LEVEL_INFO))
	//             {
	//                 globalTestHooks->LogMessage(sender, buffer, false /*isVerboseLevel*/);
	//             }
	//         }
	//         va_end(args);
	//     }
	//
	//     static void TraceVerbose(bool includeTraceLogging, const winrt::IInspectable& sender, PCWSTR message, ...) noexcept
	//     {
	//         va_list args;
	//         va_start(args, message);
	//         WCHAR buffer[1024]{};
	//         const HRESULT hr = StringCchVPrintfW(buffer, ARRAYSIZE(buffer), message, args);
	//         if (SUCCEEDED(hr) || hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
	//         {
	//             if (includeTraceLogging)
	//             {
	//                 // TraceViewers
	//                 // http://toolbox/pef
	//                 // http://fastetw/index.aspx
	//                 TraceLoggingWrite(
	//                     g_hLoggingProvider,
	//                     "LinedFlowLayoutVerbose" /* eventName */,
	//                     TraceLoggingLevel(WINEVENT_LEVEL_VERBOSE),
	//                     TraceLoggingKeyword(KEYWORD_LINEDFLOWLAYOUT),
	//                     TraceLoggingWideString(buffer, "Message"));
	//             }
	//
	//             if (s_IsDebugOutputEnabled || s_IsVerboseDebugOutputEnabled)
	//             {
	//                 OutputDebugStringW(buffer);
	//             }
	//
	//             com_ptr<MUXControlsTestHooks> globalTestHooks = MUXControlsTestHooks::GetGlobalTestHooks();
	//
	//             if (globalTestHooks &&
	//                 (globalTestHooks->GetLoggingLevelForType(L"LinedFlowLayout") >= WINEVENT_LEVEL_VERBOSE || globalTestHooks->GetLoggingLevelForInstance(sender) >= WINEVENT_LEVEL_VERBOSE))
	//             {
	//                 globalTestHooks->LogMessage(sender, buffer, true /*isVerboseLevel*/);
	//             }
	//         }
	//         va_end(args);
	//     }
	//
	//     static void TracePerfInfo(PCWSTR info) noexcept
	//     {
	//         // TraceViewers
	//         // http://toolbox/pef
	//         // http://fastetw/index.aspx
	//         TraceLoggingWrite(
	//             g_hPerfProvider,
	//             "LinedFlowLayoutPerf" /* eventName */,
	//             TraceLoggingLevel(WINEVENT_LEVEL_INFO),
	//             TraceLoggingKeyword(KEYWORD_LINEDFLOWLAYOUT),
	//             TraceLoggingWideString(info, "Info"));
	//     }
	// };
}
