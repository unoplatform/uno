// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/CommunityToolkit/Microsoft.Toolkit.Win32/blob/master/Microsoft.Toolkit.Wpf.UI.XamlHost/WindowsXamlHost.DataBinding.cs

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Uno.UI.XamlHost.Extensions;
using WUX = Windows.UI.Xaml;

namespace Uno.UI.XamlHost.Skia.Wpf
{
	/// <summary>
	/// UnoXamlHost control hosts UWP XAML content inside the Windows Presentation Foundation
	/// </summary>
	partial class UnoXamlHost : UnoXamlHostBase
	{
		/// <summary>
		/// Gets XAML Content by type name
		/// </summary>
		public static DependencyProperty InitialTypeNameProperty { get; } = DependencyProperty.Register("InitialTypeName", typeof(string), typeof(UnoXamlHost));

		/// <summary>
		/// Gets or sets XAML Content by type name
		/// </summary>
		/// <example><code>XamlClassLibrary.MyUserControl</code></example>
		/// <remarks>
		/// Content creation is deferred until after the parent hwnd has been created.
		/// </remarks>
		[Browsable(true)]
		[Category("XAML")]
		public string InitialTypeName
		{
			get => (string)GetValue(InitialTypeNameProperty);

			set => SetValue(InitialTypeNameProperty, value);
		}

		private WUX.UIElement CreateXamlContent()
		{
			WUX.UIElement content = null;
			try
			{
				content = UnoTypeFactory.CreateXamlContentByType(InitialTypeName);
			}
			catch
			{
				content = new WUX.Controls.TextBlock()
				{
					Text = $"Cannot create control of type {InitialTypeName}",
				};
			}

			return content;
		}

		///// <summary>
		///// Creates <see cref="WUX.Application" /> object, wrapped <see cref="WUX.Hosting.DesktopWindowXamlSource" /> instance; creates and
		///// sets root UWP XAML element on DesktopWindowXamlSource.
		///// </summary>
		///// <param name="hwndParent">Parent window handle</param>
		///// <returns>Handle to XAML window</returns>
		//protected override HandleRef BuildWindowCore(HandleRef hwndParent)
		//{
		//    // Create and set initial root UWP XAML content
		//    if (!string.IsNullOrEmpty(InitialTypeName) && Child == null)
		//    {
		//        Child = CreateXamlContent();
		//        var frameworkElement = Child as WUX.FrameworkElement;

		//        // Default to stretch : UWP XAML content will conform to the size of UnoXamlHost
		//        if (frameworkElement != null)
		//        {
		//            frameworkElement.HorizontalAlignment = WUX.HorizontalAlignment.Stretch;
		//            frameworkElement.VerticalAlignment = WUX.VerticalAlignment.Stretch;
		//        }
		//    }

		//    return base.BuildWindowCore(hwndParent);
		//}

		/// <summary>
		/// Set data context on <seealso cref="Child"/> when it has changed.
		/// </summary>
		protected override void OnChildChanged()
		{
			base.OnChildChanged();
			PropagateDataContext();
		}

		private void PropagateDataContext()
		{
			var frameworkElement = _xamlSource.GetVisualTreeRoot() as WUX.FrameworkElement;
			if (frameworkElement != null && frameworkElement.DataContext != DataContext)
			{
				// UnoXamlHost DataContext should flow through to UWP XAML content
				frameworkElement.DataContext = DataContext;
			}
		}
	}
}
