using System;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml.xLoadTests
{
	[Sample("x:Load", Name = "xLoad_xBind_VisualState_5489", Description = "Issue #5489: x:Bind DependencyProperties not resolved when element loaded via VisualStateManager")]
	public sealed partial class xLoad_xBind_VisualState_5489 : UserControl
	{
		public string BoundText
		{
			get { return (string)GetValue(BoundTextProperty); }
			set { SetValue(BoundTextProperty, value); }
		}

		public static readonly DependencyProperty BoundTextProperty =
			DependencyProperty.Register(nameof(BoundText), typeof(string), typeof(xLoad_xBind_VisualState_5489), new PropertyMetadata("Hello from x:Bind"));

		public int BoundNumber
		{
			get { return (int)GetValue(BoundNumberProperty); }
			set { SetValue(BoundNumberProperty, value); }
		}

		public static readonly DependencyProperty BoundNumberProperty =
			DependencyProperty.Register(nameof(BoundNumber), typeof(int), typeof(xLoad_xBind_VisualState_5489), new PropertyMetadata(42));

		public xLoad_xBind_VisualState_5489()
		{
			this.InitializeComponent();
		}

		private void OnLoadClick(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "Loaded", true);
			StatusText.Text = $"Status: Loaded. BoundText='{BoundText}', BoundNumber={BoundNumber}. " +
				$"Check if the TextBlocks above show these values.";
		}

		private void OnUnloadClick(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "Unloaded", true);
			StatusText.Text = "Status: Unloaded";
		}
	}
}
