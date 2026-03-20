// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Base interactions with libinput derived from https://github.com/AvaloniaUI/Avalonia

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Uno.UI.Runtime.Skia.Native
{
	unsafe class Libc
	{
		public const int O_RDWR = 0x0002;

		public const int PROT_READ = 0x1;
		public const int PROT_WRITE = 0x2;

		public const int MAP_SHARED = 0x01;

		[DllImport("libc", SetLastError = true)]
		public static extern int open(string pathname, int flags, int mode);

		[DllImport("libc", SetLastError = true)]
		public static extern int close(int fd);

		[DllImport("libc", CharSet = CharSet.Ansi)]
		public static extern IntPtr strerror(int errno);

		[DllImport("libc", SetLastError = true)]
		public static extern int ioctl(int fd, FbIoCtl code, void* arg);

		[DllImport("libc", SetLastError = true)]
		public static extern IntPtr mmap(IntPtr addr, IntPtr length, int prot, int flags, int fd, IntPtr offset);

		[DllImport("libc", SetLastError = true)]
		public static extern int munmap(IntPtr addr, IntPtr length);

		[DllImport("libc", SetLastError = true)]
		public static extern int memcpy(IntPtr dest, IntPtr src, IntPtr length);

		[DllImport("libc", SetLastError = true)]
		public static extern int select(int nfds, void* rfds, void* wfds, void* exfds, IntPtr* timevals);

		[DllImport("libc", SetLastError = true)]
		public static extern int poll(pollfd* fds, IntPtr nfds, int timeout);
	}
}
