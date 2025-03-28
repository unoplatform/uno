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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Controls.ScrollBar
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[SampleControlInfo("Scrolling"
#if __WASM__
		, ignoreInSnapshotTests: true
#endif
		)]
	public sealed partial class ScrollBar_Simple : Page
	{
		public ScrollBar_Simple()
		{
			this.InitializeComponent();
		}

		public void OnVerticalScroll(object sender, ScrollEventArgs args)
		{
			scrollValue.Text = $"Vertical Scroll: {args.ScrollEventType}, {args.NewValue}";
		}

		public void OnHorizontalScroll(object sender, ScrollEventArgs args)
		{
			scrollValue.Text = $"Horizontal Scroll: {args.ScrollEventType}, {args.NewValue}";
		}
	}
}
