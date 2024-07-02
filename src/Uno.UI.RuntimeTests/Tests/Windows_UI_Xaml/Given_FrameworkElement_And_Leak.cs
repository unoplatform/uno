// Uncomment to get additional reference tracking
// #define TRACK_REFS
#nullable enable

#if !WINAPPSDK
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

#if __MACOS__
using AppKit;
#elif __IOS__
using UIKit;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
#if __MACOS__
	[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
	public class Given_FrameworkElement_And_Leak
	{
		[TestMethod]
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
		[DataRow(typeof(Windows.UI.Xaml.Controls.ProgressBar), 15)]
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.ProgressBar), 15)]
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Windows.UI.Xaml.Controls.ProgressRing), 15)]
#endif
		//[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.ProgressRing), 15)] This leaks, issue #9078
		[DataRow(typeof(Pivot), 15)]
		[DataRow(typeof(ScrollBar), 15)]
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Slider), 15)]
#endif
		[DataRow(typeof(SymbolIcon), 15)]
		[DataRow(typeof(Viewbox), 15)]
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.MenuBar), 15)]
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
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(DatePicker), 15)]
		[DataRow(typeof(TimePicker), 15)]
#endif
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/issues/9080
		[DataRow(typeof(CalendarView), 15)]
#endif
		[DataRow(typeof(Page), 15)]
		[DataRow(typeof(Image), 15)]
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(ToggleSwitch), 15)]
#endif
#if __SKIA__ && HAS_UNO_WINUI // Control is currently supported on Skia targets only.
		[DataRow(typeof(SelectorBar), 15)]
		[DataRow(typeof(SelectorBarItem), 15)]
		[DataRow(typeof(ItemsView), 15)]
		[DataRow(typeof(ScrollView), 15)]
#endif
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeControl), 15)]
		[DataRow(typeof(SplitView), 15)]
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.AnimatedIcon), 15,
#if __ANDROID__
			LeakTestStyles.Default // Fluent styles disabled - #14341
#else
			LeakTestStyles.All
#endif
			)]
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.BreadcrumbBar), 15)]
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.BreadcrumbBarItem), 15)]
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/issues/9080
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.ColorPicker), 15)]
#endif
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives.ColorPickerSlider), 15)]
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives.ColorSpectrum), 15)]
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.Expander), 15)]
#endif
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.ImageIcon), 15)]
#if !WINAPPSDK
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.InfoBadge), 15)]
#endif
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.InfoBar), 15)]
#endif
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives.InfoBarPanel), 15)]
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives.MonochromaticOverlayPresenter), 15)]
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItem), 15)]
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter), 15)]
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationView), 15)]
#endif
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.NumberBox), 15)]
#if !WINAPPSDK
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.PagerControl), 15)]
#endif
#endif
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.PipsPager), 15)]
#endif
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshContainer), 15)]
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.RadioButtons), 15)]
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.RadioMenuFlyoutItem), 15)]
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.RatingControl), 15)]
#endif
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.ItemsRepeater), 15)]
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.SplitButton), 15)]
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.TabView), 15)]
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives.TabViewListView), 15)]
#endif
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeView), 15)]
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow(typeof(Microsoft/* UWP don't rename */.UI.Xaml.Controls.TwoPaneView), 15)]
		[DataRow("SamplesApp.Windows_UI_Xaml.Clipping.XamlButtonWithClipping_Scrollable", 15)]
		[DataRow("Uno.UI.Samples.Content.UITests.ButtonTestsControl.AppBar_KeyBoard", 15)]
		[DataRow("Uno.UI.Samples.Content.UITests.ButtonTestsControl.Buttons", 15)]
#endif
		[DataRow("UITests.Windows_UI_Xaml.xLoadTests.xLoad_Test_For_Leak", 15)]
#if !__IOS__ // Disabled https://github.com/unoplatform/uno/pull/15540
		[DataRow("UITests.Windows_UI_Xaml_Controls.ToolTip.ToolTip_LeakTest", 15)]
#endif
		[DataRow("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.Button_Command_Leak", 15)]
		[DataRow("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.ItemsControl_ItemsSource_Leak", 15)]
#if !__WASM__ && !__IOS__ // Disabled - https://github.com/unoplatform/uno/issues/7860
		[DataRow("Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.ContentDialog_Leak", 15)]
