using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;



namespace GenericApp.Views.Content.UITests.Animations
{
	[Sample("Animations", "DoubleAnimation_Opacity_TextBlock")]
	public sealed partial class DoubleAnimation_Opacity_TextBlock : UserControl
	{
		private Storyboard _storyboard;

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
				_storyboard = storyboard;
				_storyboard.Begin();
			}
		}
		private void EndAnimation(object sender, RoutedEventArgs e)
		{
			_storyboard?.Stop();
		}
	}
}
