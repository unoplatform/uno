// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Base interactions with libinput derived from https://github.com/AvaloniaUI/Avalonia
// Enhanced to add keyboard support

using System;
using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.Native
{
	unsafe class LibInput
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int OpenRestrictedCallbackDelegate(IntPtr path, int flags, IntPtr userData);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void CloseRestrictedCallbackDelegate(int fd, IntPtr userData);

		static int OpenRestricted(IntPtr path, int flags, IntPtr userData)
		{
			if (!(Marshal.PtrToStringAnsi(path) is { } pathAsString))
			{
				return -1;
			}

			var fd = Libc.open(pathAsString, flags, 0);

			if (fd == -1)
			{
				return -Marshal.GetLastWin32Error();
			}

			return fd;
		}

		static void CloseRestricted(int fd, IntPtr userData)
#pragma warning disable CA1806 // Do not ignore method results
			=> Libc.close(fd);
#pragma warning restore CA1806 // Do not ignore method results

		private static readonly IntPtr* s_Interface;

		static LibInput()
		{
			s_Interface = (IntPtr*)Marshal.AllocHGlobal(IntPtr.Size * 2);

			static IntPtr Convert<TDelegate>(TDelegate del) where TDelegate : notnull
			{
				GCHandle.Alloc(del);
				return Marshal.GetFunctionPointerForDelegate(del);
			}

			s_Interface[0] = Convert<OpenRestrictedCallbackDelegate>(OpenRestricted);
			s_Interface[1] = Convert<CloseRestrictedCallbackDelegate>(CloseRestricted);
		}

		private const string LibInputName = "libinput.so.10";

		[DllImport(LibInputName)]
		public extern static IntPtr libinput_path_create_context(IntPtr* iface, IntPtr userData);

		public static IntPtr libinput_path_create_context() =>
			libinput_path_create_context(s_Interface, IntPtr.Zero);

		[DllImport(LibInputName)]
		public extern static IntPtr libinput_path_add_device(IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string path);

		[DllImport(LibInputName)]
		public extern static IntPtr libinput_path_remove_device(IntPtr device);

		[DllImport(LibInputName)]
		public extern static int libinput_get_fd(IntPtr ctx);

		[DllImport(LibInputName)]
		public extern static int libinput_dispatch(IntPtr ctx);

		[DllImport(LibInputName)]
		public extern static IntPtr libinput_get_event(IntPtr ctx);

		[DllImport(LibInputName)]
		public extern static libinput_event_type libinput_event_get_type(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static void libinput_event_destroy(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static IntPtr libinput_event_get_touch_event(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static int libinput_event_touch_get_slot(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static ulong libinput_event_touch_get_time_usec(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static double libinput_event_touch_get_x_transformed(IntPtr ev, int width);

		[DllImport(LibInputName)]
		public extern static double libinput_event_touch_get_y_transformed(IntPtr ev, int height);

		[DllImport(LibInputName)]
		public extern static IntPtr libinput_event_get_pointer_event(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static ulong libinput_event_pointer_get_time_usec(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static double libinput_event_pointer_get_absolute_x_transformed(IntPtr ev, int width);

		[DllImport(LibInputName)]
		public extern static double libinput_event_pointer_get_absolute_y_transformed(IntPtr ev, int height);

		[DllImport(LibInputName)]
		public extern static double libinput_event_pointer_get_dx(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static double libinput_event_pointer_get_dy(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static libinput_event_code libinput_event_pointer_get_button(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static libinput_button_state libinput_event_pointer_get_button_state(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static int libinput_event_pointer_has_axis(IntPtr ev, libinput_pointer_axis axis);

		[DllImport(LibInputName)]
		public extern static double libinput_event_pointer_get_axis_value(IntPtr ev, libinput_pointer_axis axis);

		[DllImport(LibInputName)]
		public extern static libinput_pointer_axis_source libinput_event_pointer_get_axis_source(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static double libinput_event_pointer_get_axis_value_discrete(IntPtr ev, libinput_pointer_axis axis);

		[DllImport(LibInputName)]
		public extern static IntPtr libinput_event_get_keyboard_event(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static libinput_key libinput_event_keyboard_get_key(IntPtr ev);

		[DllImport(LibInputName)]
		public extern static libinput_key_state libinput_event_keyboard_get_key_state(IntPtr ev);
	}
}
