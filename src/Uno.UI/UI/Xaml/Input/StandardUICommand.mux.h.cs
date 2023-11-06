// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference StandardUICommand_Partial.h, tag winui3/release/1.4.2

using Windows.System;

namespace Windows.UI.Xaml.Input;

partial class StandardUICommand
{
	private bool m_ownsLabel = true;
	private bool m_ownsIconSource = true;
	private bool m_ownsKeyboardAccelerator = true;
	private bool m_ownsDescription = true;

	private VirtualKey m_previousAcceleratorKey;
	private VirtualKeyModifiers m_previousAcceleratorModifiers;
	private bool m_settingPropertyInternally;
}
