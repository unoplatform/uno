// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FocusedElementRemovedEventArgs.h, FocusedElementRemovedEventArgs.cpp

#nullable enable

using System;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Input
{
	internal class FocusedElementRemovedEventArgs : EventArgs
	{
		public FocusedElementRemovedEventArgs(DependencyObject? focusedElement, DependencyObject? currentNextFocusableElement)
		{
			OldFocusedElement = focusedElement;
			NewFocusedElement = currentNextFocusableElement;
		}

		public DependencyObject? OldFocusedElement { get; }

		public DependencyObject? NewFocusedElement { get; set; }
	}
}
