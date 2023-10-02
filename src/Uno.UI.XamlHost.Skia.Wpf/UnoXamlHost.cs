// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/CommunityToolkit/Microsoft.Toolkit.Win32/blob/master/Microsoft.Toolkit.Wpf.UI.XamlHost/WindowsXamlHost.cs

using System.ComponentModel;
using System.Windows;
using WUX = Microsoft.UI.Xaml;

namespace Uno.UI.XamlHost.Skia.Wpf
{
	/// <summary>
	/// UnoXamlHost control hosts UWP XAML content inside the Windows Presentation Foundation
	/// </summary>
	public partial class UnoXamlHost : UnoXamlHostBase
	{
		public UnoXamlHost()
		{
			this.DefaultStyleKey = typeof(UnoXamlHost);
			this.DataContextChanged += UnoXamlHost_DataContextChanged;
			this.Loaded += OnLoaded;
		}

		private void UnoXamlHost_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			PropagateDataContext();
		}

		private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
		{
			Child = CreateXamlContent();
			TryLoadContent();
		}

		/// <summary>
		/// Gets or sets the root UWP XAML element displayed in the WPF control instance.
		/// </summary>
		/// <remarks>This UWP XAML element is the root element of the wrapped DesktopWindowXamlSource.</remarks>
		[Browsable(true)]
		public WUX.UIElement Child
		{
			get => ChildInternal;

			set => ChildInternal = value;
		}
	}
}
