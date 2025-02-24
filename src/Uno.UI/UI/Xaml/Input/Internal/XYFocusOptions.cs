// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// XYFocus.h

#nullable enable

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input
{
	internal struct XYFocusOptions
	{
		public static XYFocusOptions Default => new XYFocusOptions()
		{
			IgnoreClipping = true,
			ConsiderEngagement = true,
			UpdateManifold = true,
		};

		internal DependencyObject? SearchRoot { get; set; }

		internal Rect? ExclusionRect { get; set; }

		internal Rect FocusedElementBounds { get; set; }

		internal Rect? FocusHintRectangle { get; set; }

		internal bool IgnoreClipping { get; set; }

		internal bool IgnoreCone { get; set; }

		internal bool ShouldConsiderXYFocusKeyboardNavigation { get; set; }

		internal bool ConsiderEngagement { get; set; }

		internal bool UpdateManifold { get; set; }

		internal XYFocusNavigationStrategyOverride NavigationStrategyOverride { get; set; }

		internal bool UpdateManifoldsFromFocusHintRectangle { get; set; }

		internal bool IgnoreOcclusivity { get; set; }

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + (SearchRoot?.GetHashCode() ?? 0);
			hash = hash * 23 + NavigationStrategyOverride.GetHashCode();
			hash = hash * 23 + IgnoreClipping.GetHashCode();
			hash = hash * 23 + IgnoreCone.GetHashCode();
			hash = hash * 23 + (ExclusionRect?.GetHashCode() ?? 0);
			hash = hash * 23 + (FocusHintRectangle?.GetHashCode() ?? 0);
			hash = hash * 23 + IgnoreOcclusivity.GetHashCode();
			hash = hash * 23 + UpdateManifoldsFromFocusHintRectangle.GetHashCode();
			return hash;
		}
	}
}
