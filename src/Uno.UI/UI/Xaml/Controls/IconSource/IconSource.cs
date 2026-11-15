// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference IconSource_Partial.cpp

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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

	public IconElement CreateIconElement()
	{
		IconElement element;
		// IconSourceGenerated::CreateIconElement dispatches to the virtual CreateIconElementCore.
		element = CreateIconElementCore();

		if (element != null)
		{
			Brush foreground = Foreground;

			// This check ensures that we are not forcing developers to provide a foreground, if no foreground is set, it picks the default foreground based on theme.
			if (foreground != null)
			{
				element.Foreground = foreground;
			}
		}

		WeakReference<IconElement> weakElement;
		weakElement = new WeakReference<IconElement>(element);
		m_createdIconElements.Add(weakElement);

		return element;
	}

	protected virtual IconElement CreateIconElementCore()
	{
		throw new NotImplementedException("This is an abstract base type, so this should never be called."); // return E_NOTIMPL;
	}

	protected virtual DependencyProperty GetIconElementPropertyCore(DependencyProperty iconSourceProperty)
	{
		// ctl::ComPtr<DependencyPropertyHandle> iconSourcePropertyHandle;
		// IFC_RETURN(ctl::do_query_interface(iconSourcePropertyHandle, iconSourceProperty));

		// switch (iconSourcePropertyHandle->GetDP()->GetIndex())
		// {
		// 	case KnownPropertyIndex::IconSource_Foreground:
		// 		IFC_RETURN(MetadataAPI::GetIDependencyProperty(KnownPropertyIndex::IconElement_Foreground, returnValue));
		// 		break;
		// 	default:
		// 		*returnValue = nullptr;
		// }

		// return S_OK;

		if (iconSourceProperty == ForegroundProperty)
		{
			return IconElement.ForegroundProperty;
		}

		return null;
	}

	// TODO Uno specific: WinUI overrides OnPropertyChanged2 for every property, Uno routes each
	// IconSource property's changed callback here instead.
	internal static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var iconSource = sender as IconSource;
		iconSource?.OnPropertyChanged(args);
	}

	// _Check_return_ HRESULT IconSource::OnPropertyChanged2(_In_ const PropertyChangedParams& args)
	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (GetIconElementPropertyCore(args.Property) is { } iconElementProperty)
		{
			m_createdIconElements.RemoveAll(
				weakElement =>
				{
					if (weakElement.TryGetTarget(out var target))
					{
						target.SetValue(iconElementProperty, args.NewValue);
						return false;
					}
					return true;
				});
		}
	}
}
