using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

using TabView = Microsoft.UI.Xaml.Controls.TabView;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls
{
	public partial class Given_TabView_SyncContext_12265_ViewModel
	{
		public string Header { get; set; }
		public SynchronizationContext SyncContextAtBinding;
		public bool WasBindingEvaluated;

		public IList<int> Numbers
		{
			get
			{
				WasBindingEvaluated = true;
				SyncContextAtBinding = SynchronizationContext.Current;
				return new List<int> { 1, 2, 3 };
			}
		}
	}

	[TestClass]
	[RunsOnUIThread]
	public partial class Given_TabView_SyncContext_12265
	{
		// Reproduction for https://github.com/unoplatform/uno/issues/12265
		// On the first-rendered tab, binding evaluation sees
		// SynchronizationContext.Current == CoreDispatcherSynchronizationContext.
		// When switching to a tab that hasn't been materialized yet, the same
		// binding evaluation sees SynchronizationContext.Current == null, which
		// breaks user code that relies on TaskScheduler.FromCurrentSynchronizationContext()
		// to marshal async continuations back to the UI thread.
		// NativeWinUI exhibits the same null-SynchronizationContext behavior on the
		// deferred-tab binding pass, so the second-tab assertion fails there too.
		// Tracked by https://github.com/unoplatform/uno/issues/12265.
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Second_Tab_Selected_Binding_Has_SynchronizationContext_12265()
		{
			var vms = new ObservableCollection<Given_TabView_SyncContext_12265_ViewModel>
			{
				new() { Header = "Tab1" },
				new() { Header = "Tab2" },
			};

			var tabItemTemplate = (DataTemplate)XamlReader.Load(
				"""
				<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					<ItemsControl ItemsSource="{Binding Numbers}" />
				</DataTemplate>
				""");

			var SUT = new TabView
			{
				TabItemsSource = vms,
				TabItemTemplate = tabItemTemplate,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(vms[0].WasBindingEvaluated, "First tab's binding should have been evaluated.");
			Assert.IsNotNull(vms[0].SyncContextAtBinding, "First tab: SynchronizationContext.Current should be non-null.");

			SUT.SelectedIndex = 1;
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(vms[1].WasBindingEvaluated, "Second tab's binding should have been evaluated after selection.");
			Assert.IsNotNull(
				vms[1].SyncContextAtBinding,
				"Second tab: SynchronizationContext.Current was null during binding evaluation. " +
				"See https://github.com/unoplatform/uno/issues/12265");
		}
	}
}
