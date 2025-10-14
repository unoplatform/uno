using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace GenericApp.Views.Content.UITests.Animations
{
	[SampleControlInfo("Animations", "DoubleAnimation_AutoReverse", description: "Demonstrates AutoReverse functionality for animations")]
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
				// Get the storyboard from the template resources
				var sampleControl = Content as SampleControl;
				if (sampleControl?.SampleContent is DataTemplate template)
				{
					var grid = template.LoadContent() as Grid;
					if (grid?.Resources["MoveStoryboard"] is Storyboard storyboard)
					{
						_storyboard = storyboard;
						_storyboard.Completed += OnStoryboardCompleted;
					}
				}
			}

			if (_storyboard != null)
			{
				StatusText.Text = "Animation started...";
				_storyboard.Begin();
			}
			else
			{
				StatusText.Text = "Error: Could not load storyboard";
			}
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
