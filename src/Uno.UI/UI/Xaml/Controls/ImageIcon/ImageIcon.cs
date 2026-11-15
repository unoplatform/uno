// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ImageIcon.cpp

#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

public partial class ImageIcon : IconElement
{
	private Image? m_rootImage = null;

	public ImageIcon()
	{
		// TODO Uno: RuntimeProfiler marker (__RP_Marker_ClassById) not ported.
	}

	protected override void OnApplyTemplate()
	{
		// TODO Uno specific: WinAppSDK containment flags don't exist in Uno, only the enabled
		// (default) branch of WINAPPSDK_CHANGEID_62784965 is ported.
		var firstChild = VisualTreeHelper.GetChild(this, 0);

		// The first child may be a Grid (old behavior) or directly an Image (new behavior).
		if (firstChild is Grid grid)
		{
			var image = (Image)VisualTreeHelper.GetChild(grid, 0);
			image.Source = Source;
			m_rootImage = image;
		}
		else if (firstChild is Image image)
		{
			image.Source = Source;
			m_rootImage = image;
		}
		else
		{
			m_rootImage = null;
		}
	}

	private void OnSourcePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (m_rootImage is { } image)
		{
			image.Source = Source;
		}
	}
}
