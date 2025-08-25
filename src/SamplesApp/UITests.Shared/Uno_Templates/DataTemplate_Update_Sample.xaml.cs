using System;
using Uno.UI.Samples.Controls;
using Uno;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Uno.UI.Samples.UITests.Templates
{
	[Sample("Templates", Description = "Demonstrates updating a shared DataTemplate factory at runtime and auto-refresh of multiple presenters.")]
	public sealed partial class DataTemplate_Update_Sample : Page
	{
		public DataTemplate_Update_Sample()
		{
			Uno.DataTemplateHelper.EnableUpdateSubscriptions();

			this.InitializeComponent();
		}

		private void OnRedClick(object sender, RoutedEventArgs e)
		{
#if !WINAPPSDK
			if (Resources["SharedTemplate"] is DataTemplate dt)
			{
				Uno.DataTemplateHelper.UpdateDataTemplate(dt, () =>
				{
					var rect = new Rectangle
					{
						Width = 60,
						Height = 40,
						Fill = new SolidColorBrush(Windows.UI.Colors.Red)
					};
					var grid = new Grid();
					grid.Children.Add(rect);
					return grid;
				});
			}
#endif
		}

		private void OnBlueClick(object sender, RoutedEventArgs e)
		{
#if !WINAPPSDK
			if (Resources["SharedTemplate"] is DataTemplate dt)
			{
				Uno.DataTemplateHelper.UpdateDataTemplate(dt, () =>
				{
					var rect = new Rectangle
					{
						Width = 60,
						Height = 40,
						Fill = new SolidColorBrush(Windows.UI.Colors.Blue)
					};
					var grid = new Grid();
					grid.Children.Add(rect);
					return grid;
				});
			}
		}
#endif
	}
}
