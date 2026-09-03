using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Uno.UI.Samples.Controls;

namespace GenericApp.Views.Content.UITests.Animations
{
	[Sample("Animations", Name = "DoubleAnimationUsingKeyFrames_TranslateX")]
	public sealed partial class DoubleAnimationUsingKeyFrames_TranslateX : UserControl
	{
		public DoubleAnimationUsingKeyFrames_TranslateX()
		{
			this.InitializeComponent();

			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "Normal", false);
		}
	}
}
