// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;

namespace DirectUI
{
	internal enum ElementBrushProperty
	{
		Fill,
		Stroke
	};

	// Parameters for getting a WUC brush for the render walk
	internal struct BrushParams
	{
		// Identifies SolidColorBrush usage. Used to pick the correct transitioning brush.
		public UIElement m_element;
		public ElementBrushProperty m_brushProperty;
	};
}
