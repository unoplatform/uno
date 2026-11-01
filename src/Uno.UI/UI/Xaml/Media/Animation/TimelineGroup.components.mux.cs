// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference components/animation/TimelineGroup.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Storyboard
{
	internal override void InitializeIteration()
	{
		foreach (var item in Children.Items)
		{
			item.InitializeIteration();
		}

		base.InitializeIteration();
	}
}
