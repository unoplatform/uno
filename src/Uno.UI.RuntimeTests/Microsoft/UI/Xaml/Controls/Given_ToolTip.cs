using System;
using Microsoft.UI;
using System.Linq;
using System.Reflection;
using Windows.UI;
using System.Threading.Tasks;
using Windows.UI.Input.Preview.Injection;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.DevTools.Input;
using Uno.Extensions;
using Uno.UI.Extensions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ToolTip
	{
#if HAS_UNO // ToolTipService.GetToolTipReference is internal
		[TestMethod]
		public async Task When_String_ToolTip_Changes_Then_Wrapper_Reused()
		{
			var owner = new Border { Width = 50, Height = 50 };
			await UITestHelper.Load(owner);

			ToolTipService.SetToolTip(owner, "first");
			var wrapper = ToolTipService.GetToolTipReference(owner);
			Assert.IsNotNull(wrapper);
			Assert.AreEqual("first", wrapper.Content);
			var subscriptions = wrapper.OwnerEventSubscriptions;
			Assert.IsNotNull(subscriptions);

			ToolTipService.SetToolTip(owner, "second");

			Assert.AreSame(wrapper, ToolTipService.GetToolTipReference(owner), "The implicit wrapper should be reused");
			Assert.AreEqual("second", wrapper.Content);
			Assert.AreSame(subscriptions, wrapper.OwnerEventSubscriptions, "The owner subscriptions should be kept");

			try
			{
				// A change of value closes an open tooltip, as replacing the wrapper did.
				wrapper.IsOpen = true;
				await UITestHelper.WaitForIdle();

				ToolTipService.SetToolTip(owner, "third");

				Assert.IsFalse(wrapper.IsOpen, "Changing the value should close the tooltip");
				Assert.AreSame(wrapper, ToolTipService.GetActualToolTipObject(owner));
				Assert.AreEqual("third", wrapper.Content);
			}
			finally
			{
				Microsoft.UI.Xaml.Media.VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
			}
		}

		[TestMethod]
		public async Task When_String_ToolTip_Replaced_By_ToolTip()
		{
			var owner = new Border { Width = 50, Height = 50 };
			await UITestHelper.Load(owner);

			ToolTipService.SetToolTip(owner, "text");
			var wrapper = ToolTipService.GetToolTipReference(owner);
			Assert.IsNotNull(wrapper);

			var explicitToolTip = new ToolTip { Content = "explicit" };
			ToolTipService.SetToolTip(owner, explicitToolTip);

			Assert.AreSame(explicitToolTip, ToolTipService.GetToolTipReference(owner));
			Assert.IsNull(wrapper.OwnerEventSubscriptions, "The discarded wrapper should be unsubscribed");
			Assert.IsNotNull(explicitToolTip.OwnerEventSubscriptions);

			// Going back to a string from an explicit ToolTip creates a new wrapper; the explicit one is not mutated.
			ToolTipService.SetToolTip(owner, "text again");
			var newWrapper = ToolTipService.GetToolTipReference(owner);
			Assert.AreNotSame(explicitToolTip, newWrapper);
			Assert.AreEqual("text again", newWrapper.Content);
			Assert.AreEqual("explicit", explicitToolTip.Content);
			Assert.IsNull(explicitToolTip.OwnerEventSubscriptions);
		}

		[TestMethod]
		public async Task When_String_ToolTip_Set_To_Null()
		{
			var owner = new Border { Width = 50, Height = 50 };
			await UITestHelper.Load(owner);

			ToolTipService.SetToolTip(owner, "text");
			var wrapper = ToolTipService.GetToolTipReference(owner);
			Assert.IsNotNull(wrapper);

			ToolTipService.SetToolTip(owner, null);

			Assert.IsNull(ToolTipService.GetToolTipReference(owner));
			Assert.IsNull(wrapper.OwnerEventSubscriptions);

			// From null, a value gets a new wrapper.
			ToolTipService.SetToolTip(owner, "back");
			var newWrapper = ToolTipService.GetToolTipReference(owner);
			Assert.IsNotNull(newWrapper);
			Assert.AreEqual("back", newWrapper.Content);
			Assert.IsNotNull(newWrapper.OwnerEventSubscriptions);
		}

		[TestMethod]
		public async Task When_Element_ToolTip_Swapped_For_String_And_Back_Then_Wrapper_Reused()
		{
			var owner = new Border { Width = 50, Height = 50 };
			await UITestHelper.Load(owner);

			var element = new TextBlock { Text = "element" };
			ToolTipService.SetToolTip(owner, element);
			var wrapper = ToolTipService.GetToolTipReference(owner);
			Assert.IsNotNull(wrapper);
			Assert.AreSame(element, wrapper.Content);

			ToolTipService.SetToolTip(owner, "text");
			Assert.AreSame(wrapper, ToolTipService.GetToolTipReference(owner));
			Assert.AreEqual("text", wrapper.Content);
			Assert.AreNotSame(wrapper, element.GetParent(), "The element replaced in the wrapper should no longer be parented by it");

			ToolTipService.SetToolTip(owner, element);
			Assert.AreSame(wrapper, ToolTipService.GetToolTipReference(owner));
			Assert.AreSame(element, wrapper.Content, "The element should be back in the tooltip");
		}

		[TestMethod]
		public async Task When_Element_ToolTip_Shared_By_Two_Owners_Then_Change_On_One_Does_Not_Affect_The_Other()
		{
			var owner1 = new Border { Width = 50, Height = 50 };
			var owner2 = new Border { Width = 50, Height = 50 };
			await UITestHelper.Load(new StackPanel { Children = { owner1, owner2 } });

			var element = new TextBlock { Text = "shared" };
			ToolTipService.SetToolTip(owner1, element);
			var shared = ToolTipService.GetToolTipReference(owner1);
			Assert.IsNotNull(shared);

			// ConvertToToolTip hands a second owner the ToolTip that is the element's parent (as in WinUI). In Uno the
			// content's reported parent is the template's ContentPresenter (or null before the first open), so that
			// branch is only reached when something sets the parent; set it here to exercise it.
			element.SetParent(shared);
			ToolTipService.SetToolTip(owner2, element);
			Assert.AreSame(shared, ToolTipService.GetToolTipReference(owner2), "premise: the ToolTip is shared");
			Assert.IsFalse(shared.IsImplicitToolTip, "A shared ToolTip must not be reused in place");

			ToolTipService.SetToolTip(owner2, "own");

			Assert.AreSame(shared, ToolTipService.GetToolTipReference(owner1));
			Assert.AreSame(element, shared.Content, "owner1's tooltip should still show the element");
			var owner2ToolTip = ToolTipService.GetToolTipReference(owner2);
			Assert.AreNotSame(shared, owner2ToolTip);
			Assert.AreEqual("own", owner2ToolTip.Content);
		}

		[TestMethod]
		public async Task When_Element_Parented_By_Explicit_ToolTip_Then_That_ToolTip_Is_Used()
		{
			var owner = new Border { Width = 50, Height = 50 };
			await UITestHelper.Load(owner);

			var element = new TextBlock { Text = "inside" };
			var explicitToolTip = new ToolTip { Content = element };
			// See When_Element_ToolTip_Shared_By_Two_Owners_...: Uno does not report the ToolTip as the content's parent.
			element.SetParent(explicitToolTip);

			ToolTipService.SetToolTip(owner, "text");
			var wrapper = ToolTipService.GetToolTipReference(owner);

			ToolTipService.SetToolTip(owner, element);

			Assert.AreSame(explicitToolTip, ToolTipService.GetToolTipReference(owner));
			Assert.AreEqual("text", wrapper.Content, "The previous wrapper should not have taken the element");
			Assert.IsNull(wrapper.OwnerEventSubscriptions);

			ToolTipService.SetToolTip(owner, "again");
			var newWrapper = ToolTipService.GetToolTipReference(owner);
			Assert.AreNotSame(explicitToolTip, newWrapper);
			Assert.AreSame(element, explicitToolTip.Content, "The explicit ToolTip must not be changed");
		}

		[TestMethod]
		public async Task When_KeyboardAccelerator_ToolTip_Changes_Then_Wrapper_Reused_And_Public_ToolTip_Untouched()
		{
			var owner = new Button { Content = "Owner" };
			await UITestHelper.Load(owner);

			ToolTipService.SetToolTip(owner, "tip");
			var publicToolTip = ToolTipService.GetToolTipReference(owner);

			ToolTipService.SetKeyboardAcceleratorToolTip(owner, "Ctrl+A");
			var acceleratorToolTip = ToolTipService.GetKeyboardAcceleratorToolTipObject(owner);
			Assert.IsNotNull(acceleratorToolTip);
			Assert.AreNotSame(publicToolTip, acceleratorToolTip);

			ToolTipService.SetKeyboardAcceleratorToolTip(owner, "Ctrl+B");

			Assert.AreSame(acceleratorToolTip, ToolTipService.GetKeyboardAcceleratorToolTipObject(owner));
			Assert.AreEqual("Ctrl+B", acceleratorToolTip.Content);
			Assert.AreSame(publicToolTip, ToolTipService.GetToolTipReference(owner));
			Assert.AreEqual("tip", publicToolTip.Content);

			ToolTipService.SetToolTip(owner, "tip 2");
			Assert.AreSame(publicToolTip, ToolTipService.GetToolTipReference(owner));
			Assert.AreSame(acceleratorToolTip, ToolTipService.GetKeyboardAcceleratorToolTipObject(owner));
			Assert.AreEqual("Ctrl+B", acceleratorToolTip.Content);
		}

		[TestMethod]
		public async Task When_PlacementTarget_Set_Between_Changes_Then_Reused_Wrapper_Takes_It()
		{
			var owner = new Border { Width = 50, Height = 50 };
			var target = new Border { Width = 50, Height = 50 };
			await UITestHelper.Load(new StackPanel { Children = { owner, target } });

			ToolTipService.SetToolTip(owner, "a");
			var wrapper = ToolTipService.GetToolTipReference(owner);
			Assert.AreSame(owner, AnchorOf(wrapper));

			ToolTipService.SetPlacementTarget(owner, target);
			ToolTipService.SetToolTip(owner, "b");

			Assert.AreSame(wrapper, ToolTipService.GetToolTipReference(owner));
			Assert.AreSame(target, AnchorOf(wrapper), "The reused wrapper should be anchored to the new PlacementTarget");
		}

		[TestMethod]
		public async Task When_Placement_Set_On_Owner_Then_It_Survives_A_Value_Change()
		{
			var owner = new Border { Width = 50, Height = 50 };
			await UITestHelper.Load(owner);

			ToolTipService.SetToolTip(owner, "a");
			var wrapper = ToolTipService.GetToolTipReference(owner);
			ToolTipService.SetPlacement(owner, PlacementMode.Bottom);
			Assert.AreEqual(PlacementMode.Bottom, wrapper.Placement);

			ToolTipService.SetToolTip(owner, "b");

			// Replacing the wrapper reset this to Top; the reused wrapper keeps what the owner asked for.
			Assert.AreEqual(PlacementMode.Bottom, wrapper.Placement);
		}

		[TestMethod]
		public async Task When_Closed_Handler_Changes_Value_Then_Nested_Value_Wins()
		{
			var owner = new Border { Width = 50, Height = 50 };
			await UITestHelper.Load(owner);

			ToolTipService.SetToolTip(owner, "a");
			var wrapper = ToolTipService.GetToolTipReference(owner);

			var nestedDone = false;
			wrapper.Closed += (_, _) =>
			{
				if (!nestedDone)
				{
					nestedDone = true;
					ToolTipService.SetToolTip(owner, "nested");
				}
			};

			try
			{
				wrapper.IsOpen = true;
				await UITestHelper.WaitForIdle();

				ToolTipService.SetToolTip(owner, "outer");

				Assert.IsTrue(nestedDone, "premise: the Closed handler ran");
				Assert.AreEqual("nested", ToolTipService.GetToolTip(owner));
				Assert.AreEqual("nested", wrapper.Content, "The outer change must not overwrite the nested one");
			}
			finally
			{
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
			}
		}

		[TestMethod]
		public async Task When_UseToolTips_Turned_Off_Then_Value_Change_Unregisters()
		{
			var owner = new Border { Width = 50, Height = 50 };
			await UITestHelper.Load(owner);

			ToolTipService.SetToolTip(owner, "a");
			var wrapper = ToolTipService.GetToolTipReference(owner);
			Assert.IsNotNull(wrapper);

			var previous = FeatureConfiguration.ToolTip.UseToolTips;
			FeatureConfiguration.ToolTip.UseToolTips = false;
			try
			{
				ToolTipService.SetToolTip(owner, "b");

				Assert.IsNull(ToolTipService.GetToolTipReference(owner), "With tooltips off, a value change leaves no tooltip, as before");
				Assert.IsNull(wrapper.OwnerEventSubscriptions);
			}
			finally
			{
				FeatureConfiguration.ToolTip.UseToolTips = previous;
			}
		}

		[TestMethod]
		public async Task When_Owner_Unloaded_And_Reloaded_Between_Changes_Then_Wrapper_Reused()
		{
			var host = new StackPanel();
			var owner = new Border { Width = 50, Height = 50 };
			host.Children.Add(owner);
			await UITestHelper.Load(host);

			ToolTipService.SetToolTip(owner, "a");
			var wrapper = ToolTipService.GetToolTipReference(owner);
			var subscriptions = wrapper.OwnerEventSubscriptions;

			host.Children.Remove(owner);
			await UITestHelper.WaitForIdle();
			ToolTipService.SetToolTip(owner, "b");
			host.Children.Add(owner);
			await TestServices.WindowHelper.WaitForLoaded(owner);

			Assert.AreSame(wrapper, ToolTipService.GetToolTipReference(owner));
			Assert.AreSame(subscriptions, wrapper.OwnerEventSubscriptions);
			Assert.AreEqual("b", wrapper.Content);
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Value_Changes_While_Open_Is_Pending_Then_ToolTip_Does_Not_Open()
		{
			var owner = new Button { Content = "Owner", Width = 120, Height = 40 };
			ToolTipService.SetToolTip(owner, "a");
			await UITestHelper.Load(owner);
			var wrapper = ToolTipService.GetToolTipReference(owner);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			try
			{
				mouse.MoveTo(owner.GetAbsoluteBoundsRect().GetCenter());
				await UITestHelper.WaitForIdle();
				Assert.AreSame(wrapper, ToolTipService.CurrentToolTipForTesting, "premise: the open is pending");

				ToolTipService.SetToolTip(owner, "b");

				Assert.IsNull(ToolTipService.CurrentToolTipForTesting, "The pending open should be cancelled");
				await Task.Delay(TimeSpan.FromMilliseconds(FeatureConfiguration.ToolTip.ShowDelay + 300));
				Assert.IsFalse(wrapper.IsOpen, "The tooltip should not open after the value changed");
			}
			finally
			{
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
				mouse.MoveBy(0, 200);
				await UITestHelper.WaitForIdle();
			}
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Value_Changes_While_Open_Then_Closes_And_Reopens_Once_With_New_Content()
		{
			var owner = new Button { Content = "Owner", Width = 120, Height = 40, DataContext = "dc1" };
			ToolTipService.SetToolTip(owner, "a");
			await UITestHelper.Load(owner);
			var wrapper = ToolTipService.GetToolTipReference(owner);

			var opened = 0;
			wrapper.Opened += (_, _) => opened++;

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();
			var center = owner.GetAbsoluteBoundsRect().GetCenter();

			try
			{
				mouse.MoveTo(center);
				await UITestHelper.WaitFor(() => wrapper.IsOpen, timeoutMS: 5000, message: "premise: the tooltip opened on hover");

				// Several changes, as a recycled row would get.
				ToolTipService.SetToolTip(owner, "b");
				ToolTipService.SetToolTip(owner, "c");
				ToolTipService.SetToolTip(owner, "d");
				Assert.IsFalse(wrapper.IsOpen, "A value change should close the open tooltip");
				Assert.IsNull(ToolTipService.CurrentToolTipForTesting);

				owner.DataContext = "dc2";
				mouse.MoveBy(0, 200);
				await UITestHelper.WaitForIdle();
				opened = 0;
				mouse.MoveTo(center);
				await UITestHelper.WaitFor(() => wrapper.IsOpen, timeoutMS: 5000, message: "The tooltip did not open again on hover");
				await Task.Delay(300);

				Assert.AreEqual(1, opened, "The owner handlers should open the tooltip once, not once per value change");
				Assert.AreEqual("d", wrapper.Content);
				Assert.AreEqual("dc2", wrapper.DataContext);
			}
			finally
			{
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
				mouse.MoveBy(0, 200);
				await UITestHelper.WaitForIdle();
			}
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_String_ToolTip_Changes_Then_Same_ToolTip_Is_Shown()
		{
			// Public API only: the ToolTip shown in the popup is the same instance before and after a value change.
			var owner = new Button { Content = "Owner", Width = 120, Height = 40 };
			ToolTipService.SetToolTip(owner, "a");
			await UITestHelper.Load(owner);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();
			var center = owner.GetAbsoluteBoundsRect().GetCenter();

			async Task<ToolTip> HoverAndGetShownToolTip()
			{
				mouse.MoveTo(center);
				ToolTip shown = null;
				await UITestHelper.WaitFor(
					() => (shown = VisualTreeHelper.GetOpenPopupsForXamlRoot(owner.XamlRoot).Select(p => p.Child).OfType<ToolTip>().FirstOrDefault()) is not null,
					timeoutMS: 5000,
					message: "The tooltip did not open on hover");
				mouse.MoveBy(0, 200);
				await UITestHelper.WaitForIdle();
				return shown;
			}

			try
			{
				var first = await HoverAndGetShownToolTip();
				Assert.AreEqual("a", first.Content);

				ToolTipService.SetToolTip(owner, "b");

				var second = await HoverAndGetShownToolTip();
				Assert.AreEqual("b", second.Content);
				Assert.AreSame(first, second, "The value change should reuse the ToolTip rather than create a new one");
			}
			finally
			{
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
				mouse.MoveBy(0, 200);
				await UITestHelper.WaitForIdle();
			}
		}
#endif

		[TestMethod]
		public async Task When_DataContext_Set_On_ToolTip_Owner()
		{
			try
			{
				var textBlock = new TextBlock();
				var SUT = new ToolTip();
				ToolTipService.SetToolTip(textBlock, SUT);
				var stackPanel = new StackPanel
				{
					Children =
					{
						textBlock,
					}
				};

				TestServices.WindowHelper.WindowContent = stackPanel;
				await TestServices.WindowHelper.WaitForIdle();

				stackPanel.DataContext = "DataContext1";

				Assert.AreEqual("DataContext1", textBlock.DataContext);
				Assert.AreEqual("DataContext1", SUT.DataContext);

				SUT.IsOpen = true;

				stackPanel.DataContext = "DataContext2";

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("DataContext2", textBlock.DataContext);
				Assert.AreEqual("DataContext2", SUT.DataContext);
			}
			finally
			{
#if HAS_UNO
				Microsoft.UI.Xaml.Media.VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			}
		}

		[TestMethod]
		public async Task When_ToggleButton_DataContext_Set_On_ToolTip_Owner_After()
		{
			try
			{
				var toggleButton = new ToggleButton();

				var textBlock = new TextBlock();
				textBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new(".") });

				var SUT = new ToolTip() { Content = textBlock };
				ToolTipService.SetToolTip(toggleButton, SUT);

				var stackPanel = new StackPanel
				{
					Children =
					{
						toggleButton,
					}
				};

				TestServices.WindowHelper.WindowContent = stackPanel;
				await TestServices.WindowHelper.WaitForIdle();

				stackPanel.DataContext = "DataContext1";

				Assert.AreEqual("DataContext1", toggleButton.DataContext);
				Assert.AreEqual("DataContext1", SUT.DataContext);

				SUT.IsOpen = true;

				stackPanel.DataContext = "DataContext2";

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("DataContext2", toggleButton.DataContext);
				Assert.AreEqual("DataContext2", SUT.DataContext);
				Assert.AreEqual("DataContext2", textBlock.DataContext);
			}
			finally
			{
#if HAS_UNO
				Microsoft.UI.Xaml.Media.VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			}
		}

		[TestMethod]
		public async Task When_ToggleButton_DataContext_Set_On_ToolTip_Owner_Before()
		{
			try
			{
				var toggleButton = new ToggleButton();

				var textBlock = new TextBlock();
				textBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new(".") });

				var SUT = new ToolTip() { Content = textBlock };
				ToolTipService.SetToolTip(toggleButton, SUT);

				var stackPanel = new StackPanel
				{
					Children =
					{
						toggleButton,
					}
				};

				TestServices.WindowHelper.WindowContent = stackPanel;
				await TestServices.WindowHelper.WaitForIdle();

				stackPanel.DataContext = "DataContext1";

				Assert.AreEqual("DataContext1", toggleButton.DataContext);
				Assert.AreEqual("DataContext1", SUT.DataContext);

				// Set the datacontext before opening
				stackPanel.DataContext = "DataContext2";

				SUT.IsOpen = true;

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("DataContext2", toggleButton.DataContext);
				Assert.AreEqual("DataContext2", SUT.DataContext);
				Assert.AreEqual("DataContext2", textBlock.DataContext);

				stackPanel.DataContext = "DataContext3";

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("DataContext3", toggleButton.DataContext);
				Assert.AreEqual("DataContext3", SUT.DataContext);
				Assert.AreEqual("DataContext3", textBlock.DataContext);

				SUT.IsOpen = false;

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				stackPanel.DataContext = "DataContext4";

				SUT.IsOpen = true;

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("DataContext4", toggleButton.DataContext);
				Assert.AreEqual("DataContext4", SUT.DataContext);
				Assert.AreEqual("DataContext4", textBlock.DataContext);
			}
			finally
			{
#if HAS_UNO
				Microsoft.UI.Xaml.Media.VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			}
		}

		[TestMethod]
		public async Task When_ToggleButton_DataContext_Set_On_ToolTip_Owner_Nested()
		{
			try
			{
				var toggleButton = new ToggleButton();

				var textBlock = new TextBlock();
				textBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new(".") });

				var innerStackPanel = new StackPanel();
				innerStackPanel.Children.Add(textBlock);

				var SUT = new ToolTip() { Content = innerStackPanel };
				ToolTipService.SetToolTip(toggleButton, SUT);

				var stackPanel = new StackPanel
				{
					Children =
					{
						toggleButton,
					}
				};

				TestServices.WindowHelper.WindowContent = stackPanel;
				await TestServices.WindowHelper.WaitForIdle();

				SUT.DataContextChanged += (s, e) =>
				{
				};

				stackPanel.DataContext = "DataContext1";

				Assert.AreEqual("DataContext1", toggleButton.DataContext);
				Assert.AreEqual("DataContext1", SUT.DataContext);

				SUT.IsOpen = true;

				stackPanel.DataContext = "DataContext2";

				// This might not be needed when ToolTips are ported from WinUI?
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual("DataContext2", toggleButton.DataContext);
				Assert.AreEqual("DataContext2", SUT.DataContext);
				Assert.AreEqual("DataContext2", textBlock.DataContext);
			}
			finally
			{
#if HAS_UNO
				Microsoft.UI.Xaml.Media.VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public Task When_Switch_Theme_Fluent() => When_Switch_Theme_Inner(brush => (brush as AcrylicBrush).TintColor);

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Switch_Theme_Uwp()
		{
			using var _ = StyleHelper.UseUwpStyles();
			await When_Switch_Theme_Inner(brush => (brush as SolidColorBrush).Color);
		}

		private async Task When_Switch_Theme_Inner(Func<Brush, Color> backgroundColorGetter)
		{
			try
			{
				var textBlock = new TextBlock() { Text = "Test" };
				var SUT = new ToolTip()
				{
					Content = "I'm a ToolTip!"
				};
				ToolTipService.SetToolTip(textBlock, SUT);
				var stackPanel = new StackPanel
				{
					Children =
					{
						textBlock,
					}
				};

				TestServices.WindowHelper.WindowContent = stackPanel;
				await TestServices.WindowHelper.WaitForIdle();

				SUT.IsOpen = true;
				await TestServices.WindowHelper.WaitForIdle();
				await Task.Delay(1000);
				await TestServices.WindowHelper.WaitForIdle();

				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(textBlock.XamlRoot);
				var popup = popups[0];
				var toolTipChild = popup.Child as ToolTip;
				Assert.AreEqual(SUT, toolTipChild);
				var color = backgroundColorGetter(toolTipChild.Background);
				Assert.IsTrue(color.R > 100 && color.G > 100 && color.B > 100);

				using var _ = ThemeHelper.UseDarkTheme();

				await TestServices.WindowHelper.WaitForIdle();
				color = backgroundColorGetter(toolTipChild.Background);
				Assert.IsTrue(color.R < 100 && color.G < 100 && color.B < 100);
			}
			finally
			{
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			}
		}

#if HAS_UNO
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_ToolTip_Dismissed_On_PointerCanceled()
		{
			var border = new Border() { Margin = new Thickness(30), Width = 30, Height = 30, Background = new SolidColorBrush(Colors.Red) };
			var sv = new ScrollViewer
			{
				Content = new StackPanel
				{
					Height = 1000,
					Children =
					{
						border.Apply(tb => ToolTipService.SetToolTip(tb, new ToolTip { Content = "Simple ToolTip 1" }))
					}
				}
			};

			await UITestHelper.Load(sv);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			finger.Press(border.GetAbsoluteBoundsRect().GetCenter());
			await Task.Delay(TimeSpan.FromMilliseconds(FeatureConfiguration.ToolTip.ShowDelay + 300));
			Assert.HasCount(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(border.XamlRoot));

			finger.MoveBy(0, -50);
			await UITestHelper.WaitForIdle();
			Assert.IsEmpty(VisualTreeHelper.GetOpenPopupsForXamlRoot(border.XamlRoot));
		}
#endif

		[TestMethod]
		public async Task When_ToolTip_Popup_XamlRoot()
		{
			var toolTip = new ToolTip();
			try
			{
				var host = new Button() { Content = "Asd" };
				toolTip.Content = new Button() { Content = "Test" };

				TestServices.WindowHelper.WindowContent = host;
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForLoaded(host);

				ToolTipService.SetToolTip(host, toolTip);
				toolTip.IsOpen = true;

				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(host.XamlRoot, toolTip.XamlRoot);
				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot);
				Assert.AreEqual(host.XamlRoot, popups[0].XamlRoot);
				Assert.AreEqual(host.XamlRoot, popups[0].Child.XamlRoot);
			}
			finally
			{
				toolTip.IsOpen = false;
			}
		}
