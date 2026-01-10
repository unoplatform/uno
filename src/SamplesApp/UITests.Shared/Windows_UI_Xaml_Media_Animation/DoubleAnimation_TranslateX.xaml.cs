using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace GenericApp.Views.Content.UITests.Animations
{
	[Sample("Animations", "DoubleAnimation_TranslateX")]
	public sealed partial class DoubleAnimation_TranslateX : UserControl
	{
		public DoubleAnimation_TranslateX()
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
