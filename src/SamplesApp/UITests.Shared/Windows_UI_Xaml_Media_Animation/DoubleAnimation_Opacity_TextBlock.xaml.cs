using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;



namespace GenericApp.Views.Content.UITests.Animations
{
	[SampleControlInfo("Animations", "DoubleAnimation_Opacity_TextBlock")]
	public sealed partial class DoubleAnimation_Opacity_TextBlock : UserControl
	{
		public DoubleAnimation_Opacity_TextBlock()
		{
			this.InitializeComponent();
		}

		private void BeginAnimation(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement elt
				&& elt.Resources.TryGetValue("Storyboard", out var res)
				&& res is Storyboard storyboard)
			{
				Storyboard.SetTarget(storyboard, elt);
				storyboard.Begin();
			}
		}
	}
}
