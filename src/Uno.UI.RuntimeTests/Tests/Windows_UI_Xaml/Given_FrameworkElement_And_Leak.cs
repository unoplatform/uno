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
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FrameworkElement_And_Leak
	{
		[TestMethod]
		[DataRow(typeof(XamlEvent_Leak_UserControl), 15)]
		[DataRow(typeof(XamlEvent_Leak_UserControl_xBind), 15)]
		[DataRow(typeof(XamlEvent_Leak_UserControl_xBind_Event), 15)]
		[DataRow(typeof(XamlEvent_Leak_TextBox), 15)]
		[DataRow(typeof(TextBox), 15)]
		[DataRow(typeof(Button), 15)]
		[DataRow(typeof(RadioButton), 15)]
		[DataRow(typeof(TextBlock), 15)]
		[DataRow(typeof(CheckBox), 15)]
		[DataRow(typeof(ListView), 15)]
		[DataRow(typeof(ProgressRing), 15)]
		[DataRow(typeof(Pivot), 15)]
		[DataRow(typeof(ScrollBar), 15)]
		[DataRow(typeof(Slider), 15)]
		[DataRow(typeof(SymbolIcon), 15)]
		[DataRow(typeof(Viewbox), 15)]
		[DataRow(typeof(MenuBar), 15)]
		[DataRow(typeof(ComboBox), 15)]
		[DataRow(typeof(Canvas), 15)]
		[DataRow(typeof(AutoSuggestBox), 15)]
		[DataRow(typeof(AppBar), 15)]
		[DataRow(typeof(Border), 15)]
		[DataRow(typeof(ContentControl), 15)]
		[DataRow(typeof(ContentDialog), 15)]
		public async Task When_Add_Remove(Type controlType, int count)
		{
			var _holders = new ConditionalWeakTable<FrameworkElement, Holder>();

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

				// Waiting for idle is required for collection of
				// DispatcherConditionalDisposable to be executed
				await TestServices.WindowHelper.WaitForIdle();
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

				// Waiting for idle is required for collection of
				// DispatcherConditionalDisposable to be executed
				await TestServices.WindowHelper.WaitForIdle();
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
