using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
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
#elif XAMARIN
using Windows.UI.Xaml.Controls;
using System.Globalization;
#endif

namespace GenericApp.Views.Content.UITests.Clip
{
	[SampleControlInfo("Clip", "ButtonClippingTestsControl", typeof(ButtonTestsViewModel))]
	public sealed partial class ButtonClippingTestsControl : UserControl
	{
		public ButtonClippingTestsControl()
		{
			this.InitializeComponent();
		}
	}
}
