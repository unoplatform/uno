// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable 105 // Disabled until the tree is migrate to WinUI

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;
using Windows.UI;
using System.Windows.Input;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using IconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.IconSource;
using FontIconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.FontIconSource;
using BitmapIconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.BitmapIconSource;
using SymbolIconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SymbolIconSource;
using PathIconSource = Microsoft/* UWP don't rename */.UI.Xaml.Controls.PathIconSource;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls.AnimatedVisuals;
using ColorChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ColorChangedEventArgs;
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.InfoBadgeTests
{
	[Sample("Info", "MUX")]
	public sealed partial class InfoBadgePage : Page
	{
		IconSource pageIconSource = null;
		SolidColorBrush iconForegroundBrush = new SolidColorBrush();

		public InfoBadgePage()
		{
			this.InitializeComponent();
			iconForegroundBrush.Color = IconForegroundColorPicker.Color;
		}

		public void IconComboBoxSelectionChanged(object sender, SelectionChangedEventArgs args)
		{
			if (ReferenceEquals(IconComboBox.SelectedItem, this.NullIcon))
			{
				UpdateIcon(null);
			}
			else if (ReferenceEquals(IconComboBox.SelectedItem, this.FontIcon))
			{
				UpdateIcon(MakeFontIcon());
			}
			else if (ReferenceEquals(IconComboBox.SelectedItem, this.BitmapIcon))
			{
				UpdateIcon(MakeBitmapIcon());
			}
			else if (ReferenceEquals(IconComboBox.SelectedItem, this.ImageIcon))
			{
				UpdateIcon(MakeImageIcon());
			}
			else if (ReferenceEquals(IconComboBox.SelectedItem, this.SymbolIcon))
			{
				UpdateIcon(MakeSymbolIcon());
			}
			else if (ReferenceEquals(IconComboBox.SelectedItem, this.PathIcon))
			{
				UpdateIcon(MakePathIcon());
			}
			else if (ReferenceEquals(IconComboBox.SelectedItem, this.AnimatedIcon))
			{
				UpdateIcon(MakeAnimatedIcon());
			}
		}

		public void OnIconForegroundColorPickerColorChanged(object sender, ColorChangedEventArgs args)
		{
			iconForegroundBrush.Color = args.NewColor;
			if (pageIconSource != null)
			{
				pageIconSource.Foreground = iconForegroundBrush;
			}
		}

		public void OnForegroundToNullButtonClicked(object sender, RoutedEventArgs args)
		{
			if (pageIconSource != null)
			{
				pageIconSource.Foreground = null;
			}
		}

		public void OnClearForegroundButtonClicked(object sender, RoutedEventArgs args)
		{
			if (pageIconSource != null)
			{
				pageIconSource.ClearValue(IconSource.ForegroundProperty);
			}
		}

		private void UpdateIcon(IconSource iconSource)
		{
			pageIconSource = iconSource;
			DynamicInfoBadge.IconSource = iconSource;
		}

		private FontIconSource MakeFontIcon()
		{
			FontIconSource iconSource = new FontIconSource();
			iconSource.Glyph = "99+";
			iconSource.FontFamily = new FontFamily("XamlAutoFontFamily");
			iconSource.Foreground = iconForegroundBrush;
			return iconSource;
		}

		private BitmapIconSource MakeBitmapIcon()
		{
			BitmapIconSource iconSource = new BitmapIconSource();
			iconSource.ShowAsMonochrome = false;
			Uri uri = new Uri("ms-appx:/Assets/ingredient1.png");
			iconSource.UriSource = uri;
			iconSource.Foreground = iconForegroundBrush;
			return iconSource;
		}

		private ImageIconSource MakeImageIcon()
		{
			ImageIconSource iconSource = new ImageIconSource();
			var uri = new Uri("https://raw.githubusercontent.com/DiemenDesign/LibreICONS/master/svg-color/libre-camera-panorama.svg");
			iconSource.ImageSource = new SvgImageSource(uri);
			iconSource.Foreground = iconForegroundBrush;
			return iconSource;
		}

		private SymbolIconSource MakeSymbolIcon()
		{
			SymbolIconSource iconSource = new SymbolIconSource();
			iconSource.Symbol = Symbol.Setting;
			iconSource.Foreground = iconForegroundBrush;
			return iconSource;
		}

		private PathIconSource MakePathIcon()
		{
			PathIconSource iconSource = new PathIconSource();
			var geometry = new RectangleGeometry();
			geometry.Rect = new Windows.Foundation.Rect { Width = 5, Height = 2, X = 0, Y = 0 };
			iconSource.Data = geometry;
			iconSource.Foreground = iconForegroundBrush;
			return iconSource;
		}

		private AnimatedIconSource MakeAnimatedIcon()
		{
			AnimatedIconSource iconSource = new AnimatedIconSource();
			iconSource.Source = new AnimatedSettingsVisualSource();
			iconSource.FallbackIconSource = new SymbolIconSource { Symbol = Symbol.Setting, Foreground = iconForegroundBrush };
			iconSource.Foreground = iconForegroundBrush;
			return iconSource;
		}
	}
}
