using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
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

namespace UITests.Shared.Windows_UI_Xaml_Controls.Pivot
{
	[Sample("Pivot Tests")]
    public sealed partial class LazyLoaded : UserControl
    {
        public LazyLoaded()
        {
            this.InitializeComponent();
			testPivot.Items.Add(LoadOrderItem("sample 1"));
			testPivot.Items.Add(LoadOrderItem("sample 2"));
			testPivot.Items.Add(LoadOrderItem("sample 3"));
		}

		private PivotItem LoadOrderItem(string sampleText)
		{
			PivotItem pi = new PivotItem { Header = sampleText };
			TextBlock tb = new TextBlock { Text = sampleText };
			tb.Loaded += (s, e) =>
			{
				logsTextBlock.Text += $"\n{sampleText}";
			};

			pi.Content = tb;
			return pi;
		}
	}
}
