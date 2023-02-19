using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Windows.UI.Xaml.Controls.Primitives;
using Uno;

namespace Windows.UI.Xaml.Controls
{
	public partial class FlipView : Selector
	{
		public FlipView()
		{
			DefaultStyleKey = typeof(FlipView);

			InitializePartial();
		}

		public bool UseTouchAnimationsForAllNavigation
		{
			get { return (bool)GetValue(UseTouchAnimationsForAllNavigationProperty); }
			set { SetValue(UseTouchAnimationsForAllNavigationProperty, value); }
		}

		// Using a DependencyProperty as the backing store for UseTouchAnimationsForAllNavigation.  This enables animation, styling, binding, etc...
		public static DependencyProperty UseTouchAnimationsForAllNavigationProperty { get; } =
			DependencyProperty.Register("UseTouchAnimationsForAllNavigation", typeof(bool), typeof(FlipView), new FrameworkPropertyMetadata(true));

		partial void InitializePartial();
	}
}
