// The MIT License (MIT)
//
// Copyright (c) .NET Foundation and Contributors
// All Rights Reserved
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;

namespace Avalonia.X11
{
	[Flags]
	public enum XEventMask : int
	{
		NoEventMask = 0,
		KeyPressMask = (1 << 0),
		KeyReleaseMask = (1 << 1),
		ButtonPressMask = (1 << 2),
		ButtonReleaseMask = (1 << 3),
		EnterWindowMask = (1 << 4),
		LeaveWindowMask = (1 << 5),
		PointerMotionMask = (1 << 6),
		PointerMotionHintMask = (1 << 7),
		Button1MotionMask = (1 << 8),
		Button2MotionMask = (1 << 9),
		Button3MotionMask = (1 << 10),
		Button4MotionMask = (1 << 11),
		Button5MotionMask = (1 << 12),
		ButtonMotionMask = (1 << 13),
		KeymapStateMask = (1 << 14),
		ExposureMask = (1 << 15),
		VisibilityChangeMask = (1 << 16),
		StructureNotifyMask = (1 << 17),
		ResizeRedirectMask = (1 << 18),
		SubstructureNotifyMask = (1 << 19),
		SubstructureRedirectMask = (1 << 20),
		FocusChangeMask = (1 << 21),
		PropertyChangeMask = (1 << 22),
		ColormapChangeMask = (1 << 23),
		OwnerGrabButtonMask = (1 << 24)
	}

	[Flags]
	public enum XModifierMask
	{
		ShiftMask = (1 << 0),
		LockMask = (1 << 1),
		ControlMask = (1 << 2),
		Mod1Mask = (1 << 3),
		Mod2Mask = (1 << 4),
		Mod3Mask = (1 << 5),
		Mod4Mask = (1 << 6),
		Mod5Mask = (1 << 7),
		Button1Mask = (1 << 8),
		Button2Mask = (1 << 9),
		Button3Mask = (1 << 10),
		Button4Mask = (1 << 11),
		Button5Mask = (1 << 12),
		AnyModifier = (1 << 15)

	}

	[Flags]
	internal enum XCreateWindowFlags
	{
		CWBackPixmap = (1 << 0),
		CWBackPixel = (1 << 1),
		CWBorderPixmap = (1 << 2),
		CWBorderPixel = (1 << 3),
		CWBitGravity = (1 << 4),
		CWWinGravity = (1 << 5),
		CWBackingStore = (1 << 6),
		CWBackingPlanes = (1 << 7),
		CWBackingPixel = (1 << 8),
		CWOverrideRedirect = (1 << 9),
		CWSaveUnder = (1 << 10),
		CWEventMask = (1 << 11),
		CWDontPropagate = (1 << 12),
		CWColormap = (1 << 13),
		CWCursor = (1 << 14),
	}
}
