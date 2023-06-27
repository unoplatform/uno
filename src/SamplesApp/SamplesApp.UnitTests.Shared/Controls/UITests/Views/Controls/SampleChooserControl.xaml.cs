using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SampleControl.Presentation;
using Uno.UI.Extensions;
#if NETFX_CORE
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
#elif XAMARIN || UNO_REFERENCE_API
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using System.Globalization;
#endif

namespace Uno.UI.Samples.Controls
{
	public sealed partial class SampleChooserControl : UserControl
	{
		public SampleChooserControl()
		{
			this.InitializeComponent();
		}

		private void DebugVT(object sender, RoutedEventArgs e)
		{
			var sampleRoot =
				(SampleContentControl as ContentControl).Content as FrameworkElement ??
				((SampleContentControl as ContentControl).TemplatedRoot as ContentPresenter).Content as FrameworkElement ??
				SampleContentControl;
			var sut = sampleRoot?.FindFirstDescendant<FrameworkElement>(x => x.Name == "SUT") ?? sampleRoot;

			var tree = sut?.TreeGraph();
		}
	}
}
