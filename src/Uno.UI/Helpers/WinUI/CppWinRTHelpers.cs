// TODO:MZ!!!


using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Uno.Disposables;
using Uno.UI.Xaml;
using Windows.Foundation;

namespace Uno.UI.Helpers.WinUI;

internal static class CppWinRTHelpers
{
	/// <remarks>
	/// Note: Although this is usually called as 'SetDefaultStyleKey(this)' (per WinUI C++ code), we actually only use the compile-time
	///  TDerived type and ignore the runtime derivedControl parameter, preserving the expected behaviour that DefaultStyleKey is 'fixed'
	/// under inheritance unless explicitly changed by an inheriting type.
	/// </remarks>
	internal static void SetDefaultStyleKey<TDerived>(this TDerived derivedControl) where TDerived : Control
	{
		derivedControl.SetDefaultStyleKeyInternal(typeof(TDerived));

		if (derivedControl is Control control)
		{
			Uri uri = new Uri(XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.WinUIThemeResourceURL);
			control.DefaultStyleResourceUri = uri;
		}
	}

	/// <summary>
	/// Default style key for controls of the Microsoft.UI.Xaml.Controls.Tabular binary (TableView and its parts),
	/// whose default styles live in that binary's own generic.xaml.
	/// </summary>
	/// <remarks>
	/// Port of SetDefaultStyleKeyWorker, controls\dev\dll-tabular\TabularControlsResources.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475.
	/// Like <see cref="SetDefaultStyleKey{TDerived}(TDerived)"/>, the key is the compile-time TDerived type.
	/// </remarks>
	internal static void SetTabularDefaultStyleKey<TDerived>(this TDerived derivedControl) where TDerived : Control
	{
		derivedControl.SetDefaultStyleKeyInternal(typeof(TDerived));

		// TABULAR_BINARY_EMITS_THEME_RESOURCES is defined for the Tabular binary (dll-tabular\Microsoft.UI.Xaml.Common.props).
		if (derivedControl is Control control)
		{
			const bool isPerf2026Enabled = false; // TODO: Decide based on opt-in flag, task.ms/60958581
			Uri uri = new Uri(isPerf2026Enabled
				? XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.TabularRootNamespace + "/Themes/generic_perf2026.xaml"
				: XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.TabularGenericURL);
			control.DefaultStyleResourceUri = uri;
		}
	}

	public static IDisposable RegisterXamlRootChanged(XamlRoot xamlRoot, TypedEventHandler<XamlRoot, XamlRootChangedEventArgs> handler)
	{
		xamlRoot.Changed += handler;
		return Disposable.Create(() => xamlRoot.Changed -= handler);
	}

	public static IDisposable RegisterPropertyChanged(DependencyObject dependencyObject, DependencyProperty dependencyProperty, DependencyPropertyChangedCallback callback)
	{
		var token = dependencyObject.RegisterPropertyChangedCallback(dependencyProperty, callback);
		return Disposable.Create(() => dependencyObject.UnregisterPropertyChangedCallback(dependencyProperty, token));
	}

	public static bool SetFocus(DependencyObject obj, FocusState focusState)
	{
		if (obj != null)
		{
			// Use TryFocusAsync if it's available.
			if (false) //TODO Uno specific: WinUI checks for TryFocusAsync method presence, which is not implemented yet in Uno (issue #4256)
			{
				//var result = FocusManager.TryFocusAsync(obj, focusState);
				//if (result.Status == AsyncStatus.Completed)
				//{
				//	return result.GetResults().Succeeded;
				//}
				//// Operation was async, let's assume it worked.
				//return true;
			}

			if (obj is Control control)
			{
				return control.Focus(focusState);
			}
			else if (obj is Hyperlink hyperlink)
			{
				return hyperlink.Focus(focusState);
			}
			else if (obj is WebView webview)
			{
				return webview.Focus(focusState);
			}
		}

		return false;
	}
}
