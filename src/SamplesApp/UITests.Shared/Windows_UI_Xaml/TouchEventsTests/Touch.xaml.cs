using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Samples.Content.UITests.TouchEventsTests
{
	[SampleControlInfo("Pointers", "Touch", typeof(TouchViewModel), Description = "Description for sample of Touch")]
	public sealed partial class Touch : UserControl
	{
		public Touch()
		{
			this.InitializeComponent();
		}
	}
}
