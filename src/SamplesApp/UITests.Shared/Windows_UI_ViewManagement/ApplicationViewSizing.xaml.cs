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
	[SampleControlInfo(category: "Windows.UI.ViewManagement")]

	public sealed partial class ApplicationViewSizing : Page
	{
		public ApplicationViewSizing()
		{
			this.InitializeComponent();
			var coreWindow = CoreWindow.GetForCurrentThreadSafe();
			if (coreWindow is not null)
			{
				coreWindow.SizeChanged += ApplicationViewSizing_SizeChanged;
			}
			this.Unloaded += ApplicationViewSizing_Unloaded;
		}

		private void ApplicationViewSizing_SizeChanged(CoreWindow sender, Windows.UI.Core.WindowSizeChangedEventArgs args)
		{
			LastSizeTime.Text = DateTime.Now.ToLongTimeString();
			LastSize.Text = args.Size.Width + "x" + args.Size.Height;
		}

		private void ApplicationViewSizing_Unloaded(object sender, RoutedEventArgs e)
		{
			var coreWindow = CoreWindow.GetForCurrentThreadSafe();
			if (coreWindow is not null)
			{
				coreWindow.SizeChanged -= ApplicationViewSizing_SizeChanged;
			}
		}

		void SetWindowSize_Click(System.Object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			ApplicationView
				.GetForCurrentView()
				.TryResizeView(new Size(
					WidthInput.Value,
					HeightInput.Value));
		}

		void SetMinWindowSize_Click(System.Object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			ApplicationView
				.GetForCurrentView()
				.SetPreferredMinSize(new Size(
					WidthInput.Value,
					HeightInput.Value));
		}

		void SetPreferredLaunchViewSize_Click(System.Object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			ApplicationView.PreferredLaunchViewSize = new Size(WidthInput.Value, HeightInput.Value);
		}
	}
}
