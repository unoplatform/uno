// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	partial class AppBar
	{
		#region IsSticky

		public bool IsSticky
		{
			get => (bool)GetValue(IsStickyProperty);
			set => SetValue(IsStickyProperty, value);
		}

		public static DependencyProperty IsStickyProperty { get; } =
			DependencyProperty.Register(
				nameof(IsSticky),
				typeof(bool),
				typeof(AppBar),
				new FrameworkPropertyMetadata(default(bool))
			);

		#endregion

		#region IsOpen

		public bool IsOpen
		{
			get => (bool)GetValue(IsOpenProperty);
			set => SetValue(IsOpenProperty, value);
		}

		public static DependencyProperty IsOpenProperty { get; } =
		DependencyProperty.Register(
			nameof(IsOpen),
			typeof(bool),
			typeof(AppBar),
			new FrameworkPropertyMetadata(default(bool))
		);

		#endregion

		#region ClosedDisplayMode

		public AppBarClosedDisplayMode ClosedDisplayMode
		{
			get => (AppBarClosedDisplayMode)GetValue(ClosedDisplayModeProperty);
			set => SetValue(ClosedDisplayModeProperty, value);
		}

		public static DependencyProperty ClosedDisplayModeProperty { get; } =
			DependencyProperty.Register(
				nameof(ClosedDisplayMode),
				typeof(AppBarClosedDisplayMode),
				typeof(AppBar),
				new FrameworkPropertyMetadata(AppBarClosedDisplayMode.Compact)
			);

		#endregion

		#region LightDismissOverlayMode

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get => (LightDismissOverlayMode)GetValue(LightDismissOverlayModeProperty);
			set => SetValue(LightDismissOverlayModeProperty, value);
		}

		public static DependencyProperty LightDismissOverlayModeProperty { get; } =
			DependencyProperty.Register(
				nameof(LightDismissOverlayMode),
				typeof(LightDismissOverlayMode),
				typeof(AppBar),
				new FrameworkPropertyMetadata(default(LightDismissOverlayMode))
			);

		#endregion

		#region TemplateSettings
		public AppBarTemplateSettings TemplateSettings
		{
			get => (AppBarTemplateSettings)GetValue(TemplateSettingsProperty);
			set => SetValue(TemplateSettingsProperty, value);
		}
		public static DependencyProperty TemplateSettingsProperty { get; } =
			DependencyProperty.Register(nameof(TemplateSettings), typeof(AppBarTemplateSettings), typeof(AppBar), new FrameworkPropertyMetadata(null));
		#endregion
	}
}