#endif
		[DataRow(typeof(TextBox_Focus_Leak), 15,
#if __IOS__
			LeakTestStyles.None // Disabled - #10344
#else
			LeakTestStyles.All
#endif
			)]
		[DataRow(typeof(PasswordBox_Focus_Leak), 15,
#if __IOS__
			LeakTestStyles.None // Disabled - #10344
#elif __ANDROID__
			LeakTestStyles.Default // Fluent styles disabled - #14340
#else
			LeakTestStyles.All
#endif
			)]
		[DataRow(typeof(MediaPlayerElement), 15)]
		public async Task When_Add_Remove(object controlTypeRaw, int count, LeakTestStyles leakTestStyles = LeakTestStyles.All)
		{
			if (leakTestStyles.HasFlag(LeakTestStyles.Default))
			{
				// Test for leaks both without and with fluent styles
				await When_Add_Remove_Inner(controlTypeRaw, count);
			}

			if (leakTestStyles.HasFlag(LeakTestStyles.Fluent))
			{
				using (var themeHelper = StyleHelper.UseFluentStyles())
				{
					await When_Add_Remove_Inner(controlTypeRaw, count);
				}
			}
		}

		private async Task When_Add_Remove_Inner(object controlTypeRaw, int count)
		{
#if TRACK_REFS
			var initialInactiveStats = Uno.UI.DataBinding.BinderReferenceHolder.GetInactiveViewReferencesStats();
			var initialActiveStats = Uno.UI.DataBinding.BinderReferenceHolder.GetReferenceStats();
#endif

			Type GetType(string s)
				=> AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(s)).Where(t => t != null).First()!;

			var controlType = controlTypeRaw switch
			{
				Type ct => ct,
				string s => GetType(s),
				_ => throw new InvalidOperationException()
			};

			var _holders = new ConditionalWeakTable<DependencyObject, Holder>();
			void TrackDependencyObject(DependencyObject target) => _holders.Add(target, new Holder(HolderUpdate));

			var maxCounter = 0;
			var activeControls = 0;
			var maxActiveControls = 0;

			// Ensure Holder counter is reset between individual control tests.
			Holder.Reset();

			var rootContainer = new ContentControl();

			TestServices.WindowHelper.WindowContent = rootContainer;

			await TestServices.WindowHelper.WaitForIdle();

			for (int i = 0; i < count; i++)
			{
				await MaterializeControl(controlType, _holders, maxCounter, rootContainer);
			}

			TestServices.WindowHelper.WindowContent = null;

			void HolderUpdate(int value)
			{
				_ = rootContainer!.Dispatcher.RunAsync(CoreDispatcherPriority.High,
					() =>
					{
						maxCounter = Math.Max(value, maxCounter);
						activeControls = value;
						maxActiveControls = maxCounter;
					}
				);
			}

			var sw = Stopwatch.StartNew();

			var endTime = TimeSpan.FromSeconds(5);
			var maxTime = TimeSpan.FromSeconds(30);

			var lastActiveControls = activeControls;

			while (sw.Elapsed < endTime && sw.Elapsed < maxTime && activeControls != 0)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				// Waiting for idle is required for collection of
				// DispatcherConditionalDisposable to be executed
				await TestServices.WindowHelper.WaitForIdle();

				if (lastActiveControls != activeControls)
				{
					// Expand the timeout if the count has changed, as the
					// GC may still be processing levels of the hierarcy on iOS
					endTime += TimeSpan.FromMilliseconds(500);
				}

				lastActiveControls = activeControls;
			}

#if TRACK_REFS
			Uno.UI.DataBinding.BinderReferenceHolder.LogInactiveViewReferencesStatsDiff(initialInactiveStats);
			Uno.UI.DataBinding.BinderReferenceHolder.LogActiveViewReferencesStatsDiff(initialActiveStats);
#endif

			var retainedMessage = "";

#if __IOS__ || __ANDROID__
			if (activeControls != 0)
			{
				var retainedTypes = _holders.AsEnumerable().Select(ExtractTargetName).JoinBy(";");
				Console.WriteLine($"Retained types: {retainedTypes}");

				retainedMessage = $"Retained types: {retainedTypes}";
			}
#endif

#if __IOS__
			// On iOS, the collection of objects does not seem to be reliable enough
			// to always go to zero during runtime tests. If the count of active objects
			// is arbitrarily below the half of the number of top-level objects.
			// created, we can assume that enough objects were collected entirely.
			Assert.IsTrue(activeControls < count, retainedMessage);
#else
			Assert.AreEqual(0, activeControls, retainedMessage);
#endif

#if __IOS__ || __ANDROID__
			static string? ExtractTargetName(KeyValuePair<DependencyObject, Holder> p)
			{
				if (p.Key is FrameworkElement fe)
				{
					return $"{fe}/{fe.Name}";
				}
				else
				{
					return p.Key?.ToString();
				}
			}
#endif

			async Task MaterializeControl(Type controlType, ConditionalWeakTable<DependencyObject, Holder> _holders, int maxCounter, ContentControl rootContainer)
			{
				var item = (FrameworkElement)Activator.CreateInstance(controlType)!;
				TrackDependencyObject(item);
				rootContainer.Content = item;
				await TestServices.WindowHelper.WaitForIdle();

				if (item is IExtendedLeakTest extendedTest)
				{
					await extendedTest.WaitForTestToComplete();
				}

				// Add all children to the tracking
				foreach (var child in item.EnumerateAllChildren(maxDepth: 200).OfType<UIElement>())
				{
					TrackDependencyObject(child);

					if (child is FrameworkElement fe)
					{
						// Don't use VisualStateManager.GetVisualStateManager to avoid creating an instance
						if (child.GetValue(VisualStateManager.VisualStateManagerProperty) is VisualStateManager vsm)
						{
							TrackDependencyObject(vsm);

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

				rootContainer.Content = null;
				GC.Collect();
				GC.WaitForPendingFinalizers();

				// Waiting for idle is required for collection of
				// DispatcherConditionalDisposable to be executed
				await TestServices.WindowHelper.WaitForIdle();
			}
		}

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
				var counter = Interlocked.Decrement(ref _counter);
				_update(counter);
			}

			public static void Reset() => _counter = 0;
		}

		[Flags]
		public enum LeakTestStyles
		{
			None = 0,
			Default = 1,
			Fluent = 2,
			All = Default | Fluent
		}
	}
}
#endif
