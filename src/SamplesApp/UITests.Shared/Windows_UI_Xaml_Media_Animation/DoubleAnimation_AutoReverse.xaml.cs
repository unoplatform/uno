using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace GenericApp.Views.Content.UITests.Animations
{
	[Sample("Animations", Description = "Demonstrates AutoReverse functionality. The rectangle should move right then back to the start position.")]
	public sealed partial class DoubleAnimation_AutoReverse : UserControl
	{
		private Storyboard _storyboard;

		public DoubleAnimation_AutoReverse()
		{
			this.InitializeComponent();
		}

		private void StartAnimation(object sender, RoutedEventArgs e)
		{
			if (_storyboard == null)
			{
				_storyboard = (Storyboard)Resources["MoveStoryboard"];
				_storyboard.Completed += OnStoryboardCompleted;
			}

			StatusText.Text = "Animation started...";
			_storyboard.Begin();
		}

		private void StopAnimation(object sender, RoutedEventArgs e)
		{
			_storyboard?.Stop();
			StatusText.Text = "Animation stopped";
		}

		private void OnStoryboardCompleted(object sender, object e)
		{
			StatusText.Text = "Animation completed - rectangle should be back at start position";
		}
	}
}
