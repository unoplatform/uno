using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.App.Views
{
	/// <summary>
	/// Test control for verifying that app-level Generic.xaml provides default styles.
	/// </summary>
	public partial class GenericXamlTestControl : Control
	{
		public GenericXamlTestControl()
		{
			DefaultStyleKey = typeof(GenericXamlTestControl);
		}

		public string TestTag
		{
			get => (string)GetValue(TestTagProperty);
			set => SetValue(TestTagProperty, value);
		}

		public static readonly DependencyProperty TestTagProperty =
			DependencyProperty.Register(nameof(TestTag), typeof(string), typeof(GenericXamlTestControl),
				new PropertyMetadata("NotApplied"));

		public string TestTag2
		{
			get => (string)GetValue(TestTag2Property);
			set => SetValue(TestTag2Property, value);
		}

		public static readonly DependencyProperty TestTag2Property =
			DependencyProperty.Register(nameof(TestTag2), typeof(string), typeof(GenericXamlTestControl),
				new PropertyMetadata("NotApplied"));
	}
}
