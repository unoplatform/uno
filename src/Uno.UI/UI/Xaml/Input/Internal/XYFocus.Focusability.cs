// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Focusability.h

#nullable enable

using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Input
{
	internal static class XYFocusFocusability
	{
		internal static bool IsValidCandidate(DependencyObject element)
		{
			bool isFocusable = FocusProperties.IsFocusable(element);
			bool isGamepadFocusCandidate = FocusProperties.IsGamepadFocusCandidate(element);
			//TODO Uno: RootScrollViewer is not available in Uno yet
			//bool isRootScrollViewer = element is RootScrollViewer;
			bool isValidTabStop = FocusProperties.IsPotentialTabStop(element);

			return isFocusable &&
				isGamepadFocusCandidate &&
				//!isRootScrollViewer &&
				isValidTabStop;
		}
	}
}
