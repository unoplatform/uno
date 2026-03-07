using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.App.Views
{
	/// <summary>
	/// Test control for verifying that styles in MergedDictionaries of Generic.xaml are applied.
	/// </summary>
	public partial class GenericXamlMergedControl : Control
	{
		public GenericXamlMergedControl()
		{
			DefaultStyleKey = typeof(GenericXamlMergedControl);
		}

		public string TestTag
		{
			get => (string)GetValue(TestTagProperty);
			set => SetValue(TestTagProperty, value);
		}

		public static readonly DependencyProperty TestTagProperty =
			DependencyProperty.Register(nameof(TestTag), typeof(string), typeof(GenericXamlMergedControl),
				new PropertyMetadata("NotApplied"));
	}
}
