// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Base interactions with libinput derived from https://github.com/AvaloniaUI/Avalonia

namespace Uno.UI.Runtime.Skia.Native
{
	enum FbIoCtl : uint
	{
		FBIOGET_VSCREENINFO = 0x4600,
		FBIOPUT_VSCREENINFO = 0x4601,
		FBIOGET_FSCREENINFO = 0x4602,
		FBIO_WAITFORVSYNC = 0x40044620,
	}
}
