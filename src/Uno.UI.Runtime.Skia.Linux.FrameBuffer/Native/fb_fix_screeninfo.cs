// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Base interactions with libinput derived from https://github.com/AvaloniaUI/Avalonia

using System;
using System.Runtime.InteropServices;
using __u16 = System.UInt16;
using __u32 = System.UInt32;

namespace Uno.UI.Runtime.Skia.Native
{
	[StructLayout(LayoutKind.Sequential)]
	unsafe struct fb_fix_screeninfo
	{
		public fixed byte id[16];	/* identification string eg "TT Builtin" */

		public IntPtr smem_start;	/* Start of frame buffer mem */

		/* (physical address) */
		public __u32 smem_len;		/* Length of frame buffer mem */

		public __u32 type;			/* see FB_TYPE_*		*/
		public __u32 type_aux;		/* Interleave for interleaved Planes */
		public __u32 visual;		/* see FB_VISUAL_*		*/
		public __u16 xpanstep;		/* zero if no hardware panning  */
		public __u16 ypanstep;		/* zero if no hardware panning  */
		public __u16 ywrapstep;		/* zero if no hardware ywrap    */
		public __u32 line_length;	/* length of a line in bytes    */

		public IntPtr mmio_start;	/* Start of Memory Mapped I/O   */

		/* (physical address) */
		public __u32 mmio_len;		/* Length of Memory Mapped I/O  */

		public __u32 accel;			/* Type of acceleration available */
		public fixed __u16 reserved[3]; /* Reserved for future compatibility */
	}
}
