// Uncomment to get additional reference tracking
//#define TRACK_REFS
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Extensions;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Windows.UI.Core;
using Uno.Extensions;

#if !HAS_UNO_WINUI && !WINAPPSDK
using Microsoft.UI.Xaml.Controls;
#endif

#if WINAPPSDK
using Microsoft.UI.Xaml.Media;
#endif

#if __APPLE_UIKIT__
using UIKit;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_FrameworkElement_And_Leak
	{
		[TestMethod]
		[Timeout(3 * 60 * 1000)]
		[DataRow(typeof(XamlEvent_Leak_UserControl), 15)]
		[DataRow(typeof(XamlEvent_Leak_UserControl_xBind), 15)]
		[DataRow(typeof(XamlEvent_Leak_UserControl_xBind_Event), 15)]
		[DataRow(typeof(XamlEvent_Leak_TextBox), 15)]
		[DataRow(typeof(Animation_Leak), 15)]
		[DataRow(typeof(TextBox), 15)]
		[DataRow(typeof(Button), 15)]
		[DataRow(typeof(RadioButton), 15)]
		[DataRow(typeof(ToggleButton), 15)]
		[DataRow(typeof(RepeatButton), 15)]
		[DataRow(typeof(TextBlock), 15)]
		[DataRow(typeof(ScrollViewer), 15)]
		[DataRow(typeof(CheckBox), 15)]
		[DataRow(typeof(ListView), 15)]
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.ProgressBar), 15,
#if __APPLE_UIKIT__
			LeakTestStyles.Uwp // Fluent styles disabled - #18105
#else
			LeakTestStyles.All
#endif
			)]
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.ProgressRing), 15)]
#endif
		//[DataRow(typeof(Microsoft.UI.Xaml.Controls.ProgressRing), 15)] This leaks, issue #9078
		[DataRow(typeof(Pivot), 15)]
		[DataRow(typeof(ScrollBar), 15)]
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Slider), 15)]
#endif
		[DataRow(typeof(SymbolIcon), 15)]
		[DataRow(typeof(Viewbox), 15)]
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.MenuBar), 15)]
		[DataRow(typeof(ComboBox), 15)]
		[DataRow(typeof(Canvas), 15)]
		[DataRow(typeof(AutoSuggestBox), 15)]
		[DataRow(typeof(AppBar), 15)]
		[DataRow(typeof(CommandBar), 15)]
		[DataRow(typeof(Border), 15)]
		[DataRow(typeof(ContentControl), 15)]
		[DataRow(typeof(ContentDialog), 15)]
		[DataRow(typeof(RelativePanel), 15)]
		[DataRow(typeof(FlipView), 15)]
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(DatePicker), 15)]
		[DataRow(typeof(TimePicker), 15)]
#endif
#if !__APPLE_UIKIT__ && !__ANDROID__ // Disabled https://github.com/unoplatform/uno/issues/9080
		[DataRow(typeof(CalendarView), 15)]
#endif
		[DataRow(typeof(Page), 15)]
		[DataRow(typeof(Image), 15)]
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(ToggleSwitch), 15)]
#endif
#if __SKIA__ && HAS_UNO_WINUI // Control is currently supported on Skia targets only.
		[DataRow(typeof(SelectorBar), 15)]
		[DataRow(typeof(SelectorBarItem), 15)]
		[DataRow(typeof(ItemsView), 15)]
		[DataRow(typeof(ScrollView), 15)]
#endif
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.SwipeControl), 15)]
		[DataRow(typeof(SplitView), 15)]
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.AnimatedIcon), 15,
#if __ANDROID__
			LeakTestStyles.Uwp // Fluent styles disabled - #14341
#else
			LeakTestStyles.All
