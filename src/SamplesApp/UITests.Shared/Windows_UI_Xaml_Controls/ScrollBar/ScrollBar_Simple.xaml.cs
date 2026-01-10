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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Controls.ScrollBar
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("Scrolling"
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
