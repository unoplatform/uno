using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Runtime.Skia.MacOS;

// keep in sync with UNOWindow.h
internal enum NativeMouseEvents
{
	None,
	Entered,
	Exited,
	Down,
	Up,
	Moved,
	ScrollWheel,
}

// values are set in native code
#pragma warning disable 0649
[StructLayout(LayoutKind.Sequential)]
internal struct NativeMouseEventData
{
	public NativeMouseEvents EventType;
	public double /* CGFloat */ X;
	public double /* CGFloat */ Y;
	// mouse
	[MarshalAs(UnmanagedType.I4)]
	public bool InContact;
	public uint MouseButtons;
	// pen
	public float TiltX;
	public float TiltY;
	public float Pressure;
	// scroll wheel
	public int ScrollingDeltaX;
	public int ScrollingDeltaY;
	// others
	public VirtualKeyModifiers KeyModifiers;
	public PointerDeviceType PointerDeviceType;
	public uint FrameId;
	public ulong Timestamp;
	public uint Pid;
};

// keep in sync with UNOClipboard.h
[NativeMarshalling(typeof(ClipboardDataMarshaller))]
internal struct NativeClipboardData
{
	public string? HtmlContent;
	public string? RtfContent;
	public string? TextContent;
	public string? Uri;
	public string? FileUrl;

	public string? BitmapFormat;
	public string? BitmapPath;
	public byte[]? BitmapData;
};
#pragma warning restore 0649

[CustomMarshaller(typeof(NativeClipboardData), MarshalMode.Default, typeof(ClipboardDataMarshaller))]
internal static unsafe class ClipboardDataMarshaller
{
	public static ClipboardDataUnmanaged ConvertToUnmanaged(NativeClipboardData managed)
	{
		var size = (nuint)(managed.BitmapData?.Length ?? 0);
		void* data = null;
		if (size > 0)
		{
			data = NativeMemory.Alloc(size);
			fixed (void* ptr = managed.BitmapData)
			{
				NativeMemory.Copy(ptr, data, size);
			}
		}

		return new ClipboardDataUnmanaged()
		{
			HtmlContent = Utf8StringMarshaller.ConvertToUnmanaged(managed.HtmlContent),
			RtfContent = Utf8StringMarshaller.ConvertToUnmanaged(managed.RtfContent),
			TextContent = Utf8StringMarshaller.ConvertToUnmanaged(managed.TextContent),
			Uri = Utf8StringMarshaller.ConvertToUnmanaged(managed.Uri),
			FileUrl = Utf8StringMarshaller.ConvertToUnmanaged(managed.FileUrl),
			BitmapFormat = Utf8StringMarshaller.ConvertToUnmanaged(managed.BitmapFormat),
			BitmapPath = Utf8StringMarshaller.ConvertToUnmanaged(managed.BitmapPath),
			BitmapData = data,
			BitmapSize = size,
		};
	}

	public static NativeClipboardData ConvertToManaged(ClipboardDataUnmanaged unmanaged)
	{
		var data = Array.Empty<byte>();
		if (unmanaged.BitmapData is not null)
		{
			data = new byte[unmanaged.BitmapSize];
			fixed (void* ptr = data)
			{
				NativeMemory.Copy(unmanaged.BitmapData, ptr, (nuint)unmanaged.BitmapSize);
			}
		}

		return new NativeClipboardData()
		{
			HtmlContent = Utf8StringMarshaller.ConvertToManaged(unmanaged.HtmlContent),
			RtfContent = Utf8StringMarshaller.ConvertToManaged(unmanaged.RtfContent),
			TextContent = Utf8StringMarshaller.ConvertToManaged(unmanaged.TextContent),
			Uri = Utf8StringMarshaller.ConvertToManaged(unmanaged.Uri),
			FileUrl = Utf8StringMarshaller.ConvertToManaged(unmanaged.FileUrl),
			BitmapFormat = Utf8StringMarshaller.ConvertToManaged(unmanaged.BitmapFormat),
			BitmapPath = Utf8StringMarshaller.ConvertToManaged(unmanaged.BitmapPath),
			BitmapData = data,
		};
	}

	public static void Free(ClipboardDataUnmanaged unmanaged)
	{
		Utf8StringMarshaller.Free(unmanaged.HtmlContent);
		Utf8StringMarshaller.Free(unmanaged.RtfContent);
		Utf8StringMarshaller.Free(unmanaged.TextContent);
		Utf8StringMarshaller.Free(unmanaged.Uri);
		Utf8StringMarshaller.Free(unmanaged.FileUrl);
		Utf8StringMarshaller.Free(unmanaged.BitmapFormat);
		Utf8StringMarshaller.Free(unmanaged.BitmapPath);
		NativeMemory.Free(unmanaged.BitmapData);
	}

