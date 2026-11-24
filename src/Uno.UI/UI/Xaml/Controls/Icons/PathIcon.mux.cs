// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference icon.cpp

#nullable enable

using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls;

partial class PathIcon
{
	// TODO Uno: NOT PORTED - CPathIcon::Create, the type converter building a PathIcon from a path markup string.

	private protected override void ApplyTemplate(out bool fAddedVisuals)
	{
		fAddedVisuals = false;

		if (GetChildElementNoRef() is null)
		{
			// Create a new Path
			var pPath = new Path();
			AddChildElement(pPath);

			// Set the Path properties that aren't linked to our properties
			pPath.HorizontalAlignment = HorizontalAlignment.Stretch;
			pPath.VerticalAlignment = VerticalAlignment.Stretch;

			// Set the Path's Fill property to match our Foreground property
			UpdateLinkedChildProperty(ForegroundProperty, Shape.FillProperty);

			// Set the Path's Data property to match ours
			UpdateLinkedChildProperty(DataProperty, Path.DataProperty);

			fAddedVisuals = true;
		}
	}

	private protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e) =>
		UpdateLinkedChildProperty(ForegroundProperty, Shape.FillProperty);
}
