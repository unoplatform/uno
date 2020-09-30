// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Base interactions with libinput derived from https://github.com/AvaloniaUI/Avalonia

namespace Uno.UI.Runtime.Skia.Native
{
	enum libinput_event_code : int
	{
		BTN_LEFT = 0x110,
		BTN_RIGHT = 0x111,
		BTN_MIDDLE = 0x112,
		BTN_TOUCH = 0x14a
	}
}
