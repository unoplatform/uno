#nullable disable

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SymbolIconSource.cpp, commit 083796a

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class SymbolIconSource : IconSource
{
	public Symbol Symbol
	{
		get => (Symbol)GetValue(SymbolProperty);
		set => SetValue(SymbolProperty, value);
	}

	public static DependencyProperty SymbolProperty { get; } =
		DependencyProperty.Register(nameof(Symbol), typeof(Symbol), typeof(SymbolIconSource), new FrameworkPropertyMetadata(Symbol.Emoji, OnPropertyChanged));

#if !HAS_UNO_WINUI
	private
#endif
	protected override IconElement CreateIconElementCore()
	{
		var symbolIcon = new SymbolIcon()
		{
			Symbol = Symbol
		};

		if (Foreground != null)
		{
			symbolIcon.Foreground = Foreground;
		}

		return symbolIcon;
	}

#if !HAS_UNO_WINUI
	private
#endif
	protected override DependencyProperty GetIconElementPropertyCore(DependencyProperty iconSourceProperty)
	{
		if (iconSourceProperty == SymbolProperty)
		{
			return SymbolIcon.SymbolProperty;
		}

		return base.GetIconElementPropertyCore(iconSourceProperty);
	}
}
