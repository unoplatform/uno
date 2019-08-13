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

namespace UITests.Shared.Windows_UI_Xaml_Controls.CompositionTarget_Default
{
	[SampleControlInfo("CompositionTarget", "CompositionTarget_Default")]
	public sealed partial class CompositionTarget_Default : UserControl
	{
		int counter = 0;

		public CompositionTarget_Default()
		{
			this.InitializeComponent();

			Loaded += (s, e) => Windows.UI.Xaml.Media.CompositionTarget.Rendering += OnRendering;
			Unloaded -= (s, e) => Windows.UI.Xaml.Media.CompositionTarget.Rendering -= OnRendering;

			tbCounter.Text = "Never invoked";
		}

		private void OnRendering(object sender, object e) => tbCounter.Text = $"{counter++}";
	}
}
