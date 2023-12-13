// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\inc\FlyoutBase.h, tag winui3/release/1.4.3, commit 685d2bf

#nullable enable

using System;
using System.Collections.Generic;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml.Controls.Primitives;

partial class FlyoutBase
{
	internal enum MajorPlacementMode
	{
		Top,
        Bottom,
        Left,
        Right,
        Full
	}

	internal struct OnPlacementUpdatedSubscriber
	{
		public OnPlacementUpdatedSubscriber(ManagedWeakReference target, Action<DependencyObject, MajorPlacementMode> callback)
		{
			Target = target;
			Callback = callback;
		}

		public ManagedWeakReference Target { get; }
		
		public Action<DependencyObject, MajorPlacementMode> Callback { get; }
	}

	private readonly List<OnPlacementUpdatedSubscriber> m_placementUpdatedSubscribers = new();
}