#endif
			)]
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/issues/9080
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.BreadcrumbBar), 15)]
#endif
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.BreadcrumbBarItem), 15)]
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/issues/9080
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.ColorPicker), 15)]
#endif
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.Primitives.ColorPickerSlider), 15)]
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.Primitives.ColorSpectrum), 15)]
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.Expander), 15, LeakTestStyles.All, RuntimeTestPlatforms.SkiaWasm)] // Fails on net10.0-wasm, see https://github.com/unoplatform/uno/issues/9080
#endif
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.ImageIcon), 15)]
#if !WINAPPSDK
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.InfoBadge), 15)]
#endif
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.InfoBar), 15)]
#endif
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.Primitives.InfoBarPanel), 15)]
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.Primitives.MonochromaticOverlayPresenter), 15)]
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.NavigationViewItem), 15, LeakTestStyles.All, RuntimeTestPlatforms.SkiaWasm)] // Fails on net10.0-wasm, see https://github.com/unoplatform/uno/issues/9080
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter), 15)]
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.NavigationView), 15)]
#endif
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.NumberBox), 15)]
#if !WINAPPSDK
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.PagerControl), 15)]
#endif
#endif
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.PipsPager), 15)]
#endif
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.RefreshContainer), 15)]
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.RadioButtons), 15)]
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem), 15)]
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.RatingControl), 15)]
#endif
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.ItemsRepeater), 15)]
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.SplitButton), 15)]
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.TabView), 15)]
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.Primitives.TabViewListView), 15)]
#endif
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.TreeView), 15)]
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft.UI.Xaml.Controls.TwoPaneView), 15)]
		[DataRow("SamplesApp.Windows_UI_Xaml.Clipping.XamlButtonWithClipping_Scrollable", 15)]
		[DataRow("Uno.UI.Samples.Content.UITests.ButtonTestsControl.AppBar_KeyBoard", 15)]
		[DataRow("Uno.UI.Samples.Content.UITests.ButtonTestsControl.Buttons", 15)]
#endif
		[DataRow("UITests.Windows_UI_Xaml.xLoadTests.xLoad_Test_For_Leak", 15)]
#if !__APPLE_UIKIT__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow("UITests.Windows_UI_Xaml_Controls.ToolTip.ToolTip_LeakTest", 15)]
#endif
		[DataRow("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.Button_Command_Leak", 15)]
		[DataRow("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.ItemsControl_ItemsSource_Leak", 15)]
#if !__WASM__ && !__APPLE_UIKIT__ && !WINAPPSDK // Disabled - https://github.com/unoplatform/uno/issues/7860
		[DataRow("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.ContentDialog_Leak", 15, LeakTestStyles.All, RuntimeTestPlatforms.SkiaUIKit | RuntimeTestPlatforms.NativeUIKit)]
#endif
		[DataRow(typeof(TextBox_Focus_Leak), 15, LeakTestStyles.All, RuntimeTestPlatforms.SkiaUIKit | RuntimeTestPlatforms.NativeUIKit)] // UIKit Disabled - #10344
		[DataRow(typeof(PasswordBox_Focus_Leak), 15,
#if __ANDROID__
			LeakTestStyles.Uwp // Fluent styles disabled - #14340
#else
			LeakTestStyles.All
#endif
			, RuntimeTestPlatforms.SkiaUIKit | RuntimeTestPlatforms.NativeUIKit)] // UIKit Disabled - #10344
		[DataRow(typeof(MediaPlayerElement), 15, LeakTestStyles.All, RuntimeTestPlatforms.SkiaWasm)]
		[DataRow("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.CommandBarFlyout_Leak", 15, LeakTestStyles.All, RuntimeTestPlatforms.NativeUIKit)] // flaky on native iOS
		public async Task When_Add_Remove(object controlTypeRaw, int count, LeakTestStyles leakTestStyles = LeakTestStyles.All, RuntimeTestPlatforms ignoredPlatforms = RuntimeTestPlatforms.None)
		{
			if (ignoredPlatforms.HasFlag(RuntimeTestsPlatformHelper.CurrentPlatform))
			{
				Assert.Inconclusive("This test is ignored on this platform.");
			}

			if (leakTestStyles.HasFlag(LeakTestStyles.Fluent))
			{
				await When_Add_Remove_Inner(controlTypeRaw, count);
			}

			if (leakTestStyles.HasFlag(LeakTestStyles.Uwp))
			{
				using var _ = StyleHelper.UseUwpStyles();
				await When_Add_Remove_Inner(controlTypeRaw, count);
			}
		}

		private async Task When_Add_Remove_Inner(object controlTypeRaw, int count)
		{
			Type GetType(string s)
				=> AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(s)).Where(t => t != null).First()!;

			var controlType = controlTypeRaw switch
			{
				Type ct => ct,
				string s => GetType(s),
				_ => throw new InvalidOperationException()
			};

			var weakRefs = new HashSet<WeakReference<DependencyObject>>();
			var totalRefCount = 0;
			void TrackDependencyObject(DependencyObject target)
			{
				totalRefCount++;
				weakRefs.Add(new WeakReference<DependencyObject>(target));
			}

			IEnumerable<DependencyObject> RemoveDeadRefsAndGetAliveRefs()
			{
				var toRemove = new List<WeakReference<DependencyObject>>();
				foreach (var weakRef in weakRefs)
				{
					if (weakRef.TryGetTarget(out var target))
					{
						yield return target;
					}
					else
					{
						toRemove.Add(weakRef);
					}
				}

				foreach (var weakReference in toRemove)
				{
					weakRefs.Remove(weakReference);
				}
			}

			var forest = new List<string>();

			var rootContainer = new ContentControl();

			TestServices.WindowHelper.WindowContent = rootContainer;

			await TestServices.WindowHelper.WaitForIdle();

