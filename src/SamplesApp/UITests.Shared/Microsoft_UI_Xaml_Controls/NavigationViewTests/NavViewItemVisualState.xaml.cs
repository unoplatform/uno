using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UITests.Helpers;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MUXC = Microsoft.UI.Xaml.Controls;
using MUXCP = Microsoft.UI.Xaml.Controls.Primitives;

namespace UITests.Microsoft_UI_Xaml_Controls.NavigationViewTests
{
	[SampleControlInfo("NavigationView", nameof(NavViewItemVisualState), Description = Description, IsManualTest = true, IgnoreInSnapshotTests = true)]
	public sealed partial class NavViewItemVisualState : Page
	{
		private const string Description =
			"[iOS]ManualTest: open the nav-view, and then expand 'asd'. Press and hold 'asd_4'." +
			"While still holding, slowly drag vertically. When the scrollviewer moved, " +
			"the visual-state should reset to 'Normal', instead of being stuck in 'PointerOver'.";

		private bool _initialized = false;

		public NavViewItemVisualState()
		{
			this.InitializeComponent();
			InitializeNVItems();
		}

		private void InitializeNVItems()
		{
			// add a few items and some sub-items for each
			foreach (var header in "qwe,asd,zxc".Split(','))
			{
				var item = new MUXC.NavigationViewItem { Content = new TextBlock { Text = header } };
				InstallDebugHook(item);

				for (int i = 0; i < 20; i++)
				{
					var subitem = new MUXC.NavigationViewItem { Content = new TextBlock { Text = $"{header}_{i}" } };
					InstallDebugHook(subitem);

					item.MenuItems.Add(subitem);
				}
				NavigationViewControl.MenuItems.Add(item);
			}

			void InstallDebugHook(MUXC.NavigationViewItem item)
			{
				var tb = item.Content as TextBlock;
				tb.Tag = tb.Text;

				tb.Loaded += (s, e) =>
				{
					// for tier-1 nvi, the textbox is loaded immediately
					// for tier-2 nvi, the textbox is loaded twice and the 2nd time is when it is ready
					var nvip = VisualTreeHelperEx.GetFirstDescendant<MUXCP.NavigationViewItemPresenter>(item, x => x.Name == "NavigationViewItemPresenter");
					var root = nvip == null ? default : VisualTreeHelperEx.GetFirstDescendant<Grid>(nvip, x => x.Name == "LayoutRoot");

					// check if the template is ready
					if (root != null)
					{
						var group = VisualStateManager.GetVisualStateGroups(root).FirstOrDefault(x => x.Name == "PointerStates");
						group.CurrentStateChanged += (s2, e2) =>
							tb.Text = $"{tb.Tag}: {e2.NewState?.Name}";
						tb.Text = $"{tb.Tag}: {group.CurrentState?.Name}";
					}
				};
			}
		}
	}
}
