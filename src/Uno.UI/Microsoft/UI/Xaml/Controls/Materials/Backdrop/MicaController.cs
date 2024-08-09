// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference MicaController.cpp, commit b2aab7e

#nullable enable

using Windows.UI;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	internal class MicaController
	{
		internal static readonly Color DarkThemeColor = Color.FromArgb(255, 32, 32, 32);
		internal const float DarkThemeTintOpacity = 0.8f;

		internal static readonly Color LightThemeColor = Color.FromArgb(255, 243, 243, 243);
		internal const float LightThemeTintOpacity = 0.5f;

		internal bool SetTarget(Windows.UI.Xaml.Window? xamlWindow)
		{
			// Uno specific: Actual Mica is not yet supported on any target.
			return false;
		}
	}
}