#if HAS_UNO
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[TestMethod]
		public async Task When_Programmatic_Focus_Does_Not_Show_ToolTip_At_Pointer_Position()
		{
			var button = new Button
			{
				Content = "Button with tooltip",
				Width = 200,
				Height = 50,
			};
			var tooltip = new ToolTip
			{
				Content = "ToolTip content",
			};
			ToolTipService.SetToolTip(button, tooltip);

			// A separate element to tap on, positioned far from the button.
			var tapTarget = new Border
			{
				Width = 200,
				Height = 200,
				Background = new SolidColorBrush(Colors.Transparent),
			};

			var stackPanel = new StackPanel
			{
				Spacing = 100,
				Children = { button, tapTarget },
			};

			await UITestHelper.Load(stackPanel);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			// Tap the far-away target to set the last pointer position away from the button.
			var tapTargetCenter = tapTarget.GetAbsoluteBoundsRect().GetCenter();
			mouse.Press(tapTargetCenter);
			await UITestHelper.WaitForIdle();
			mouse.Release();
			await UITestHelper.WaitForIdle();

			// Programmatically focus the button - this should NOT open the tooltip.
			button.Focus(FocusState.Programmatic);
			await Task.Delay(TimeSpan.FromMilliseconds(FeatureConfiguration.ToolTip.ShowDelay + 500));
			await UITestHelper.WaitForIdle();

			Assert.IsFalse(tooltip.IsOpen, "ToolTip should not open on programmatic focus.");
		}

