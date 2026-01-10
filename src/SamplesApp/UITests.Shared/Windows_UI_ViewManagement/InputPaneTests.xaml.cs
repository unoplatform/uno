using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using System;

namespace UITests.Windows_UI_ViewManagement
{
	[Sample(category: "Windows.UI.ViewManagement")]

	public sealed partial class InputPaneTests : Page
	{
		public InputPaneTests()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
			this.Unloaded += OnUnloaded;
		}

		public bool SimulateKeyboard { get; set; }

		private void TryShow()
		{
			var inputPane = InputPane.GetForCurrentView();
			inputPane.TryShow();
		}

		private void TryHide()
		{
			var inputPane = InputPane.GetForCurrentView();
			inputPane.TryHide();
		}

		private void OnInputPaneShowing(InputPane sender, InputPaneVisibilityEventArgs args)
		{
			UpdateOccludedRect(sender.OccludedRect, "Showing");
		}

		private void OnInputPaneHiding(InputPane sender, InputPaneVisibilityEventArgs args)
		{
			UpdateOccludedRect(sender.OccludedRect, "Hiding");
		}

		private void UpdateOccludedRect(Rect occludedRect, string eventType)
		{
			OccludedRectTextBlock.Text = $"Occluded Rect: {occludedRect}, Last Event: {eventType}";
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			var inputPane = InputPane.GetForCurrentView();
			inputPane.Showing += OnInputPaneShowing;
			inputPane.Hiding += OnInputPaneHiding;
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			var inputPane = InputPane.GetForCurrentView();
			inputPane.Showing -= OnInputPaneShowing;
			inputPane.Hiding -= OnInputPaneHiding;
		}
	}
}
