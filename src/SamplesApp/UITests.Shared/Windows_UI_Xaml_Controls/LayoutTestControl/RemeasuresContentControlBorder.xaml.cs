using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.LayoutTestControl
{
	[SampleControlInfo("LayoutTestControl", nameof(RemeasuresContentControlBorder))]
	public sealed partial class RemeasuresContentControlBorder : UserControl
	{
		public int MeasureCount { get; set; } = 0;
		private Random Rand = new Random(1000);

		public RemeasuresContentControlBorder()
		{
			this.InitializeComponent();
		}

		private void ChangeContentClick(object sender, RoutedEventArgs e)
		{
			var width = Rand.Next(10, 150);
			this.ContentBorder.Width = width;
			this.ContentBorder.Height = width;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureCount++;
			this.MeasureText.Text = $"Total number of Measure of this Sample: {MeasureCount}";
			return base.MeasureOverride(availableSize);
		}
	}
}
