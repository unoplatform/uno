using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class ToggleSplitButton
	{
		public bool IsChecked
		{
			get { return (bool)GetValue(IsCheckedProperty); }
			set { SetValue(IsCheckedProperty, value); }
		}

		public static DependencyProperty IsCheckedProperty { get; } =
			DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(ToggleSplitButton), new FrameworkPropertyMetadata(false, OnIsCheckedPropertyChanged));

		private static void OnIsCheckedPropertyChanged(
			DependencyObject sender,
			DependencyPropertyChangedEventArgs args)
		{
			var owner = (ToggleSplitButton)sender;
			owner.OnPropertyChanged(args);
		}
	}
}