#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[TestMethod]
		public async Task When_Keyboard_Focus_Shows_ToolTip_Above_Button()
		{
			try
			{
				var button = new Button
				{
					Content = "Button with tooltip",
					Width = 200,
					Height = 50,
				};
				var tooltip = new ToolTip
				{
					Content = "ToolTip content",
				};
				ToolTipService.SetToolTip(button, tooltip);

				// A separate focusable element placed far below the button.
				var tapTarget = new Button
				{
					Content = "Tap here first",
					Width = 200,
					Height = 50,
				};

				var stackPanel = new StackPanel
				{
					Spacing = 300,
					Children = { button, tapTarget },
				};

				await UITestHelper.Load(stackPanel);

				var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
				using var mouse = injector.GetMouse();

				// Click the far-away target to set the last pointer position away from the button.
				var tapTargetCenter = tapTarget.GetAbsoluteBoundsRect().GetCenter();
				mouse.Press(tapTargetCenter);
				await UITestHelper.WaitForIdle();
				mouse.Release();
				await UITestHelper.WaitForIdle();

				// Tab from tapTarget to the button (Shift+Tab since button is before tapTarget).
				await TestServices.KeyboardHelper.ShiftTab();
				await Task.Delay(TimeSpan.FromMilliseconds(FeatureConfiguration.ToolTip.ShowDelay + 500));
				await UITestHelper.WaitForIdle();

				Assert.IsTrue(tooltip.IsOpen, "ToolTip should open on keyboard focus.");

				var buttonBounds = button.GetAbsoluteBoundsRect();
				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(button.XamlRoot);
				Assert.IsTrue(popups.Count > 0, "Expected at least one open popup.");

				var tooltipPopup = popups[0];

				// The tooltip should be positioned near the button (above it), not near the tap target.
				// With keyboard input mode, placement falls back to Top, so the popup's vertical
				// offset should be near or above the button's top edge, not near tapTargetCenter.Y.
				Assert.IsTrue(
					tooltipPopup.VerticalOffset < tapTargetCenter.Y - 50,
					$"ToolTip popup should be positioned above the button (near Y={buttonBounds.Top}), " +
					$"not near the tap position (Y={tapTargetCenter.Y}). Actual VerticalOffset={tooltipPopup.VerticalOffset}");
			}
			finally
			{
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
			}
		}
