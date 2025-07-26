// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference IconSource_Partial.cpp, tag winui3/release/1.4.2

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
		// Uno Specific: we don't have IsComposed()
		// if (IsComposed())
		// {
		// 	IFC(ctl::do_query_interface(pVirtuals, this));
		//
		// 	// SYNC_CALL_TO_APP DIRECT - This next line may directly call out to app code.
		// 	IFC(pVirtuals->CreateIconElementCore(ppReturnValue));
		// }
		// else
		// {
		// 	IFC(CreateIconElementCore(ppReturnValue));
		// }
		element = CreateIconElementCore();

		if (element != null
			// Uno Specific: WinUI doesn't check if Foreground is null, it sets it anyway
			&& Foreground != null)
		{
			Brush foreground = Foreground;
			element.Foreground = foreground;
		}

		WeakReference<IconElement> weakElement;
		weakElement = new WeakReference<IconElement>(element);
		m_createdIconElements.Add(weakElement);

		return element;
	}

	// Note: Both CreateIconElementCore and GetIconElementPropertyCore are 'protected' only on WinUI 3
	// https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.iconsource.createiconelementcore?view=windows-app-sdk-1.3
	// https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.iconsource.geticonelementpropertycore?view=windows-app-sdk-1.3
	// We make them private protected for UWP build.
#if !HAS_UNO_WINUI
	private
#endif
	protected virtual IconElement CreateIconElementCore()
	{
		throw new NotImplementedException("This is an abstract base type, so this should never be called."); // return E_NOTIMPL;
	}

#if !HAS_UNO_WINUI
	private
#endif
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
