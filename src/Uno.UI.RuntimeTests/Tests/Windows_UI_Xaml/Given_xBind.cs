using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_xBind
{
	[TestMethod]
#if __IOS__
	[Ignore("Fails on iOS")]
#endif
	public async Task When_xBind_With_Cast()
	{
		var SUT = new When_xBind_With_Cast();
		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);

		Assert.AreEqual("ItemOther", SUT.tb.Text);

		SUT.ItemHelp.IsSelected = true;
		Assert.AreEqual("ItemHelp", SUT.tb.Text);

		SUT.ItemOther2.IsSelected = true;
		Assert.AreEqual("ItemOther2", SUT.tb.Text);

		SUT.ItemOther.IsSelected = true;
		Assert.AreEqual("ItemOther", SUT.tb.Text);
	}

	[TestMethod]
	public async Task When_xBind_With_Cast_Default_Namespace()
	{
		var SUT = new When_xBind_With_Cast_Default_Namespace();
		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);

		Assert.AreEqual("Hello", SUT.tb.Text);
	}

#if __ANDROID__
	[TestMethod]
	public async Task When_XBind_TargetDisposed_Test()
	{
		// load the view with the x:Bind
		var SUT = new When_XBind_TargetDisposed();
		var vm = SUT.ViewModel;
		var wrSUT = new WeakReference(SUT);
		await UITestHelper.Load(SUT);

		// remove references to the view
		SUT = null;
		await UITestHelper.Load(new Border { Width = 50, Height = 50 });

		// make sure it is disposed
		GC.Collect();
		GC.WaitForPendingFinalizers();
		await Task.Delay(1000);
		if (!await WaitUntilCollected())
		{
			Assert.Fail("Timed out on waiting the view to be disposed");
		}

		// trigger an INotifyCollectionChanged update via ObservableCollection
		vm.Items.Clear();

		// and, it should not throw:
		/* System.ObjectDisposedException: Cannot access a disposed object. Object name: 'Windows.UI.Xaml.Controls.StackPanel'.
			at Android.Views.ViewGroup.RemoveAllViews()
			at Windows.UI.Xaml.Controls.UIElementCollection.ClearCore()
			at Windows.UI.Xaml.Controls.UIElementCollection.Clear()
			at Windows.UI.Xaml.Controls.ItemsControl.UpdateItems(NotifyCollectionChangedEventArgs args)
			at Windows.UI.Xaml.Controls.ItemsControl.OnItemsSourceSingleCollectionChanged(Object sender, NotifyCollectionChangedEventArgs args, Int32 section)
			at Windows.UI.Xaml.Controls.ItemsControl.OnItemsVectorChanged(IObservableVector`1 sender, IVectorChangedEventArgs e)
			at Uno.UI.Extensions.VectorChangedEventHandlerExtensions.TryRaise(ValueTuple`2 handlers, IObservableVector`1 owner, IVectorChangedEventArgs args)
			at Windows.UI.Xaml.Controls.ItemCollection.OnItemsSourceCollectionChanged(Object sender, NotifyCollectionChangedEventArgs args)
			at Windows.UI.Xaml.Controls.ItemCollection.<>c__DisplayClass33_0.<ObserveCollectionChangedInner>g__handler|0(Object s, NotifyCollectionChangedEventArgs e)
			at System.Collections.ObjectModel.ObservableCollection`1.OnCollectionChanged(NotifyCollectionChangedEventArgs e)
			at System.Collections.ObjectModel.ObservableCollection`1.OnCollectionReset()
			at System.Collections.ObjectModel.ObservableCollection`1.ClearItems()
			at System.Collections.ObjectModel.Collection`1.Clear()
			at Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_xBind.When_XBind_TargetDisposed()
		*/

		async Task<bool> WaitUntilCollected()
		{
			var sw = Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds <= 1000 && wrSUT.Target is When_XBind_TargetDisposed)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				await Task.Delay(100);
			}

			return wrSUT.Target is not When_XBind_TargetDisposed;
		}
	}
#endif
}
