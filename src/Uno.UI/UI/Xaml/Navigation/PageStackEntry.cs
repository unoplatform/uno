using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Windows.UI.Xaml.Navigation
{
	public sealed partial class PageStackEntry : DependencyObject
	{
		public PageStackEntry(Type sourcePageType, object parameter, NavigationTransitionInfo navigationTransitionInfo)
		{
			InitializeBinder();

			NavigationTransitionInfo = navigationTransitionInfo;
			SourcePageType = sourcePageType;
			Parameter = parameter;
		}

		#region SourcePageType DependencyProperty

		public Type SourcePageType
		{
			get { return (Type)this.GetValue(SourcePageTypeProperty); }
			set { this.SetValue(SourcePageTypeProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SourcePageType.  This enables animation, styling, binding, etc...
		public static DependencyProperty SourcePageTypeProperty { get; } =
			DependencyProperty.Register(
				"SourcePageType",
				typeof(Type),
				typeof(PageStackEntry),
				new FrameworkPropertyMetadata(null, (s, e) => ((PageStackEntry)s)?.OnSourcePageTypeChanged(e))
			);

		private void OnSourcePageTypeChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		public NavigationTransitionInfo NavigationTransitionInfo { get; internal set; }

		public object Parameter { get; }

		internal Page Instance { get; set; }
	}
}