#if TRACK_REFS
			Uno.UI.DataBinding.BinderReferenceHolder.IsEnabled = true;

			Uno.UI.DataBinding.BinderReferenceHolder.PurgeHolders();
			var preStats = Uno.UI.DataBinding.BinderReferenceHolder.GetReferenceStats()
				.GroupBy(x => x.Item1, x => x.Item2)
				.ToDictionary(g => g.Key, g => g.Sum());
			Dictionary<Type, int>? run0Stats = null;
#endif

			for (int i = 0; i < count - 1; i++)
			{
				await MaterializeControl(controlType, rootContainer);
			}
			var totalRefCountExceptForLastControlInstance = totalRefCount;
			await MaterializeControl(controlType, rootContainer);

			var sw = Stopwatch.StartNew();

			var endTime = TimeSpan.FromSeconds(30);
			while (sw.Elapsed < endTime && RemoveDeadRefsAndGetAliveRefs().Any())
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				// This gets around some GC quirks where the objects from the last instance of the control
				// stick around until this test method returns
#if __SKIA__
				if (OperatingSystem.IsBrowser() || OperatingSystem.IsLinux() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
#endif
				{
					await Task.Yield();
					GC.Collect();
					GC.WaitForPendingFinalizers();
					GC.Collect();
				}

				// Waiting for idle is required for collection of
				// DispatcherConditionalDisposable to be executed
				await TestServices.WindowHelper.WaitForIdle();
			}

#if TRACK_REFS
			var postStats = Uno.UI.DataBinding.BinderReferenceHolder.GetReferenceStats()
				.GroupBy(x => x.Item1, x => x.Item2)
				.ToDictionary(g => g.Key, g => g.Sum());
			var delta = preStats.Keys.Concat(postStats.Keys).Distinct()
				.Select(x => $"{x.Name}: {(preStats.TryGetValue(x, out var pre) ? pre : 0)} -> {(postStats.TryGetValue(x, out var post) ? post : 0)}")
				.ToArray();

			// GetLeakedObjects contain more objects than tracked by _holder
			//var leaks = Uno.UI.DataBinding.BinderReferenceHolder.GetLeakedObjects();
