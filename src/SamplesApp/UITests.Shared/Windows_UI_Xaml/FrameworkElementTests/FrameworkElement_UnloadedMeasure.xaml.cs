using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Private.Infrastructure;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml.FrameworkElementTests
{
	[Sample("FrameworkElement", "UnloadedMeasure", Description: "This control should not display anything if unloaded UIElements get arranged")]
	public sealed partial class FrameworkElement_UnloadedMeasure : UserControl
	{
		public FrameworkElement_UnloadedMeasure()
		{
			this.InitializeComponent();

			var content = new Border
			{
				Width = 200,
				Height = 200,
				Background = new SolidColorBrush(Colors.Red),
				Child = new TextBlock { Text = "Some text", TextWrapping = TextWrapping.WrapWholeWords, MaxLines = 10 },
			};

			var _ = UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
			{
				myControl.Content = content;
				content.Measure(new Size(10, 100));

				var __ = UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
				{
					myControl.Content = null;
					content.Width = 100;
					content.Height = 100;
					content.Child = new TextBlock { Text = "This should not be visible !!" + new Guid().ToString(), TextWrapping = TextWrapping.WrapWholeWords, MaxLines = 10 };
					content.InvalidateMeasure();
					content.Measure(new Size(100, 10));
					content.Arrange(new Rect(0, 0, 100, 100));
				});
			});
		}
	}
}
