using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.Animations
{
	[Sample("Visual states", Description = "A visual state with several {Binding} setter values: each target must receive its bound value, not only the first.")]
	public sealed partial class VisualState_Setter_Bindings : Page
	{
		public VisualState_Setter_Bindings()
		{
			this.InitializeComponent();
			Root.DataContext = new PanelValues();
			Loaded += (_, _) => ShowPanel();
		}

		private void OnReapply(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "PanelHidden", false);
			ShowPanel();
		}

		private void ShowPanel()
		{
			VisualStateManager.GoToState(this, "PanelVisible", false);
			DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, Report);
		}

		private void Report()
		{
			Check(OpacityResult, "Opacity", Panel.Opacity == 0.5, Panel.Opacity);
			Check(OffsetResult, "TranslateTransform.X", PanelTransform.X == 20, PanelTransform.X);
			Check(WidthResult, "ColumnDefinition.Width", PanelColumn.Width == new GridLength(120), PanelColumn.Width);
		}

		private static void Check(TextBlock result, string what, bool ok, object actual)
			=> result.Text = $"{(ok ? "PASS" : "FAIL")}: {what} = {actual}";

		public sealed class PanelValues
		{
			public double PanelOpacity => 0.5;

			public double PanelOffset => 20;

			public double PanelWidth => 120;
		}
	}
}
