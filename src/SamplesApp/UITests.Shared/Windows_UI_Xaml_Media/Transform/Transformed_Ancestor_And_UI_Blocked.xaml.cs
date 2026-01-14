using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.Extensions;
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

#if HAS_UNO
using Uno.Foundation.Logging;
#else
using Microsoft.Extensions.Logging;
using Uno.Logging;
#endif

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Media.Transform
{
	[Sample("Transform", "Transformed_Ancestor_And_UI_Blocked", IgnoreInSnapshotTests: true, Description: "Animation in transformed hierarchy, under simulated heavy UI load. Animation should remain smooth on Android 8+ devices.")]
	public sealed partial class Transformed_Ancestor_And_UI_Blocked : UserControl
	{
#pragma warning disable CS0109
#if HAS_UNO
		private new readonly Logger _log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(Transformed_Ancestor_And_UI_Blocked));
#else
		private static readonly ILogger _log = Uno.Extensions.LogExtensionPoint.Log(typeof(Transformed_Ancestor_And_UI_Blocked));
#endif
#pragma warning restore CS0109

		private bool _isLoaded;

		public Transformed_Ancestor_And_UI_Blocked()
		{
			this.InitializeComponent();

			Loaded += MainPage_Loaded;
			Unloaded += MainPage_Unloaded;
		}

		private void MainPage_Unloaded(object sender, RoutedEventArgs e)
		{
			_isLoaded = false;
		}

		private void MainPage_Loaded(object sender, RoutedEventArgs e)
		{
			_isLoaded = true;
			TryBlockUI();
		}

		private bool _isUIBlockingEnabled = true;
		private void ToggleBusyButton_Click(object sender, RoutedEventArgs e)
		{
			_isUIBlockingEnabled = !_isUIBlockingEnabled;
			(sender as Button).Content = _isUIBlockingEnabled ? "Disable heavy UI load" : "Enable heavy UI load";
		}

		private async void TryBlockUI()
		{
			try
			{
				while (_isLoaded)
				{
					if (_isUIBlockingEnabled)
					{
						BlockUI(400);
					}
					await Task.Delay(100);
				}
			}
			catch (Exception e)
			{
				_log.Error("Failed", e);
			}
		}

		private void BlockUI(int timeMS)
		{
			var startTime = DateTimeOffset.Now;
			var elapsed = 0d;
			do
			{
				// Spin
				elapsed = (DateTimeOffset.Now - startTime).TotalMilliseconds;
			}
			while (elapsed < timeMS && _isUIBlockingEnabled);
		}
	}
}
