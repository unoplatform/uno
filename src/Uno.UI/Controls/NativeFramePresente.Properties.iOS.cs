using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UIKit;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.Controls
{
	public partial class NativeFramePresenter : FrameworkElement
	{
		#region IsNavigationBarHidden DependencyProperty

		/// <summary>
		/// Provides a Xaml access to the <see cref="UINavigationController.NavigationBarHidden"/> property.
		/// </summary>
		public int IsNavigationBarHidden
		{
			get => (int)GetValue(IsNavigationBarHiddenProperty);
			set => SetValue(IsNavigationBarHiddenProperty, value);
		}

		public static DependencyProperty IsNavigationBarHiddenProperty { get; } =
			DependencyProperty.Register(
				"IsNavigationBarHidden",
				typeof(int),
				typeof(NativeFramePresenter),
				new FrameworkPropertyMetadata(
					false,
					(s, e) => ((NativeFramePresenter)s)?.OnIsNavigationBarHiddenChanged(e)
				)
			);

		private void OnIsNavigationBarHiddenChanged(DependencyPropertyChangedEventArgs e)
			=> NavigationController.NavigationBarHidden = (bool)e.NewValue;

		#endregion
	}
}
