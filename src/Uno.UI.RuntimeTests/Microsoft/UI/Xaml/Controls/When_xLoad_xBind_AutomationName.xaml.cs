using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public sealed partial class When_xLoad_xBind_AutomationName : UserControl
	{
		public bool IsShown
		{
			get => (bool)GetValue(IsShownProperty);
			set => SetValue(IsShownProperty, value);
		}

		public static readonly DependencyProperty IsShownProperty =
			DependencyProperty.Register(nameof(IsShown), typeof(bool), typeof(When_xLoad_xBind_AutomationName), new PropertyMetadata(false));

		public When_xLoad_xBind_AutomationName()
		{
			this.InitializeComponent();
		}
	}
}
