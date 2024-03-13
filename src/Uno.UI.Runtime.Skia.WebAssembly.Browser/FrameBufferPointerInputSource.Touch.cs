// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// See the LICENSE file in the project root for more information.
//
// Base interactions with libinput derived from https://github.com/AvaloniaUI/Avalonia

#nullable enable

using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Uno.Foundation.Logging;
using System.Collections.Generic;

namespace Uno.UI.Runtime.Skia;

unsafe internal partial class BrowserPointerInputSource
{
	private readonly Dictionary<uint, Point> _activePointers = new();

}
