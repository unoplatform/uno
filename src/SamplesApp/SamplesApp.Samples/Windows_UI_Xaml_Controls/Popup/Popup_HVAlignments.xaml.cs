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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.Popup
{
	[Sample("Popup", Name = nameof(Popup_HVAlignments), Description = "Popup always opens at the top-left corner of their available \"zone\".\n This zone is can be visualized by placing a Grid next to the Popup with the same H&V-Alignement, Height/Width, Margin.")]
	public sealed partial class Popup_HVAlignments : UserControl
	{
		public Popup_HVAlignments()
		{
			this.InitializeComponent();
		}
	}
}
