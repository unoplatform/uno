// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	partial class CommandBar
	{
		public Style CommandBarOverflowPresenterStyle
		{
			get { return (Style)this.GetValue(CommandBarOverflowPresenterStyleProperty); }
			set { this.SetValue(CommandBarOverflowPresenterStyleProperty, value); }
		}
		public static DependencyProperty CommandBarOverflowPresenterStyleProperty { get; } =
			DependencyProperty.Register(nameof(CommandBarOverflowPresenterStyle), typeof(Style), typeof(CommandBar), new FrameworkPropertyMetadata(null));

		public CommandBarTemplateSettings CommandBarTemplateSettings
		{
			get { return (CommandBarTemplateSettings)this.GetValue(CommandBarTemplateSettingsProperty); }
			set { this.SetValue(CommandBarTemplateSettingsProperty, value); }
		}
		public static DependencyProperty CommandBarTemplateSettingsProperty { get; } =
			DependencyProperty.Register(nameof(CommandBarTemplateSettings), typeof(CommandBarTemplateSettings), typeof(CommandBar), new FrameworkPropertyMetadata(null));

		public CommandBarDefaultLabelPosition DefaultLabelPosition
		{
			get { return (CommandBarDefaultLabelPosition)this.GetValue(DefaultLabelPositionProperty); }
			set { this.SetValue(DefaultLabelPositionProperty, value); }
		}
		public static DependencyProperty DefaultLabelPositionProperty { get; } =
			DependencyProperty.Register(nameof(DefaultLabelPosition), typeof(CommandBarDefaultLabelPosition), typeof(CommandBar), new FrameworkPropertyMetadata(CommandBarDefaultLabelPosition.Bottom));

		public bool IsDynamicOverflowEnabled
		{
			get { return (bool)this.GetValue(IsDynamicOverflowEnabledProperty); }
			set { this.SetValue(IsDynamicOverflowEnabledProperty, value); }
		}
		public static DependencyProperty IsDynamicOverflowEnabledProperty { get; } =
			DependencyProperty.Register(nameof(IsDynamicOverflowEnabled), typeof(bool), typeof(CommandBar), new FrameworkPropertyMetadata(true));

		public CommandBarOverflowButtonVisibility OverflowButtonVisibility
		{
			get { return (CommandBarOverflowButtonVisibility)this.GetValue(OverflowButtonVisibilityProperty); }
			set { this.SetValue(OverflowButtonVisibilityProperty, value); }
		}
		public static DependencyProperty OverflowButtonVisibilityProperty { get; } =
			DependencyProperty.Register(nameof(OverflowButtonVisibility), typeof(CommandBarOverflowButtonVisibility), typeof(CommandBar), new FrameworkPropertyMetadata(CommandBarOverflowButtonVisibility.Auto));

		public IObservableVector<ICommandBarElement> PrimaryCommands
		{
			get { return (IObservableVector<ICommandBarElement>)this.GetValue(PrimaryCommandsProperty); }
			private set { this.SetValue(PrimaryCommandsProperty, value); }
		}
		public static DependencyProperty PrimaryCommandsProperty { get; } =
			DependencyProperty.Register(nameof(PrimaryCommands), typeof(IObservableVector<ICommandBarElement>), typeof(CommandBar), new FrameworkPropertyMetadata(default(IObservableVector<ICommandBarElement>), FrameworkPropertyMetadataOptions.ValueInheritsDataContext));

		public IObservableVector<ICommandBarElement> SecondaryCommands
		{
			get { return (IObservableVector<ICommandBarElement>)this.GetValue(SecondaryCommandsProperty); }
			private set { this.SetValue(SecondaryCommandsProperty, value); }
		}

		public static DependencyProperty SecondaryCommandsProperty { get; } =
			DependencyProperty.Register(nameof(SecondaryCommands), typeof(IObservableVector<ICommandBarElement>), typeof(CommandBar), new FrameworkPropertyMetadata(default(IObservableVector<ICommandBarElement>), FrameworkPropertyMetadataOptions.ValueInheritsDataContext));
	}
}
