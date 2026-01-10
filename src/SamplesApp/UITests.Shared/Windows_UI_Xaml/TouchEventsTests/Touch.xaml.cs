using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.Samples.Content.UITests.TouchEventsTests
{
	[Sample("Pointers", "Touch", typeof(TouchViewModel), Description = "Description for sample of Touch")]
	public sealed partial class Touch : UserControl
	{
		public Touch()
		{
			this.InitializeComponent();
		}
	}
}