#endif

			var retainedMessage = "";

			if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser())
			{
				var retainedTypes = RemoveDeadRefsAndGetAliveRefs().Select(ExtractTargetName).ToArray();
				if (RemoveDeadRefsAndGetAliveRefs().Any())
				{
					Console.WriteLine($"\n --- Retained types ---\n{string.Join("\n", retainedTypes)}");

					Console.WriteLine($"\n ========== first run: tree-graph ============\n{forest.FirstOrDefault()}");

#if TRACK_REFS
				Console.WriteLine($"\n ========== first run: total objects created ============");
				foreach (var kvp in run0Stats ?? new Dictionary<Type, int>())
				{
					Console.WriteLine($"{kvp.Key.Name}: {kvp.Value}");
				}
				Console.WriteLine();

				Console.WriteLine($"\n ========== post-run: ref count ============");
				foreach (var item in delta)
				{
					Console.WriteLine(item);
				}
				Console.WriteLine();
#endif
				}

				retainedMessage = retainedTypes.JoinBy(";");
			}

			if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser() || OperatingSystem.IsLinux())
			{
				// Some platforms have a problem with GC timing and/or the way things like async methods are compiled
				// where the last created instance of the control will remain in memory, so we check that objects from
				// all but the last instance of the control are collected
				RemoveDeadRefsAndGetAliveRefs().Count().Should().BeLessOrEqualTo(totalRefCount - totalRefCountExceptForLastControlInstance, retainedMessage);
			}
			else
			{
				Assert.AreEqual(0, RemoveDeadRefsAndGetAliveRefs().Count(), retainedMessage);
			}

			static string ExtractTargetName(DependencyObject p)
			{
				if (p is FrameworkElement { Name: { Length: > 0 } name } fe)
				{
					return $"{fe.GetType().Name}/{name}";
				}
				else
				{
					return p.ToString() ?? "null";
				}
			}

			async Task MaterializeControl(Type controlType, ContentControl rootContainer)
			{
				rootContainer.Content = (FrameworkElement)Activator.CreateInstance(controlType)!;
				TrackDependencyObject((rootContainer.Content as DependencyObject)!);
				await TestServices.WindowHelper.WaitForIdle();

				if (rootContainer.Content is IExtendedLeakTest extendedTest)
				{
					void TrackAdditionalObject(object? sender, DependencyObject e)
					{
						if (e is not null)
						{
							TrackDependencyObject(e);
						}
					}
					if (rootContainer.Content is ITrackingLeakTest leakTrackingProvider)
					{
						leakTrackingProvider.ObjectTrackingRequested += TrackAdditionalObject;
					}

					await extendedTest.WaitForTestToComplete();

					// Unsubscribe to avoid memory leaks
					if (rootContainer.Content is ITrackingLeakTest leakTrackingProvider2)
					{
						leakTrackingProvider2.ObjectTrackingRequested -= TrackAdditionalObject;
					}
				}

#if TRACK_REFS
				if (run0Stats is not { })
				{
					run0Stats = Uno.UI.DataBinding.BinderReferenceHolder.GetReferenceStats()
						.GroupBy(x => x.Item1, x => x.Item2)
						.ToDictionary(g => g.Key, g => g.Sum());
				}

				forest.Add(Uno.UI.Extensions.ViewExtensions.TreeGraph(rootContainer, DescribeView));
				static IEnumerable<string> DescribeView(object x)
				{
					if (x is FrameworkElement fe)
					{
						yield return $"TP={PrettyPrint.FormatType(fe.GetTemplatedParent())}, DC={PrettyPrint.FormatObject(fe.DataContext)}";
					}
				}
#endif

				#region Add all children to the tracking
#if WINAPPSDK
				for (int i = 0; i < VisualTreeHelper.GetChildrenCount((FrameworkElement)rootContainer.Content); i++)
				{
					var child = VisualTreeHelper.GetChild((FrameworkElement)rootContainer.Content, i);
#else
				foreach (var child in ((FrameworkElement)rootContainer.Content).EnumerateAllChildren(maxDepth: 200).OfType<UIElement>())
				{
#endif
					TrackDependencyObject(child);

					if (child is FrameworkElement fe)
					{
#if !WINAPPSDK
						// Don't use VisualStateManager.GetVisualStateManager to avoid creating an instance
						if (child.GetValue(VisualStateManager.VisualStateManagerProperty) is VisualStateManager vsm)
#endif
						{
#if !WINAPPSDK
							TrackDependencyObject(vsm);
#endif

							if (VisualStateManager.GetVisualStateGroups(fe) is { } groups)
							{
								foreach (var group in groups)
								{
									TrackDependencyObject(group);

									foreach (var transition in group.Transitions)
									{
										TrackDependencyObject(transition);

										foreach (var timeline in transition.Storyboard.Children)
										{
											TrackDependencyObject(timeline);
										}
									}

									foreach (var state in group.States)
									{
										TrackDependencyObject(state);

										foreach (var setter in state.Setters)
										{
											TrackDependencyObject(setter);
										}

										foreach (var trigger in state.StateTriggers)
										{
											TrackDependencyObject(trigger);
										}
									}
								}
							}
						}
					}
				}
				#endregion

				rootContainer.Content = null;
			}
		}

#if !WINAPPSDK
		[TestMethod]
		public async Task When_Control_Loaded_Then_HardReferences()
		{
			if (FeatureConfiguration.DependencyObject.IsStoreHardReferenceEnabled)
			{
				var root = new Grid();
				var SUT = new Grid();
				root.Children.Add(SUT);

				Assert.IsFalse((SUT as IDependencyObjectStoreProvider).Store.AreHardReferencesEnabled);
				Assert.IsNotNull(SUT.GetParent());

				TestServices.WindowHelper.WindowContent = root;
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue((SUT as IDependencyObjectStoreProvider).Store.AreHardReferencesEnabled);
				Assert.IsNotNull(SUT.GetParent());

				root.Children.Clear();
				Assert.IsFalse((SUT as IDependencyObjectStoreProvider).Store.AreHardReferencesEnabled);
				Assert.IsNull(SUT.GetParent());
			}
		}
#endif

		[Flags]
		public enum LeakTestStyles
		{
			None = 0,
			Fluent = 1,
			Uwp = 2,
			All = Fluent | Uwp
		}
	}
}
