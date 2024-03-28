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

namespace UITests.Windows_UI_Input.PointersTests
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("Pointers")]
	public sealed partial class HitTest_Control : Page
	{
		public HitTest_Control()
		{
			this.InitializeComponent();
		}

		void ResetResult(object sender, object args)
		{
			ResultTextBlock.Text = "None";
		}

		void BehindControlPressed(object sender, object args)
		{
			ResultTextBlock.Text = "Behind control pressed";
		}

		void FrontControlPressed(object sender, object args)
		{
			ResultTextBlock.Text = "Front control pressed";
		}
	}
}
