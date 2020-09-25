#if !WINDOWS_UWP
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FrameworkElement_And_Leak
	{
		private static ConditionalWeakTable<FrameworkElement, Holder> _holders
			= new ConditionalWeakTable<FrameworkElement, Holder>();

#if !__WASM__ // Deactivated until https://github.com/unoplatform/uno/pull/3728 is merged
		[TestMethod]
		[DataRow(typeof(XamlEvent_Leak_UserControl), 15)]
		[DataRow(typeof(XamlEvent_Leak_UserControl_xBind), 15)]
		[DataRow(typeof(XamlEvent_Leak_UserControl_xBind_Event), 15)]
#endif
		public async Task When_Add_Remove(Type controlType, int count)
		{
			var maxCounter = 0;
			var activeControls = 0;
			var maxActiveControls = 0;

			var rootContainer = new ContentControl();

			TestServices.WindowHelper.WindowContent = rootContainer;

			await TestServices.WindowHelper.WaitForIdle();

			for (int i = 0; i < count; i++)
			{
				var item = Activator.CreateInstance(controlType) as FrameworkElement;
				_holders.Add(item, new Holder(HolderUpdate));
				rootContainer.Content = item;
				await TestServices.WindowHelper.WaitForIdle();
				rootContainer.Content = null;
				GC.Collect();
				GC.WaitForPendingFinalizers();
				await Task.Yield();
			}

			void HolderUpdate(int value)
			{
				_ = rootContainer.Dispatcher.RunAsync(CoreDispatcherPriority.High,
					() =>
					{
						maxCounter = Math.Max(value, maxCounter);
						activeControls = value;
						maxActiveControls = maxCounter;
					}
				);
			}

			var sw = Stopwatch.StartNew();

			while (sw.Elapsed < TimeSpan.FromSeconds(5) && activeControls != 0)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				await Task.Yield();
			}

			Assert.AreEqual(0, activeControls);
		}

		private class Holder
		{
			private readonly Action<int> _update;
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
#endif
