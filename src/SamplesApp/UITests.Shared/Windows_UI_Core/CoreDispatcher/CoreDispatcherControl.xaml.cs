using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Core.CoreDispatcherControl
{

  [SampleControlInfo("Windows.UI.Core.CoreDispatcher", "CoreDispatcherControl")]
  public sealed partial class CoreDispatcherControl : UserControl
  {
    public CoreDispatcherControl()
    {
      this.InitializeComponent();
    }

	private async void RunAsyncButton_Click(object sender, RoutedEventArgs e)
	{
	  try
	  {
		await Dispatcher.RunAsync(async () =>
		{
		  Result.Text = "Success";
		});
	  }
	  catch(Exception ex)
	  {
		Result.Text = ex.Message;
	  }
	}

  }
  internal static class CoreDispatcherExtensions
  {
	internal static IAsyncAction RunAsync(this CoreDispatcher coreDispatcher, DispatchedHandler dispatchedHandler)
	{
	  return coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, dispatchedHandler);
	}
  }
}
