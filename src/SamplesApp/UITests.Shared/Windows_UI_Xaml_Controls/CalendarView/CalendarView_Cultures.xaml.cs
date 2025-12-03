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
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.CalendarView;

[Sample("Pickers", IgnoreInSnapshotTests = true, IsManualTest = true, Description = """
		Showing CalendarView with different cultures.
		""")]
public sealed partial class CalendarView_Cultures : Page
{
	public CalendarView_Cultures()
	{
		this.InitializeComponent();
	}
}
