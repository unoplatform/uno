// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IconSource.cpp, commit 083796a

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

public partial class IconSource : DependencyObject
{
	private List<WeakReference<IconElement>> m_createdIconElements = new List<WeakReference<IconElement>>();

	protected IconSource()
	{
	}

	public Brush Foreground
	{
		get => (Brush)GetValue(ForegroundProperty);
		set => SetValue(ForegroundProperty, value);
	}

	public static DependencyProperty ForegroundProperty { get; } =
		DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(IconSource), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	public IconElement? CreateIconElement()
	{
		var element = CreateIconElementCore();
		if (element != null)
		{
			m_createdIconElements.Add(new WeakReference<IconElement>(element));
		}
		return element;
	}

	internal static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var iconSource = sender as IconSource;
		iconSource?.OnPropertyChanged(args);
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (GetIconElementPropertyCore(args.Property) is { } iconProp)
		{
			m_createdIconElements.RemoveAll(
				weakElement =>
				{
					if (weakElement.TryGetTarget(out var target))
					{
						target.SetValue(iconProp, args.NewValue);
						return false;
					}
					return true;
				});
		}
	}

	// Note: Both CreateIconElementCore and GetIconElementPropertyCore are 'protected' only on WinUI 3
	// https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.iconsource.createiconelementcore?view=windows-app-sdk-1.3
	// https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.iconsource.geticonelementpropertycore?view=windows-app-sdk-1.3
	// We make them private protected for UWP build.
#if !HAS_UNO_WINUI
	private
#endif
	protected virtual IconElement? CreateIconElementCore() => default;

#if !HAS_UNO_WINUI
	private
#endif
	protected virtual DependencyProperty? GetIconElementPropertyCore(DependencyProperty iconSourceProperty)
	{
		if (iconSourceProperty == ForegroundProperty)
		{
			return IconElement.ForegroundProperty;
		}

		return null;
	}
}
