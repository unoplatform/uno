// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ContentRoot.h

namespace Uno.UI.Xaml.Core;

internal enum ContentRootType
{
	CoreWindow, // A window created using the "native UI framework" (e.g. iOS-UIKit / Android-View / Windows-WPF)
	XamlIslandRoot, // Xaml Island hosted in an application which uses another UI framework (e.g. WPF / WinUI)
	ShellWindow, // A window created using system OS APIs (e.g. Win32 / X11)
}
