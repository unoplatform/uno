// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\core\elements\XamlIslandRootCollection.cpp, tag winui3/release/1.4.3, commit 685d2bfa86d6169aa1998a7eaa2c38bfcf9f74bc

#nullable enable

using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using XamlIslandRoot = Uno.UI.Xaml.Islands.XamlIsland;

namespace Uno.UI.Xaml.Islands;

internal partial class XamlIslandRootCollection : Panel
{
	protected override Size MeasureOverride(Size availableSize)
	{
		var desiredSize = new Size(0, 0);

		var collection = GetChildren();
		if (collection != null)
		{
			foreach (var child in collection)
			{
				var island = child as XamlIslandRoot;
				if (island != null)
				{
					if (island.IsMeasureDirty || island.IsMeasureDirtyPath)
					{
						var islandSize = island.GetSize();
						island.Measure(new Size(islandSize.Width, islandSize.Height));
						desiredSize.Width = Math.Max(desiredSize.Width, islandSize.Width);
						desiredSize.Height = Math.Max(desiredSize.Height, islandSize.Height);
					}
				}
			}
		}

		return desiredSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var collection = GetChildren();

		if (collection != null)
		{
			foreach (var child in collection)
			{
				var island = child as XamlIslandRoot;
				if (island != null)
				{
					if (island.IsArrangeDirty || island.IsArrangeDirtyPath)
					{
						// We arrange to the island's given size, its top left always at 0,0.
						var islandSize = island.GetSize();
						island.Arrange(new Rect(0.0f, 0.0f, islandSize.Width, islandSize.Height));
					}
				}
			}
		}

		return finalSize;
	}
}