	internal struct ClipboardDataUnmanaged
	{
		public byte* HtmlContent;
		public byte* RtfContent;
		public byte* TextContent;
		public byte* Uri;
		public byte* FileUrl;
		public byte* BitmapFormat;
		public byte* BitmapPath;
		public void* BitmapData;
		public ulong BitmapSize;
	}
}

internal static partial class NativeUno
{
	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_app_initialize([MarshalAs(UnmanagedType.I1)] ref bool supportsMetal);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial nint uno_app_get_main_window();

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_application_set_badge(string badge);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_application_set_icon(string iconPath);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_application_open_url(string url);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_application_query_url_support(string url);

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_application_is_bundled();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_drawing_callbacks(
		delegate* unmanaged[Cdecl]<nint, double, double, nint, void> metalCallback,
		delegate* unmanaged[Cdecl]<nint, double, double, nint*, int*, int*, void> softCallback,
		delegate* unmanaged[Cdecl]<nint, double, double, void> resizeCallback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_window_screen_change_callbacks(
		delegate* unmanaged[Cdecl]<nint, uint, uint, double, void> screenChangeCallback,
		delegate* unmanaged[Cdecl]<nint, void> screenParametersCallback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_window_notify_screen_change(nint handle);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_system_theme_change_callback(delegate* unmanaged[Cdecl]<void> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_window_events_callbacks(
		delegate* unmanaged[Cdecl]<nint, VirtualKey, VirtualKeyModifiers, uint, ushort, int> keyDownCallback,
		delegate* unmanaged[Cdecl]<nint, VirtualKey, VirtualKeyModifiers, uint, ushort, int> keyUpCallback,
		delegate* unmanaged[Cdecl]<nint, NativeMouseEventData*, int> pointerCallback,
		delegate* unmanaged[Cdecl]<nint, double, double, void> moveCallback,
		delegate* unmanaged[Cdecl]<nint, double, double, void> resizeCallback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial uint uno_get_system_theme();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_application_start_callback(delegate* unmanaged[Cdecl]<void> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_application_can_exit_callback(delegate* unmanaged[Cdecl]<int> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_application_quit();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial nint uno_window_create(double width, double height);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_activate(nint window);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_invalidate(nint window);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_close(nint window);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_get_position(nint window, out double x, out double y);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial string uno_window_get_title(nint window);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_window_set_title(nint window, string title);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_window_close_callbacks(
		delegate* unmanaged[Cdecl]<nint, int> shouldCloseCallback,
		delegate* unmanaged[Cdecl]<nint, void> closeCallback);

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_window_enter_full_screen(nint window);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_exit_full_screen(nint window);

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_window_is_full_screen(nint window);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_maximize(nint window);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_minimize(nint window, [MarshalAs(UnmanagedType.I1)] bool activateWindow);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_restore(nint window, [MarshalAs(UnmanagedType.I1)] bool activateWindow);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial int uno_window_get_overlapped_presenter_state(nint window);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_set_always_on_top(nint window, [MarshalAs(UnmanagedType.I1)] bool isAlwaysOnTop);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_set_border_and_title_bar(nint window, [MarshalAs(UnmanagedType.I1)] bool hasBorder, [MarshalAs(UnmanagedType.I1)] bool hasTitleBar);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_set_maximizable(nint window, [MarshalAs(UnmanagedType.I1)] bool isMaximizable);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_set_minimizable(nint window, [MarshalAs(UnmanagedType.I1)] bool isMinimizable);

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_window_set_modal(nint window, [MarshalAs(UnmanagedType.I1)] bool isModal);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_set_resizable(nint window, [MarshalAs(UnmanagedType.I1)] bool isResizable);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_get_metal_handles(nint window, out nint device, out nint queue);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_move(nint window, double x, double y);

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_window_resize(nint window, double width, double height);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_set_min_size(nint window, double width, double height);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_window_clip_svg(nint window, string? svg);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial string? /* const char* _Nullable */ uno_pick_single_folder(string? prompt, string? identifier, int suggestedStartLocation);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial string? /* const char* _Nullable */ uno_pick_single_file(string? prompt, string? identifier, int suggestedStartLocation,
		string[] filters, int filterSize);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial IntPtr /* const char* _Nullable * _Nullable */ uno_pick_multiple_files(string? prompt, string? identifier, int suggestedStartLocation,
		string[] filters, int filterSize);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial string? /* const char* _Nullable */ uno_pick_save_file(string? prompt, string? identifier, string? suggestedFileName, int suggestedStartLocation,
		string[] filters, int filtersSize);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_clipboard_clear();

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_clipboard_get_content(ref NativeClipboardData data);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_clipboard_set_content(ref NativeClipboardData data);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_clipboard_start_content_changed();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_clipboard_stop_content_changed();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_clipboard_set_content_changed_callback(delegate* unmanaged[Cdecl]<void> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_cursor_hide();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_cursor_show();

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_cursor_set(CoreCursorType cursorType);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial nint uno_native_create_sample(nint window, string text);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_native_arrange(nint element, double arrangeLeft, double arrangeTop, double arrangeWidth, double arrangeHeight, double clipLeft, double clipTop, double clipWidth, double clipHeight);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_native_attach(nint element);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_native_detach(nint element);

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_native_is_attached(nint element);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_native_set_opacity(nint element, double opacity);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_native_set_visibility(nint element, [MarshalAs(UnmanagedType.I1)] bool visible);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_native_measure(nint element, double childWidth, double childHeight, double availableWidth, double availableHeight, out double width, out double height);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial nint uno_set_execute_callback(delegate* unmanaged[Cdecl]<IntPtr, sbyte*, sbyte*, void> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial nint uno_set_invoke_callback(delegate* unmanaged[Cdecl]<IntPtr, sbyte*, sbyte*, void> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_webview_navigation_callbacks(
		delegate* unmanaged[Cdecl]<IntPtr, sbyte*, int> starting,
		delegate* unmanaged[Cdecl]<IntPtr, sbyte*, void> finishing,
		delegate* unmanaged[Cdecl]<IntPtr, long, void> notification,
		delegate* unmanaged[Cdecl]<IntPtr, sbyte*, void> receiveWebMessage,
		delegate* unmanaged[Cdecl]<IntPtr, sbyte*, CoreWebView2WebErrorStatus, void> failing
		);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_webview_unsupported_scheme_identified_callback(delegate* unmanaged[Cdecl]<IntPtr, sbyte*, int> callback);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial nint uno_webview_create(nint window, string ok, string cancel);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_webview_dispose(nint webview);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial string uno_webview_get_title(nint webview);

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_webview_can_go_back(nint webview);

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_webview_can_go_forward(nint webview);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_webview_go_back(nint webview);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_webview_go_forward(nint webview);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_webview_navigate(nint webview, string? url, string? headers);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_webview_load_html(nint webview, string? html);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_webview_reload(nint webview);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_webview_stop(nint webview);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_webview_execute_script(nint webview, nint handle, string javascript);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_webview_invoke_script(nint webview, nint handle, string javascript);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_webview_set_scrolling_enabled(nint webview, [MarshalAs(UnmanagedType.I1)] bool enabled);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_mediaplayer_set_callbacks(
		delegate* unmanaged[Cdecl]<IntPtr, double, void> periodicPositionUpdate,
		delegate* unmanaged[Cdecl]<IntPtr, double, void> onRateChanged,
		delegate* unmanaged[Cdecl]<IntPtr, double, double, void> onVideoDimensionChanged,
		delegate* unmanaged[Cdecl]<IntPtr, double, void> onDurationChanged,
		delegate* unmanaged[Cdecl]<IntPtr, double, void> onReadyToPlay,
		delegate* unmanaged[Cdecl]<IntPtr, double, void> onBufferingProgressChanged,
		delegate* unmanaged[Cdecl]<IntPtr, void> onMediaOpened,
		delegate* unmanaged[Cdecl]<IntPtr, void> onMediaEnded,
		delegate* unmanaged[Cdecl]<IntPtr, void> onMediaFailed,
		delegate* unmanaged[Cdecl]<IntPtr, void> onMediaStalled
		);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial nint uno_mediaplayer_create();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial nint uno_mediaplayer_set_notifications(nint media);

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_mediaplayer_is_video(nint media);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial double uno_mediaplayer_get_current_time(nint media);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_mediaplayer_set_current_time(nint media, double seconds);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial float uno_mediaplayer_get_rate(nint media);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_mediaplayer_set_rate(nint media, float rate);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_mediaplayer_set_source_path(nint media, string path);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_mediaplayer_set_source_uri(nint media, string uri);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_mediaplayer_set_stretch(nint media, Stretch stretch);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_mediaplayer_set_volume(nint media, float volume);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_mediaplayer_pause(nint media);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_mediaplayer_play(nint media);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_mediaplayer_stop(nint media);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_mediaplayer_toggle_mute(nint media);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_mediaplayer_step_by(nint media, int frames);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial nint uno_mediaplayer_create_view();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial nint uno_mediaplayer_set_view(nint media, nint view, nint window);
}
