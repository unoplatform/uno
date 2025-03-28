// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/CommunityToolkit/Microsoft.Toolkit.Win32/blob/master/Microsoft.Toolkit.Wpf.UI.XamlHost/UwpUIElementExtensions.cs

#nullable enable

using System;

namespace Uno.UI.XamlHost.Skia.Wpf
{
	/// <summary>
	/// Extensions for use with UWP UIElement objects wrapped by the UnoXamlHostBaseExt
	/// </summary>
	public static class UwpUIElementExtensions
	{
		private static global::Windows.UI.Xaml.DependencyProperty WrapperProperty =>
			 global::Windows.UI.Xaml.DependencyProperty.RegisterAttached("Wrapper", typeof(System.Windows.UIElement), typeof(UwpUIElementExtensions), new global::Windows.UI.Xaml.PropertyMetadata(null));

		public static UnoXamlHostBase? GetWrapper(this global::Windows.UI.Xaml.UIElement element) => (UnoXamlHostBase)element.GetValue(WrapperProperty);

		public static void SetWrapper(this global::Windows.UI.Xaml.UIElement element, UnoXamlHostBase? wrapper) => element.SetValue(WrapperProperty, wrapper);
	}
}
