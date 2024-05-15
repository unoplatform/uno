// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Base interactions with libinput derived from https://github.com/AvaloniaUI/Avalonia

using System.Runtime.InteropServices;

namespace Uno.UI.Runtime.Skia.Native
{
#pragma warning disable CS8981 // The type name 'pollfd' only contains lower-cased ascii characters. Such names may become reserved for the language.
	[StructLayout(LayoutKind.Sequential)]
	struct pollfd
#pragma warning restore CS8981 // The type name 'pollfd' only contains lower-cased ascii characters. Such names may become reserved for the language.
	{
		public int fd;         /* file descriptor */
		public short events;     /* requested events */
		public short revents;    /* returned events */
	};
}
