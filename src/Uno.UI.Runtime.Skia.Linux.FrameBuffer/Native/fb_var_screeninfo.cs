// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Base interactions with libinput derived from https://github.com/AvaloniaUI/Avalonia

using System.Runtime.InteropServices;
using __u32 = System.UInt32;

namespace Uno.UI.Runtime.Skia.Native
{
	[StructLayout(LayoutKind.Sequential)]
	unsafe struct fb_var_screeninfo
	{
		public __u32 xres;				/* visible resolution		*/
		public __u32 yres;
		public __u32 xres_virtual;		/* virtual resolution		*/
		public __u32 yres_virtual;
		public __u32 xoffset;			/* offset from virtual to visible */
		public __u32 yoffset;			/* resolution			*/

		public __u32 bits_per_pixel;	/* guess what			*/
		public __u32 grayscale;			/* 0 = color, 1 = grayscale,    */
										/* >1 = FOURCC          */

		public fb_bitfield red;			/* bitfield in fb mem if true color, */
		public fb_bitfield green;		/* else only length is significant */
		public fb_bitfield blue;
		public fb_bitfield transp;		/* transparency			*/

		public __u32 nonstd;			/* != 0 Non standard pixel format */

		public __u32 activate;			/* see FB_ACTIVATE_*		*/

		public __u32 height;			/* height of picture in mm    */
		public __u32 width;				/* width of picture in mm     */

		public __u32 accel_flags;		/* acceleration flags (hints)	*/

		/* Timing: All values in pixclocks, except pixclock (of course) */
		public __u32 pixclock;			/* pixel clock in ps (pico seconds) */

		public __u32 left_margin;		/* time from sync to picture	*/
		public __u32 right_margin;		/* time from picture to sync	*/
		public __u32 upper_margin;		/* time from sync to picture	*/
		public __u32 lower_margin;
		public __u32 hsync_len;			/* length of horizontal sync	*/
		public __u32 vsync_len;			/* length of vertical sync	*/
		public __u32 sync;				/* see FB_SYNC_*		*/
		public __u32 vmode;				/* see FB_VMODE_*		*/
		public fixed __u32 reserved[6]; /* Reserved for future compatibility */
	}
}
