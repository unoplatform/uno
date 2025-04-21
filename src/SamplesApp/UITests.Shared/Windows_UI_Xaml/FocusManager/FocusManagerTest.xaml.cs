using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.FocusTests
{
	[SampleControlInfo("Focus", "FocusManagerTest", Description = "Validate FocusManager.GetFocusedElement")]
	public sealed partial class FocusManagerTest : UserControl
	{
		public FocusManagerTest()
		{
			this.InitializeComponent();
		}
	}
}
