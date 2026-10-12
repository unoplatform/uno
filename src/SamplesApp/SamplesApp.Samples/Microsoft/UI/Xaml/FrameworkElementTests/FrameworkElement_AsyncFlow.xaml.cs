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
using System.Threading.Tasks;
using System.Threading;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml.FrameworkElementTests;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[Sample("FrameworkElement", IsManualTest = true, Description = "When pressing the button, the text should not be null and should indicate the active synchronization context")]
public sealed partial class FrameworkElement_AsyncFlow : Page
{
	public FrameworkElement_AsyncFlow()
	{
		this.InitializeComponent();
	}

	public async void Button_Click(object sender, object args)
	{
		await Task.Delay(10);
		results.Text = SynchronizationContext.Current?.ToString() ?? "Invalid";
	}
}
