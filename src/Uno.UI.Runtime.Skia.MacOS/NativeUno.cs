using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Core;

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
	// scrollwheel
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
		byte[] data = Array.Empty<byte>();
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
	internal static unsafe partial void uno_set_draw_callback(delegate* unmanaged[Cdecl]<nint, double, double, nint, void> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_soft_draw_callback(delegate* unmanaged[Cdecl]<nint, double, double, nint*, int*, int*, void> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_resize_callback(delegate* unmanaged[Cdecl]<nint, double, double, void> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_window_did_change_screen_callback(ref MacOSDisplayInformationExtension.ScreenData screenData, delegate* unmanaged[Cdecl]<void> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_window_did_change_screen_parameters_callback(delegate* unmanaged[Cdecl]<void> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_system_theme_change_callback(delegate* unmanaged[Cdecl]<void> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_window_key_down_callback(delegate* unmanaged[Cdecl]<VirtualKey, VirtualKeyModifiers, uint, int> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_window_key_up_callback(delegate* unmanaged[Cdecl]<VirtualKey, VirtualKeyModifiers, uint, int> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial uint uno_get_system_theme();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_application_can_exit_callback(delegate* unmanaged[Cdecl]<int> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_application_quit();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial nint uno_window_create(double width, double height);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_invalidate(nint window);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial nint uno_window_set_title(nint window, string title);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_window_mouse_event_callback(delegate* unmanaged[Cdecl]<NativeMouseEventData*, int> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_window_should_close_callback(delegate* unmanaged[Cdecl]<int> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_application_is_full_screen();

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_application_enter_full_screen();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_application_exit_full_screen();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial nint uno_window_get_metal_context(nint window);

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_window_resize(nint window, double width, double height);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_window_set_min_size(nint window, double width, double height);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial string? /* const char* _Nullable */ uno_pick_single_folder();

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial string? /* const char* _Nullable */ uno_pick_single_file(string? prompt);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial IntPtr /* const char* _Nullable * _Nullable */ uno_pick_multiple_files(string? prompt);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial string? /* const char* _Nullable */ uno_pick_save_file(string? prompt);

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
	internal static partial void uno_cursor_unhide();

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_cursor_set(CoreCursorType cursor);
}
