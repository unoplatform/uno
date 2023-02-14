// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference InfoBadge.properties.cpp, commit 76bd573

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class InfoBadge
	{
		public int Value
		{
			get => (int)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public static DependencyProperty ValueProperty { get; } =
			DependencyProperty.Register(nameof(Value), typeof(int), typeof(InfoBadge), new FrameworkPropertyMetadata(-1, OnPropertyChanged));

		public InfoBadgeTemplateSettings TemplateSettings
		{
			get => (InfoBadgeTemplateSettings)GetValue(TemplateSettingsProperty);
			set => SetValue(TemplateSettingsProperty, value);
		}

		public static DependencyProperty TemplateSettingsProperty { get; } =
			DependencyProperty.Register(nameof(TemplateSettings), typeof(InfoBadgeTemplateSettings), typeof(InfoBadge), new FrameworkPropertyMetadata(null, OnPropertyChanged));

		public IconSource IconSource
		{
			get => (IconSource)GetValue(IconSourceProperty);
			set => SetValue(IconSourceProperty, value);
		}

		public static DependencyProperty IconSourceProperty { get; } =
			DependencyProperty.Register(nameof(IconSource), typeof(IconSource), typeof(InfoBadge), new FrameworkPropertyMetadata(null, OnPropertyChanged));

		private static void OnPropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = (InfoBadge)sender;
			owner.OnPropertyChanged(args);
		}
	}
}
