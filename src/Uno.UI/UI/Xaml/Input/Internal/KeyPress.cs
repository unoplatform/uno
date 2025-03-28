// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FocusPressState.cpp

using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Input
{
	internal static class KeyPress
	{
		internal static void StartFocusPress(UIElement focused) =>
			VisualTree.GetFocusManagerForElement(focused).OnFocusedElementKeyPressed();

		internal static void EndFocusPress(UIElement focused) =>
			VisualTree.GetFocusManagerForElement(focused).OnFocusedElementKeyReleased();
	}
}
