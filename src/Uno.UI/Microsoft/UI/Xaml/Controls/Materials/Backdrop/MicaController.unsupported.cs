// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference MicaController.cpp, commit b2aab7e

#nullable enable

#if !__ANDROID__
namespace Microsoft.UI.Xaml.Controls;

public partial class MicaController
{
	internal bool SetTarget(Windows.UI.Xaml.Window xamlWindow)
	{
		// Uno specific: Actual Mica is not yet supported on any target.
		return false;
	}
}
#endif