#endif

#if HAS_UNO
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[TestMethod]
		public async Task When_ToolTip_Owner_Clicked()
		{
			Button button = new Button()
			{
				Content = "Click when tooltip is visible",
			};
			ToolTip tooltip = new ToolTip
			{
				Content = "Tooltip should disappear when button is clicked!",
			};
			ToolTipService.SetToolTip(button, tooltip);
			await UITestHelper.Load(button);
			tooltip.IsOpen = true;
			await UITestHelper.WaitForIdle();
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();
			mouse.Press(button.GetAbsoluteBoundsRect().GetCenter());
			await UITestHelper.WaitForIdle();
			mouse.Release();
			await UITestHelper.WaitForIdle();
			Assert.IsFalse(tooltip.IsOpen);
		}
#endif

		// ---------------------------------------------------------------------------
		// {ThemeResource} inheritance for popup-hosted (ToolTip) content — kahua #484.
		//
		// Verifies that a {ThemeResource} used inside ToolTip (popup-hosted) content resolves
		// against the content's own inherited ActualTheme — not the ambient/application theme.
		//
		// A label's foreground is a {ThemeResource} whose theme-keyed brush is declared in the
		// surrounding view's themed ResourceDictionary (Light/Dark sub-dicts). When the label is
		// shown in a ToolTip from a Light subtree under a Dark application, the label's ActualTheme
		// is correctly Light, but the {ThemeResource} can wrongly resolve the Dark sub-dictionary
		// value — so the label renders with the wrong theme's color (e.g. a light-on-light
		// foreground that is invisible).
		//
		// The OS-vs-app mismatch is reproduced on Uno by pinning the application theme to Dark
		// (ThemeHelper.UseApplicationDarkTheme, #if HAS_UNO — it relies on the Uno-internal
		// SetExplicitRequestedTheme) and placing a host that pins RequestedTheme=Light. On native WinUI
		// the app-theme pin is unavailable, so the test runs as a Light-host baseline confirming the
		// WinUI-correct value (Green); WinUI never exhibits the regression, so it still validates the
		// behavior the Uno fix must match. The host's ToolTip presents a
		// TextBlock whose Foreground is {ThemeResource LabelForegroundBrush}, declared inline in the
		// same XAML so it parses inside the host's resource scope (a standalone XamlReader.Load of a
		// {ThemeResource} fragment throws on WinUI). ToolTip content is hosted in the PopupRoot,
		// reparented out of the owner's scope.
		//
		// WinUI-correct behavior: the ToolTip label's {ThemeResource} resolves against the owner's
		// inherited theme (Light), so the brush evaluates to the Light sentinel (Green) — even though
		// the application theme is Dark. Uno regression: the popup-presented label resolves the
		// {ThemeResource} against the global/application active theme (Dark), evaluating to the Dark
		// sentinel (Red), despite the label's ActualTheme correctly being Light.
		//
		// The assertion is on a single robust scenario (Light host, app pinned Dark on Uno) where the
		// element's ActualTheme is verifiably Light but the resolved brush value reveals which theme
		// the {ThemeResource} was resolved against. The expected value (Green) is identical on Skia
		// Desktop and native WinUI; only the app-level mismatch differs (forced Dark on Uno, default on
		// WinUI, as noted above).
		// ---------------------------------------------------------------------------

		// One XAML document: a RequestedTheme=Light host whose Resources declare a theme-keyed sentinel
		// brush (Light=Green, Dark=Red), an owner Border carrying a ToolTip, and the ToolTip's label using
		// the brush via {ThemeResource}. Mirrors a view declaring its own label brushes in a themed
		// ResourceDictionary.
		private static Border CreateThemeResourceLightHost()
			=> (Border)XamlReader.Load(
				"""
				<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
						RequestedTheme="Light">
					<Border.Resources>
						<ResourceDictionary>
							<ResourceDictionary.ThemeDictionaries>
								<ResourceDictionary x:Key="Light">
									<SolidColorBrush x:Key="LabelForegroundBrush" Color="Green" />
								</ResourceDictionary>
								<ResourceDictionary x:Key="Dark">
									<SolidColorBrush x:Key="LabelForegroundBrush" Color="Red" />
								</ResourceDictionary>
							</ResourceDictionary.ThemeDictionaries>
						</ResourceDictionary>
					</Border.Resources>
					<Border x:Name="Owner" Width="120" Height="40">
						<ToolTipService.ToolTip>
							<ToolTip>
								<TextBlock Text="Label"
										Foreground="{ThemeResource LabelForegroundBrush}" />
							</ToolTip>
						</ToolTipService.ToolTip>
					</Border>
				</Border>
				""");

		private static Color? GetThemeResourceForegroundColor(TextBlock textBlock)
			=> (textBlock.Foreground as SolidColorBrush)?.Color;

		// A Light host under a Dark application (the literal OS-Dark / app-Light mismatch). The ToolTip
		// label's ActualTheme is Light, so its {ThemeResource} must resolve the host's Light sentinel
		// (Green). On Uno the popup-presented label resolves the {ThemeResource} against the
		// application/global Dark theme, producing the Dark sentinel (Red).
		[TestMethod]
		[RequiresFullWindow]
		[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/484")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // Owner-subtree theme override is Skia-only; WinUI honors OS/app theme only
		public async Task When_ToolTip_Label_Uses_Owner_Subtree_Theme_Light_Under_Dark_App()
		{
#if HAS_UNO
			using var darkApp = ThemeHelper.UseApplicationDarkTheme();
			await TestServices.WindowHelper.WaitForIdle();
#endif

			var host = CreateThemeResourceLightHost();
			var owner = (Border)host.FindName("Owner");
			var tooltip = (ToolTip)ToolTipService.GetToolTip(owner);
			var label = (TextBlock)tooltip.Content;

			var root = new Border { Child = host };

			try
			{
				TestServices.WindowHelper.WindowContent = root;
				await TestServices.WindowHelper.WaitForLoaded(root);

				tooltip.IsOpen = true;
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(ElementTheme.Light, owner.ActualTheme, "Owner should be in the Light subtree.");
				Assert.AreEqual(ElementTheme.Light, label.ActualTheme, "ToolTip label should inherit the owner's Light theme.");

				var foreground = GetThemeResourceForegroundColor(label);
				Assert.IsNotNull(foreground, "ToolTip label should have resolved a SolidColorBrush foreground.");

				Assert.AreEqual(Colors.Green, foreground.Value,
					$"ToolTip label {{ThemeResource}} should resolve against the label's inherited Light theme " +
					$"(Light sentinel Green), but got {foreground.Value}. If it is Red (the Dark sentinel), the " +
					$"popup-presented label resolved the {{ThemeResource}} against the application/global Dark " +
					$"theme instead of its own inherited ActualTheme.");
			}
			finally
			{
				tooltip.IsOpen = false;
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24557")]
		public async Task When_Image_Source_Loads_While_Open()
		{
			var owner = new Button { Content = "owner" };
			var image = new Image { Width = 180, Height = 120, Stretch = Stretch.Uniform };
			var SUT = new ToolTip { Content = image };
			ToolTipService.SetToolTip(owner, SUT);

			try
			{
				TestServices.WindowHelper.WindowContent = owner;
				await TestServices.WindowHelper.WaitForLoaded(owner);

				SUT.IsOpen = true;
				await TestServices.WindowHelper.WaitForLoaded(image);

				// The image gets its source only once the tooltip is showing, so the bitmap data arrives
				// while the image sits inside an already-laid-out popup.
				var opened = new TaskCompletionSource<bool>();
				image.ImageOpened += (_, _) => opened.TrySetResult(true);
				image.ImageFailed += (_, _) => opened.TrySetResult(false);
				image.Source = new BitmapImage(new Uri("ms-appx:///Assets/my500x200.jpg"));

				await Task.WhenAny(opened.Task, Task.Delay(3000));

				Assert.IsTrue(opened.Task.IsCompleted, "The image did not open while the tooltip was showing.");
				Assert.IsTrue(await opened.Task, "The image failed to load.");

				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsTrue(image.ActualWidth > 0 && image.ActualHeight > 0, "The image was not laid out with its bitmap.");

				// Closing and reopening puts the tooltip back into the popup; a source assigned afterwards
				// must still get through.
				SUT.IsOpen = false;
				await TestServices.WindowHelper.WaitForIdle();
				SUT.IsOpen = true;
				await TestServices.WindowHelper.WaitForIdle();

				var reopened = new TaskCompletionSource<bool>();
				image.ImageOpened += (_, _) => reopened.TrySetResult(true);
				image.ImageFailed += (_, _) => reopened.TrySetResult(false);
				image.Source = new BitmapImage(new Uri("ms-appx:///Assets/StoreLogo.png"));

				await Task.WhenAny(reopened.Task, Task.Delay(3000));

				Assert.IsTrue(reopened.Task.IsCompleted, "The image did not open while the reopened tooltip was showing.");
				Assert.IsTrue(await reopened.Task, "The image failed to load after reopening.");
			}
			finally
			{
				SUT.IsOpen = false;
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24820")]
		public async Task When_Content_Realizes_On_Load_Then_Measured_On_First_Open()
		{
			// An ItemsControl only creates its containers once loaded, so it invalidates its measure
			// from inside the tooltip that has just opened.
			var items = new ItemsControl { ItemsSource = new[] { "first error", "second error" } };
			var owner = new Button { Content = "owner" };
			var SUT = new ToolTip { Content = items };
			ToolTipService.SetToolTip(owner, SUT);

			try
			{
				TestServices.WindowHelper.WindowContent = owner;
				await TestServices.WindowHelper.WaitForLoaded(owner);

				SUT.IsOpen = true;

				await TestServices.WindowHelper.WaitFor(
					() => items.ActualWidth > 0 && items.ActualHeight > 0,
					timeoutMS: 3000,
					message: "The tooltip content was never measured on its first open.");
			}
			finally
			{
				SUT.IsOpen = false;
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24557")]
		public async Task When_Closed_Then_DataContext_Kept()
		{
			// The expected values are native WinUI's for the same steps; the test runs there too.
			var owner = new Button { Content = "owner" };
			var content = new TextBlock();
			content.SetBinding(TextBlock.TextProperty, new Binding());
			var SUT = new ToolTip { Content = content };
			ToolTipService.SetToolTip(owner, SUT);

			try
			{
				TestServices.WindowHelper.WindowContent = owner;
				await TestServices.WindowHelper.WaitForLoaded(owner);
				owner.DataContext = "VM1";

				SUT.IsOpen = true;
				// WinUI binds the content some time after IsOpen is set, and idling doesn't always outlast that.
				await TestServices.WindowHelper.WaitFor(
					() => content.Text == "VM1",
					timeoutMS: 3000,
					message: "The content never received the DataContext on the first open.");
				Assert.AreEqual("VM1", SUT.DataContext);

				SUT.IsOpen = false;
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreEqual("VM1", SUT.DataContext, "The DataContext was lost on close.");
				Assert.AreEqual("VM1", content.Text, "The content lost the DataContext on close.");

				owner.DataContext = "VM2";
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreEqual("VM2", SUT.DataContext);

				SUT.IsOpen = true;
				await TestServices.WindowHelper.WaitForIdle();
				owner.DataContext = "VM3";
				await TestServices.WindowHelper.WaitFor(
					() => content.Text == "VM3",
					timeoutMS: 3000,
					message: "The content never received the DataContext on the second open.");
				Assert.AreEqual("VM3", SUT.DataContext);

				SUT.IsOpen = false;
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreEqual("VM3", SUT.DataContext, "The DataContext was lost on the second close.");
				Assert.AreEqual("VM3", content.Text, "The content lost the DataContext on the second close.");
			}
			finally
			{
				SUT.IsOpen = false;
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24557")]
		public async Task When_PlacementTarget_Then_DataContext_From_Owner()
		{
			// The expected values are native WinUI's for the same steps; the test runs there too.
			var owner = new Button { Content = "owner", DataContext = "Owner" };
			var target = new Button { Content = "target", DataContext = "Target" };
			var SUT = new ToolTip { Content = "tooltip" };
			ToolTipService.SetPlacementTarget(owner, target);
			ToolTipService.SetToolTip(owner, SUT);

			try
			{
				TestServices.WindowHelper.WindowContent = new StackPanel { Children = { owner, target } };
				await TestServices.WindowHelper.WaitForLoaded(owner);

				SUT.IsOpen = true;
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreEqual("Owner", SUT.DataContext);

				owner.DataContext = "Owner2";
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreEqual("Owner2", SUT.DataContext);

				// The placement target only positions the tooltip.
				target.DataContext = "Target2";
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreEqual("Owner2", SUT.DataContext);

				SUT.IsOpen = false;
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreEqual("Owner2", SUT.DataContext);
			}
			finally
			{
				SUT.IsOpen = false;
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Closed_Then_Parent_Cleared()
		{
			var owner = new Button { Content = "owner" };
			var SUT = new ToolTip { Content = "tooltip" };
			ToolTipService.SetToolTip(owner, SUT);

			try
			{
				TestServices.WindowHelper.WindowContent = owner;
				await TestServices.WindowHelper.WaitForLoaded(owner);

				for (var i = 0; i < 2; i++)
				{
					SUT.IsOpen = true;
					await TestServices.WindowHelper.WaitForIdle();
					Assert.IsInstanceOfType(SUT.Parent, typeof(Popup));
					Assert.IsNotNull(VisualTreeHelper.GetParent(SUT));

					SUT.IsOpen = false;
					await TestServices.WindowHelper.WaitForIdle();

					// WinUI's popup lets go of the tooltip once the Closed transition has played (ToolTip::Close).
					await TestServices.WindowHelper.WaitFor(() => SUT.Parent is null, timeoutMS: 3000, message: "The popup kept the closed tooltip.");
					Assert.IsNull(VisualTreeHelper.GetParent(SUT));
				}
			}
			finally
			{
				SUT.IsOpen = false;
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Moved_To_Another_Owner_While_Open_Then_Former_Owner_Not_Followed()
		{
			// The tooltip is set on a second element while it is open for the first one, then removed from the first:
			// changes to the first element's DataContext must no longer reach it, as on WinUI.
			var first = new Button { Content = "first", DataContext = "First" };
			var second = new Button { Content = "second", DataContext = "Second" };
			var SUT = new ToolTip { Content = "tooltip" };
			ToolTipService.SetToolTip(first, SUT);

			try
			{
				TestServices.WindowHelper.WindowContent = new StackPanel { Children = { first, second } };
				await TestServices.WindowHelper.WaitForLoaded(second);

				SUT.IsOpen = true;
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreEqual("First", SUT.DataContext);

				ToolTipService.SetToolTip(second, SUT);
				SUT.IsOpen = false;
				await TestServices.WindowHelper.WaitForIdle();

				ToolTipService.SetToolTip(first, null);
				first.DataContext = "First2";
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreNotEqual("First2", SUT.DataContext, "The tooltip still follows an element it is no longer set on.");
			}
			finally
			{
				SUT.IsOpen = false;
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24557")]
		public async Task When_Local_DataContext_Cleared_After_Close_Then_Not_Kept()
		{
			// A DataContext set on the tooltip itself must not outlive it: closing must not copy it into the DataContext
			// the owner provides. Set after registering, since WinUI's ToolTipService writes the owner's DataContext over
			// the tooltip's when it registers the tooltip.
			var owner = new Button { Content = "owner", DataContext = "Owner" };
			var SUT = new ToolTip { Content = "tooltip" };
			ToolTipService.SetToolTip(owner, SUT);

			try
			{
				TestServices.WindowHelper.WindowContent = owner;
				await TestServices.WindowHelper.WaitForLoaded(owner);
				SUT.DataContext = "Local";

				SUT.IsOpen = true;
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreEqual("Local", SUT.DataContext);

				SUT.IsOpen = false;
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitFor(() => SUT.Parent is null, timeoutMS: 3000, message: "The popup kept the closed tooltip.");
				Assert.AreEqual("Local", SUT.DataContext);

				// Once cleared, WinUI has no DataContext until the owner's changes, and Uno shows the owner's; neither keeps it.
				SUT.ClearValue(FrameworkElement.DataContextProperty);
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreNotEqual("Local", SUT.DataContext, "The tooltip's own DataContext outlived it.");

				owner.DataContext = "Owner2";
				await TestServices.WindowHelper.WaitForIdle();
				Assert.AreEqual("Owner2", SUT.DataContext);
			}
			finally
			{
				SUT.IsOpen = false;
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // WinUI writes the owner's DataContext over the binding when the owner's changes.
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24557")]
		public async Task When_DataContext_Bound_Then_Bound_To_Owner_Across_Close()
		{
			// A tooltip DataContext bound to a property of the owner's must keep resolving against the owner's DataContext,
			// not against its own result, across opening (Opened handlers included), closing and reopening.
			var owner = new Button { Content = "owner" };
			var SUT = new ToolTip { Content = "tooltip" };
			ToolTipService.SetToolTip(owner, SUT);
			object openedDataContext = null;
			SUT.Opened += (_, _) => openedDataContext = SUT.DataContext;

			try
			{
				TestServices.WindowHelper.WindowContent = owner;
				await TestServices.WindowHelper.WaitForLoaded(owner);
				SUT.SetBinding(FrameworkElement.DataContextProperty, new Binding { Path = new PropertyPath(nameof(ToolTipDataContextViewModel.Child)) });
				owner.DataContext = new ToolTipDataContextViewModel { Child = "OwnerChild" };
				await TestServices.WindowHelper.WaitForIdle();

				for (var i = 0; i < 2; i++)
				{
					openedDataContext = null;
					SUT.IsOpen = true;
					await TestServices.WindowHelper.WaitForIdle();
					Assert.AreEqual("OwnerChild", openedDataContext, $"Opened #{i + 1}");
					Assert.AreEqual("OwnerChild", SUT.DataContext, $"Open #{i + 1}");

					SUT.IsOpen = false;
					await TestServices.WindowHelper.WaitForIdle();
					Assert.AreEqual("OwnerChild", SUT.DataContext, $"Closed #{i + 1}");
				}
			}
			finally
			{
				SUT.IsOpen = false;
#if HAS_UNO
				VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // Checks the service's own bookkeeping.
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
		public async Task When_Popup_Closed_Externally_Then_Service_Releases_ToolTip()
		{
			// A tooltip closed by a path the service does not drive (here its popup, closed while the pointer stays on the
			// owner) must not stay the service's current tooltip: WinUI lets go of it in the tooltip's own close
			// (ToolTip::OnIsOpenChanged).
			var root = new ContentControl();
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			try
			{
				await HoverToolTipOwnerThenClosePopup(root, mouse);

#if HAS_UNO
				Assert.IsNull(ToolTipService.CurrentToolTipForTesting, "The service must not keep a tooltip that has closed.");
#endif
			}
			finally
			{
				mouse.MoveTo(new Windows.Foundation.Point(0, 0));
				CloseOpenPopups();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // Checks managed reachability.
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
		public async Task When_Popup_Closed_Externally_Then_Owner_Collected()
		{
			// Once its tooltip has closed, however it closed, the owner is not kept alive through the service.
			var root = new ContentControl();
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			try
			{
				var ownerRef = await HoverToolTipOwnerThenClosePopup(root, mouse);

				root.Content = null;
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(await TestHelper.TryWaitUntilCollected(ownerRef), "The owner of a tooltip closed with its popup was not collected.");
			}
			finally
			{
				mouse.MoveTo(new Windows.Foundation.Point(0, 0));
				CloseOpenPopups();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // Checks managed reachability.
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
		public async Task When_Opening_Cancelled_Then_Owner_Collected()
		{
			// The pointer leaves the owner during the show delay, so the tooltip never opens: the service must drop the pending
			// tooltip, as WinUI's CancelAutomaticToolTip drops the pending owner, rather than keep it and its owner.
			var root = new ContentControl();
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			try
			{
				var ownerRef = await HoverToolTipOwnerThenLeaveBeforeOpening(root, mouse);

				root.Content = null;
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsTrue(await TestHelper.TryWaitUntilCollected(ownerRef), "The owner of a tooltip that never opened was not collected.");
			}
			finally
			{
				mouse.MoveTo(new Windows.Foundation.Point(0, 0));
				CloseOpenPopups();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
		public async Task When_Pending_ToolTip_Opened_And_Closed_By_App_Then_Still_Opens_Automatically()
		{
			// As in WinUI, closing releases a tooltip from the service only if the service opened it: the app opening and
			// closing the tooltip during the show delay does not cancel the pending automatic open.
			var owner = new Button { Content = "Hover me", Width = 100, Height = 50 };
			var toolTip = new ToolTip { Content = "ToolTip content" };
			ToolTipService.SetToolTip(owner, toolTip);
			await UITestHelper.Load(owner);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			try
			{
				mouse.MoveTo(owner.GetAbsoluteBounds().GetCenter());
				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsFalse(toolTip.IsOpen, "premise: the tooltip is still waiting to open");
#if HAS_UNO
				Assert.AreSame(toolTip, ToolTipService.CurrentToolTipForTesting, "premise: the service holds the pending tooltip");
#endif

				// Back to back, so the show delay cannot elapse while the app holds the tooltip open.
				toolTip.IsOpen = true;
				toolTip.IsOpen = false;

#if HAS_UNO
				Assert.AreSame(toolTip, ToolTipService.CurrentToolTipForTesting, "The app closing the tooltip cancelled the pending automatic open.");
#endif
				await UITestHelper.WaitFor(() => toolTip.IsOpen, timeoutMS: 5000, message: "The hovered tooltip did not open after the app opened and closed it.");
			}
			finally
			{
				mouse.MoveTo(new Windows.Foundation.Point(0, 0));
				CloseOpenPopups();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
		public async Task When_Automatic_ToolTip_Reopened_By_App_Then_ShowDuration_Does_Not_Close_It()
		{
			// Once the app has closed the automatic tooltip, the service has let go of it, so its show-duration timer must not
			// close the app's reopen.
			var owner = new Button { Content = "Hover me", Width = 100, Height = 50 };
			var toolTip = new ToolTip { Content = "ToolTip content" };
			ToolTipService.SetToolTip(owner, toolTip);
			await UITestHelper.Load(owner);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			try
			{
				mouse.MoveTo(owner.GetAbsoluteBounds().GetCenter());
				await UITestHelper.WaitFor(() => toolTip.IsOpen, timeoutMS: 5000, message: "The automatic tooltip did not open on hover.");

				toolTip.IsOpen = false;
				await TestServices.WindowHelper.WaitForIdle();
				toolTip.IsOpen = true;
				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsTrue(toolTip.IsOpen, "premise: the app reopened the tooltip");

#if HAS_UNO
				// WinUI has no show-duration close; Uno's closes the automatic tooltip after ShowDuration.
				await Task.Delay(TimeSpan.FromMilliseconds(FeatureConfiguration.ToolTip.ShowDuration + 500));
#endif
				Assert.IsTrue(toolTip.IsOpen, "The service's show-duration timer closed a tooltip the app reopened.");
			}
			finally
			{
				mouse.MoveTo(new Windows.Foundation.Point(0, 0));
				CloseOpenPopups();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
		public async Task When_Pending_ToolTip_Opened_By_App_Then_ShowDuration_Does_Not_Close_It()
		{
			// As in WinUI, the service does not take over a tooltip the app opened during the show delay.
			var owner = new Button { Content = "Hover me", Width = 100, Height = 50 };
			var toolTip = new ToolTip { Content = "ToolTip content" };
			ToolTipService.SetToolTip(owner, toolTip);
			await UITestHelper.Load(owner);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			try
			{
				mouse.MoveTo(owner.GetAbsoluteBounds().GetCenter());
				await TestServices.WindowHelper.WaitForIdle();
				Assert.IsFalse(toolTip.IsOpen, "premise: the tooltip is still waiting to open");
#if HAS_UNO
				Assert.AreSame(toolTip, ToolTipService.CurrentToolTipForTesting, "premise: the service holds the pending tooltip");
#endif

				toolTip.IsOpen = true;

#if HAS_UNO
				await UITestHelper.WaitFor(
					() => ToolTipService.CurrentToolTipForTesting is null,
					timeoutMS: FeatureConfiguration.ToolTip.ShowDelay + 2000,
					message: "The service kept a tooltip the app opened during the show delay.");
				await Task.Delay(TimeSpan.FromMilliseconds(FeatureConfiguration.ToolTip.ShowDuration + 500));
#endif
				Assert.IsTrue(toolTip.IsOpen, "The service's show-duration timer closed a tooltip the app opened.");
			}
			finally
			{
				mouse.MoveTo(new Windows.Foundation.Point(0, 0));
				CloseOpenPopups();
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		private static async Task<WeakReference> HoverToolTipOwnerThenClosePopup(ContentControl root, Mouse mouse)
		{
			var owner = new Button { Content = "Hover me", Width = 100, Height = 50 };
			var toolTip = new ToolTip { Content = "ToolTip content" };
			ToolTipService.SetToolTip(owner, toolTip);
			root.Content = owner;
			await TestServices.WindowHelper.WaitForLoaded(owner);

			mouse.MoveTo(owner.GetAbsoluteBounds().GetCenter());
			await UITestHelper.WaitFor(() => toolTip.IsOpen, timeoutMS: 5000, message: "The automatic tooltip did not open on hover.");

			// The pointer stays over the owner, so nothing the service listens to closes the tooltip.
			CloseOpenPopups();
			await UITestHelper.WaitFor(() => !toolTip.IsOpen, timeoutMS: 5000, message: "Closing the popup did not close the tooltip.");

			return new WeakReference(owner);
		}

		private static async Task<WeakReference> HoverToolTipOwnerThenLeaveBeforeOpening(ContentControl root, Mouse mouse)
		{
			var owner = new Button { Content = "Hover me", Width = 100, Height = 50 };
			var toolTip = new ToolTip { Content = "ToolTip content" };
			ToolTipService.SetToolTip(owner, toolTip);
			root.Content = owner;
			await TestServices.WindowHelper.WaitForLoaded(owner);

			var bounds = owner.GetAbsoluteBounds();
			mouse.MoveTo(bounds.GetCenter());
			await Task.Delay(100); // well inside the show delay
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsFalse(toolTip.IsOpen, "premise: the tooltip is still waiting to open");
#if HAS_UNO
			Assert.AreSame(toolTip, ToolTipService.CurrentToolTipForTesting, "premise: the service holds the pending tooltip");
#endif

			mouse.MoveTo(new Windows.Foundation.Point(bounds.Right + 50, bounds.Bottom + 50));
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsFalse(toolTip.IsOpen, "The tooltip must not open once the pointer has left.");
#if HAS_UNO
			Assert.IsNull(ToolTipService.CurrentToolTipForTesting, "The service must drop a tooltip whose opening was cancelled.");
#endif

			return new WeakReference(owner);
		}

		private static void CloseOpenPopups()
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);

			for (var i = popups.Count - 1; i >= 0; i--)
			{
				popups[i].IsOpen = false;
			}
		}

		/// <summary>The element a ToolTip is placed relative to (the owner, or its PlacementTarget): its private _owner field.</summary>
		private static object AnchorOf(ToolTip toolTip)
			=> (typeof(ToolTip).GetField("_owner", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertFailedException("ToolTip has no _owner field."))
				.GetValue(toolTip);
	}

	[Bindable]
	public partial class ToolTipDataContextViewModel
	{
		public object Child { get; set; }
	}
}
