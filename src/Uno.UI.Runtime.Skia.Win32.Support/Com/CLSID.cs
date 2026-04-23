// The MIT License (MIT)

// Copyright (c) .NET Foundation and Contributors
//
// All rights reserved.
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Windows.Win32.System.Com;

internal static class CLSID
{
	// 00BB2763-6A77-11D0-A535-00C04FD7D062
	internal static Guid AutoComplete { get; } = new(0x00BB2763, 0x6A77, 0x11D0, 0xA5, 0x35, 0x00, 0xC0, 0x4F, 0xD7, 0xD0, 0x62);

	// 4657278A-411B-11D2-839A-00C04FD918D0
	internal static Guid DragDropHelper { get; } = new(0x4657278A, 0x411B, 0x11D2, 0x83, 0x9A, 0x0, 0xC0, 0x4F, 0xD9, 0x18, 0xD0);

	// C0B4E2F3-BA21-4773-8DBA-335EC946EB8B
	internal static Guid FileSaveDialog { get; } = new(0xC0B4E2F3, 0xBA21, 0x4773, 0x8D, 0xBA, 0x33, 0x5E, 0xC9, 0x46, 0xEB, 0x8B);

	// DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7
	internal static Guid FileOpenDialog { get; } = new(0xDC1C5A9C, 0xE88A, 0x4DDE, 0xA5, 0xA1, 0x60, 0xF8, 0x2A, 0x20, 0xAE, 0xF7);

	// 00000323-0000-0000-c000-000000000046
	internal static Guid StdGlobalInterfaceTable { get; } = new(0x00000323, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

	// 56FDF344-FD6D-11D0-958A-006097C9A090
	internal static Guid TaskbarList { get; } = new(0x56FDF344, 0xFD6D, 0x11D0, 0x95, 0x8A, 0x00, 0x60, 0x97, 0xC9, 0xA0, 0x90);
}
