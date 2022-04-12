// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using windows = Windows;

namespace Microsoft.Toolkit.Wpf.UI.XamlHost
{
    /// <summary>
    /// Extensions for use with UWP UIElement objects wrapped by the UnoXamlHostBaseExt
    /// </summary>
    public static class UwpUIElementExtensions
    {
        private static bool IsDesktopWindowsXamlSourcePresent() => windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.UI.Xaml.Hosting.HostingContract", 3);

        private static windows.UI.Xaml.DependencyProperty WrapperProperty
        {
            get
            {
                if (IsDesktopWindowsXamlSourcePresent())
                {
                    var result = windows.UI.Xaml.DependencyProperty.RegisterAttached("Wrapper", typeof(System.Windows.UIElement), typeof(UwpUIElementExtensions), new windows.UI.Xaml.PropertyMetadata(null));
                    return result;
                }

                throw new NotImplementedException();
            }
        }

        public static UnoXamlHostBase GetWrapper(this windows.UI.Xaml.UIElement element)
        {
            if (IsDesktopWindowsXamlSourcePresent())
            {
                return (UnoXamlHostBase)element.GetValue(WrapperProperty);
            }

            return null;
        }

        public static void SetWrapper(this windows.UI.Xaml.UIElement element, UnoXamlHostBase wrapper)
        {
            if (IsDesktopWindowsXamlSourcePresent())
            {
                element.SetValue(WrapperProperty, wrapper);
            }
        }
    }
}
