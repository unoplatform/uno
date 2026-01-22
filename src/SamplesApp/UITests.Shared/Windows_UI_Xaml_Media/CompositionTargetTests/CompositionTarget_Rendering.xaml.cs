using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Media.CompositionTargetTests
{
	[Sample("Microsoft.UI.Xaml.Media", Name = "CompositionTarget_Rendering", Description = "Demonstrates the CompositionTarget.Rendering event")]
	public sealed partial class CompositionTarget_Rendering : UserControl
	{
		public CompositionTarget_Rendering()
		{
			this.InitializeComponent();
		}

		TimeSpan oldRenderingTime;
		private void StartButton_Click(object sender, RoutedEventArgs args)
		{
			var counter = 0;
			CompositionTarget.Rendering += (o, e) =>
			{
				counter++;
				var args2 = e as RenderingEventArgs;
				var renderingTime = args2.RenderingTime;
				var delta = renderingTime - oldRenderingTime;
				oldRenderingTime = renderingTime;

				CounterTextBlock.Text = counter.ToString();
				TimeTextBlock.Text = renderingTime.ToString();
				DeltaTextBlock.Text = delta.ToString();
			};
		}
	}
}
