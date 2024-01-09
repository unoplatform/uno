#nullable enable

using System;
using System.Runtime.InteropServices;

using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.MacOS;

internal static partial class NativeUno 
{
	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial nint uno_app_initialize(string title);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial nint uno_app_get_main_window();

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_application_set_icon(string iconPath);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	[return: MarshalAs (UnmanagedType.I1)]
	internal static partial bool uno_application_open_url(string url);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	[return: MarshalAs (UnmanagedType.I1)]
	internal static partial bool uno_application_query_url_support(string url);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_draw_callback(delegate* unmanaged[Cdecl]<CGSize,nint,void> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_resize_callback(delegate* unmanaged[Cdecl]<CGSize,void> callback);

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
	internal static partial void uno_window_invalidate(nint window);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial nint uno_window_set_title(nint window, string title);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_window_mouse_event_callback(delegate* unmanaged[Cdecl]<int, double, double, VirtualKeyModifiers, PointerDeviceType, uint, ulong, uint, int> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_set_window_should_close_callback(delegate* unmanaged[Cdecl]<int> callback);

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs (UnmanagedType.I1)]
	internal static partial bool uno_application_is_fullscreen();

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs (UnmanagedType.I1)]
	internal static partial bool uno_application_enter_fullscreen();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_application_exit_fullscreen();

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

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial IntPtr uno_clipboard_get_content();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_clipboard_set_content(IntPtr content);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_clipboard_start_content_changed();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_clipboard_stop_content_changed();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_cursor_hide();

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_cursor_unhide();

	[LibraryImport("libUnoNativeMac.dylib")]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static partial bool uno_cursor_set(CoreCursorType cursor);
}
