#nullable enable

using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Wayland.Native;

internal static unsafe partial class WaylandNative
{
	private const string Library = "libwayland-client.so.0";
	[LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
	internal static partial nint wl_display_connect(string? name);
	[LibraryImport(Library)] internal static partial nint wl_display_connect_to_fd(int fd);
	[LibraryImport(Library)] internal static partial void wl_display_disconnect(nint display);
	[LibraryImport(Library)] internal static partial int wl_display_get_fd(nint display);
	[LibraryImport(Library)] internal static partial int wl_display_dispatch(nint display);
	[LibraryImport(Library)] internal static partial int wl_display_dispatch_pending(nint display);
	[LibraryImport(Library)] internal static partial int wl_display_prepare_read(nint display);
	[LibraryImport(Library)] internal static partial int wl_display_read_events(nint display);
	[LibraryImport(Library)] internal static partial void wl_display_cancel_read(nint display);
	[LibraryImport(Library, SetLastError = true)] internal static partial int wl_display_flush(nint display);
	[LibraryImport(Library)] internal static partial int wl_display_roundtrip(nint display);
	[LibraryImport(Library)] internal static partial int wl_display_get_error(nint display);
	[LibraryImport(Library)] internal static partial uint wl_display_get_protocol_error(nint display, WlInterface** @interface, uint* id);
	[LibraryImport(Library)] internal static partial nint wl_display_create_queue(nint display);
	[LibraryImport(Library)] internal static partial void wl_event_queue_destroy(nint queue);
	[LibraryImport(Library)] internal static partial void wl_proxy_set_queue(nint proxy, nint queue);
	[LibraryImport(Library)] internal static partial int wl_display_dispatch_queue_pending(nint display, nint queue);
	[LibraryImport(Library)] internal static partial int wl_display_prepare_read_queue(nint display, nint queue);
	[LibraryImport(Library)] internal static partial int wl_display_dispatch_queue(nint display, nint queue);
	[LibraryImport(Library)] internal static partial int wl_display_roundtrip_queue(nint display, nint queue);
	[LibraryImport(Library)] internal static partial nint wl_proxy_marshal_array_flags(nint proxy, uint opcode, WlInterface* @interface, uint version, uint flags, WlArgument* args);
	[LibraryImport(Library)] internal static partial int wl_proxy_add_listener(nint proxy, nint* implementation, nint data);
	[LibraryImport(Library)] internal static partial uint wl_proxy_get_version(nint proxy);
	[LibraryImport(Library)] internal static partial void wl_proxy_destroy(nint proxy);
	[LibraryImport(Library)] internal static partial void wl_proxy_set_user_data(nint proxy, nint data);
	[LibraryImport(Library)] internal static partial nint wl_proxy_get_user_data(nint proxy);
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WlInterface
{
	internal byte* Name;
	internal int Version;
	internal int MethodCount;
	internal WlMessage* Methods;
	internal int EventCount;
	internal WlMessage* Events;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WlMessage
{
	internal byte* Name;
	internal byte* Signature;
	internal WlInterface** Types;
}

[StructLayout(LayoutKind.Explicit)]
internal struct WlArgument
{
	[FieldOffset(0)] internal int Int;
	[FieldOffset(0)] internal uint Uint;
	[FieldOffset(0)] internal nint Pointer;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WlArray
{
	internal nuint Size;
	internal nuint Alloc;
	internal nint Data;
}

internal static unsafe partial class Posix
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct PollFd
	{
		internal int Fd;
		internal short Events;
		internal short Revents;
	}
	[LibraryImport("libc", SetLastError = true)] internal static partial int poll(PollFd* fds, nuint count, int timeout);
	[LibraryImport("libc", SetLastError = true)] internal static partial int fcntl(int fd, int command, int argument);
	[LibraryImport("libc", SetLastError = true)] internal static partial int pipe2(int* fds, int flags);
	[LibraryImport("libc", SetLastError = true)] internal static partial nint write(int fd, byte* buffer, nuint count);
	[LibraryImport("libc", SetLastError = true)] internal static partial nint read(int fd, byte* buffer, nuint count);
	[LibraryImport("libc")] internal static partial int close(int fd);
}
