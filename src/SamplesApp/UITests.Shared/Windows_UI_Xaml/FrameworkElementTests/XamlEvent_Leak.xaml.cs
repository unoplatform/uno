using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
using Private.Infrastructure;

namespace UITests.Shared.Windows_UI_Xaml.FrameworkElementTests
{
	[SampleControlInfo("FrameworkElement", "XamlEvent_Leak", description: SampleDescription)]
	public sealed partial class XamlEvent_Leak : UserControl
	{
		private const string SampleDescription =
			"This sample provides a test for XAML provided event " +
			"handlers, which must not make the declaring control leak. The counter " +
			"should not grow and stay above a few instances, and go down back to zero " +
			"after the end of the test. This may take a few seconds before the GC passes.";

		private static int _maxCounter;

		public XamlEvent_Leak()
		{
			this.InitializeComponent();
		}

		private static ConditionalWeakTable<FrameworkElement, Holder> _holders
			= new ConditionalWeakTable<FrameworkElement, Holder>();

		private async void OnRunTest(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < 30; i++)
			{
				var item = new XamlEvent_Leak_UserControl();
				_holders.Add(item, new Holder(HolderUpdate));
				contentHost.Content = item;
				await Task.Delay(100);
				contentHost.Content = null;
				GC.Collect();
				GC.WaitForPendingFinalizers();
				await Task.Delay(100);
			}
		}

		private void HolderUpdate(int value)
		{
			var unused = UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.High,
				() =>
				{
					_maxCounter = Math.Max(value, _maxCounter);
					activeControls.Text = value.ToString();
					maxActiveControls.Text = _maxCounter.ToString();
				}
			);
		}

		private class Holder
		{
			private Action<int> _update;
			private static int _counter;

			public Holder(Action<int> update)
			{
				_update = update;
				_update(++_counter);
			}

			~Holder()
			{
				_update(--_counter);
			}
		}
	}
}
