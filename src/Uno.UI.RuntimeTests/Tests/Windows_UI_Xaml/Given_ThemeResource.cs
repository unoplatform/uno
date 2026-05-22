using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MUXControlsTestApp.Utilities;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Windows.UI;
using Microsoft.UI.Xaml.Markup;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ThemeResource
	{
		[TestMethod]
#if WINAPPSDK
		[Ignore("Fails on UWP with 'The parameter is incorrect.'")]
#endif
		public async Task When_ThemeResource_Binding_In_Template()
		{
			using (StyleHelper.UseAppLevelResources(new App_Level_Resources()))
			{
				var userControl = new When_ThemeResource_Binding_In_Template_UserControl();

				WindowHelper.WindowContent = userControl;
				await WindowHelper.WaitForLoaded(userControl);

				Assert.AreEqual(Colors.Red, GetInnerBackgroundColor(userControl.Inner_ThemeResource_Control_No_Override));
				Assert.AreEqual(Colors.Blue, GetInnerBackgroundColor(userControl.Inner_ThemeResource_Control_With_Override));

				Color GetInnerBackgroundColor(Inner_ThemeResource_Control control)
				{
					var brush = control.ThemeBoundBorder?.Background as SolidColorBrush;
					Assert.IsNotNull(brush);
					return brush.Color;
				}
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		[RequiresFullWindow]
		public async Task When_Parent_Resource_Override_On_Loaded()
		{
			using var _ = ThemeHelper.UseApplicationLightTheme();
			using (StyleHelper.UseAppLevelResources(new App_Level_Resources()))
			{
				var userControl = new When_Parent_Resource_Override_On_Loaded();

				WindowHelper.WindowContent = userControl;
				await WindowHelper.WaitForLoaded(userControl);

				Assert.AreEqual(Colors.Yellow, (userControl.innerBorder.Background as SolidColorBrush).Color);
			}
		}

#if HAS_UNO // On UWP/WinUI, the Samples app is always in Fluent theme
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_DefaultForeground_Non_Fluent()
		{
			using var _ = StyleHelper.UseUwpStyles();
			await When_DefaultForeground(Colors.Black, Colors.White);
		}
#endif

		[TestMethod]
		[RequiresFullWindow]
		public async Task When_DefaultForeground_Fluent()
		{
			await When_DefaultForeground(Color.FromArgb(228, 0, 0, 0), Colors.White);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_AppLevel_Resource_CheckBox_Override()
		{
			// Use fluent styles to rely on known Theme Resources
			var SUT = new When_AppLevel_Resource_CheckBox_Override();

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var normalRectangle = SUT.cb01.FindName("NormalRectangle") as Rectangle;

			Assert.IsNotNull(normalRectangle);

			// Validation for early ThemeResource resolution in Storyboard timeline
			Assert.AreEqual(Colors.Yellow, normalRectangle.Fill.GetValue(SolidColorBrush.ColorProperty));

			SUT.cb01.IsChecked = true;

			// Validation for late (OnLoaded) ThemeResource resolution in Storyboard timeline
			Assert.AreEqual(Colors.Red, normalRectangle.Fill.GetValue(SolidColorBrush.ColorProperty));
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_AppLevel_Resource_SplitButton_Override()
		{
			// Use fluent styles to rely on known Theme Resources
			var SUT = new When_AppLevel_Resource_SplitButton_Override();

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var contentPresenter = SUT.sb01.FindName("ContentPresenter") as ContentPresenter;

			Assert.IsNotNull(contentPresenter);

			var color = contentPresenter.Foreground.GetValue(SolidColorBrush.ColorProperty);

			SUT.sb01.IsEnabled = false;

			// Validation for late (OnLoaded) ThemeResource resolution in Setter value
			Assert.AreEqual(Colors.Yellow, contentPresenter.Foreground.GetValue(SolidColorBrush.ColorProperty));
			Assert.AreNotEqual(color, contentPresenter.Foreground.GetValue(SolidColorBrush.ColorProperty));
		}

		[TestMethod]
		public async Task When_ThemeResource_Style_Switch()
		{
			var SUT = new When_ThemeResource_Style_Switch_Page();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var color = ((SolidColorBrush)SUT.TestButton.Background).Color;

			Assert.AreEqual(Colors.Blue, color);

			SUT.TestButton.Style = (Style)SUT.Resources["SecondButtonStyle"];

			await WindowHelper.WaitForIdle();

			color = ((SolidColorBrush)SUT.TestButton.Background).Color;

			Assert.AreEqual(Colors.Red, color);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		[RequiresFullWindow]
		public async Task When_Theme_Changed()
		{
			using var _ = ThemeHelper.UseApplicationLightTheme();
			var control = new ThemeResource_Theme_Changing_Override();
			WindowHelper.WindowContent = control;

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(Colors.Red, (control.button01.Background as SolidColorBrush)?.Color);
			Assert.AreEqual(Colors.Red, (control.button02.Background as SolidColorBrush)?.Color);
			Assert.AreEqual(Colors.Red, (control.button03.Background as SolidColorBrush)?.Color);
			Assert.AreEqual(Colors.Red, (control.button04.Background as SolidColorBrush)?.Color);

			Assert.AreEqual(Colors.Green, (control.button01_override.Background as SolidColorBrush)?.Color);
			Assert.AreEqual(Colors.Green, (control.button02_override.Background as SolidColorBrush)?.Color);
			Assert.AreEqual(Colors.Green, (control.button03_override.Background as SolidColorBrush)?.Color);
			Assert.AreEqual(Colors.Green, (control.button04_override.Background as SolidColorBrush)?.Color);

			using (ThemeHelper.UseDarkTheme())
			{
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(Colors.DarkRed, (control.button01.Background as SolidColorBrush)?.Color);
				Assert.AreEqual(Colors.DarkRed, (control.button02.Background as SolidColorBrush)?.Color);
				Assert.AreEqual(Colors.DarkRed, (control.button03.Background as SolidColorBrush)?.Color);
				Assert.AreEqual(Colors.DarkRed, (control.button04.Background as SolidColorBrush)?.Color);

				Assert.AreEqual(Colors.DarkGreen, (control.button01_override.Background as SolidColorBrush)?.Color);
				Assert.AreEqual(Colors.DarkGreen, (control.button02_override.Background as SolidColorBrush)?.Color);
				Assert.AreEqual(Colors.DarkGreen, (control.button03_override.Background as SolidColorBrush)?.Color);
				Assert.AreEqual(Colors.DarkGreen, (control.button04_override.Background as SolidColorBrush)?.Color);
			}
		}

		[TestMethod]
		public async Task When_Refresh_On_Loading()
		{
			var userControl = new ThemeResource_Refresh_On_Loading();
			WindowHelper.WindowContent = userControl;
			await WindowHelper.WaitForLoaded(userControl);

			var expected = new Thickness(40);

			var loaded = false;
			var datePicker = new DatePicker();
			datePicker.Loaded += (s, e) => loaded = true;
			userControl.SetContent(datePicker);

			await WindowHelper.WaitFor(() => loaded);

			var datePart = (TextBlock)datePicker.FindVisualChildByName("DayTextBlock");

			Assert.AreEqual(expected, datePart.Padding);
		}

#if HAS_UNO
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_ActualThemeChanged_Throws()
		{
			using var _ = ThemeHelper.UseApplicationLightTheme();
			using (StyleHelper.UseAppLevelResources(new App_Level_Resources()))
			{
				var border = (Border)XamlReader.Load(
				"""
				<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" BorderBrush="{ThemeResource LoadedTestPrimaryBrush}" Width="100" Height="100" />
				""");
				var parent = new ContentControl() { Content = border };
				parent.ActualThemeChanged += (sender, args) => throw new Exception();

				await UITestHelper.Load(parent);

				Assert.AreEqual(Colors.MediumPurple, ((SolidColorBrush)border.BorderBrush).Color);

				using (ThemeHelper.UseDarkTheme())
				{
					await UITestHelper.WaitForIdle();
					Assert.AreEqual(Colors.MediumOrchid, ((SolidColorBrush)border.BorderBrush).Color);
				}

				await UITestHelper.WaitForIdle();
				Assert.AreEqual(Colors.MediumPurple, ((SolidColorBrush)border.BorderBrush).Color);
			}
		}
#endif

		private async Task When_DefaultForeground(Color lightThemeColor, Color darkThemeColor)
		{
#if HAS_UNO
			using var _ = ThemeHelper.UseApplicationLightTheme();
#endif
			var run = new Run()
			{
				Text = "Hello"
			};

			var textBlock = new TextBlock()
			{
				Inlines = {
					run,
				}
			};

			var button = new Button() { Content = "Test" };
			var bitmapIcon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/Icons/search.png") };
			var contentPresenter = new ContentPresenter() { Content = "Hi" };
			var stackPanel = new StackPanel()
			{
				Children =
				{
					textBlock,
					button,
					bitmapIcon,
					contentPresenter,
				}
			};

			WindowHelper.WindowContent = stackPanel;
			await WindowHelper.WaitForLoaded(stackPanel);

#if !HAS_UNO
			// Due to a bug in WinUI, RequestedTheme needs to be set explicitly here to force the correct
			// foreground color - https://github.com/microsoft/microsoft-ui-xaml/issues/8392.
			stackPanel.RequestedTheme = ElementTheme.Light;
#endif
			// Light theme
			var runForegroundBrush = (SolidColorBrush)run.Foreground;
			var textBlockForegroundBrush = (SolidColorBrush)textBlock.Foreground;
			var buttonForegroundBrush = (SolidColorBrush)button.Foreground;
			var bitmapIconForegroundBrush = (SolidColorBrush)bitmapIcon.Foreground;
			var contentPresenterForegroundBrush = (SolidColorBrush)contentPresenter.Foreground;
			Assert.AreEqual(lightThemeColor, runForegroundBrush.Color);
			Assert.AreEqual(lightThemeColor, textBlockForegroundBrush.Color);
			Assert.AreEqual(lightThemeColor, buttonForegroundBrush.Color);
			Assert.AreEqual(lightThemeColor, bitmapIconForegroundBrush.Color);
			Assert.AreEqual(lightThemeColor, contentPresenterForegroundBrush.Color);

			using (ThemeHelper.UseDarkTheme())
			{
#if !HAS_UNO
				// Due to a bug in WinUI, RequestedTheme needs to be set explicitly here to force the correct
				// foreground color - https://github.com/microsoft/microsoft-ui-xaml/issues/8392.
				stackPanel.RequestedTheme = ElementTheme.Dark;
#endif
				runForegroundBrush = (SolidColorBrush)run.Foreground;
				textBlockForegroundBrush = (SolidColorBrush)textBlock.Foreground;
				buttonForegroundBrush = (SolidColorBrush)button.Foreground;
				bitmapIconForegroundBrush = (SolidColorBrush)bitmapIcon.Foreground;
				contentPresenterForegroundBrush = (SolidColorBrush)contentPresenter.Foreground;
				Assert.AreEqual(darkThemeColor, runForegroundBrush.Color);
				Assert.AreEqual(darkThemeColor, textBlockForegroundBrush.Color);
				Assert.AreEqual(darkThemeColor, buttonForegroundBrush.Color);
				Assert.AreEqual(darkThemeColor, bitmapIconForegroundBrush.Color);
				Assert.AreEqual(darkThemeColor, contentPresenterForegroundBrush.Color);
			}
		}

#if HAS_UNO
		[TestMethod]
		public async Task When_HotReload_Updates_ThemeResource_Bindings_Without_HotReload_Flag()
		{
			// This test simulates the scenario where a library (e.g., Uno.Toolkit.Material)
			// is compiled without Hot Reload support, so its resource bindings only have
			// the ThemeResource flag, not the HotReload flag.
			// When a standalone ResourceDictionary is hot-reloaded with updated brush values,
			// these library bindings should also be re-evaluated.

			var initialBrush = new SolidColorBrush(Colors.Red);
			var updatedBrush = new SolidColorBrush(Colors.Blue);

			// Set up an app-level resource
			var overrideDict = new ResourceDictionary();
			overrideDict["TestHRBrush"] = initialBrush;

			using var cleanup = StyleHelper.UseAppLevelResources(overrideDict);

			var border = new Border { Width = 50, Height = 50 };

			// Manually set a resource binding with ONLY ThemeResource flag (no HotReload)
			// This simulates a library-compiled {ThemeResource TestHRBrush} binding
			var resourceKey = new SpecializedResourceDictionary.ResourceKey("TestHRBrush");
			(border as IDependencyObjectStoreProvider).Store.SetResourceBinding(
				Border.BackgroundProperty,
				resourceKey,
				ResourceUpdateReason.ThemeResource, // Only ThemeResource, no HotReload - simulates library
				context: null,
				precedence: null,
				setterBindingPath: null);

			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);

			// Verify initial value
			Assert.AreEqual(Colors.Red, (border.Background as SolidColorBrush)?.Color,
				"Initial brush should be Red");

			// Simulate a hot-reload update: change the resource value
			overrideDict["TestHRBrush"] = updatedBrush;

			// Trigger hot reload resource binding update
			Application.Current.UpdateResourceBindingsForHotReload();

			// The binding should now reflect the updated value
			Assert.AreEqual(Colors.Blue, (border.Background as SolidColorBrush)?.Color,
				"After hot reload, brush should be updated to Blue even for ThemeResource-only bindings");
		}
#endif

#if HAS_UNO
		// Regression: a subtree pinned to an explicit RequestedTheme that differs from the
		// application/OS theme must resolve {ThemeResource} values against the subtree's
		// theme, not against Themes.Active. This exercises the out-of-walk lookup paths
		// (parse-time ApplyResource, RefreshValue, InnerUpdateResourceBindingsUnsafe).
		// Without ThemeResourceReference.PushOwnerThemeIfDifferent, the parse-time lookup
		// would select the Dark sub-dictionary because Themes.Active = Dark (the app theme),
		// even though the Border's inherited ActualTheme is Light from its pinned ancestor.
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Light_Pinned_Subtree_Inside_Dark_App_Resolves_Light_ThemeResource()
		{
			using var _ = ThemeHelper.UseApplicationDarkTheme();

			var resources = new ResourceDictionary();
			resources.ThemeDictionaries["Light"] = new ResourceDictionary
			{
				["OwnerThemePushTestBrush"] = new SolidColorBrush(Colors.LightYellow),
			};
			resources.ThemeDictionaries["Dark"] = new ResourceDictionary
			{
				["OwnerThemePushTestBrush"] = new SolidColorBrush(Colors.DarkSlateBlue),
			};
			Application.Current.Resources.MergedDictionaries.Add(resources);

			try
			{
				var lightPinnedRoot = (Border)XamlReader.Load(
					"""
					<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
							RequestedTheme="Light"
							Width="150" Height="150">
						<Border x:Name="Inner" Background="{ThemeResource OwnerThemePushTestBrush}" />
					</Border>
					""");

				WindowHelper.WindowContent = lightPinnedRoot;
				await WindowHelper.WaitForLoaded(lightPinnedRoot);

				var inner = (Border)lightPinnedRoot.FindName("Inner");
				var brush = inner.Background as SolidColorBrush;
				Assert.IsNotNull(brush, "Background brush should be resolved");
				Assert.AreEqual(
					Colors.LightYellow,
					brush.Color,
					"Inner Border inherits Light from the pinned ancestor; the {ThemeResource} must " +
					"resolve from the Light sub-dictionary, not from Themes.Active (Dark).");
			}
			finally
			{
				Application.Current.Resources.MergedDictionaries.Remove(resources);
			}
		}
#endif

#if HAS_UNO
		// Regression for the reflection-free owner-theme walk: a non-UIElement DependencyObject
		// (a Microsoft.Xaml.Behaviors-style behaviour) whose {ThemeResource}-bound property is
		// resolved OUTSIDE any theme walk must pick up its host element's inherited theme, reached
		// purely through DependencyObjectStore.Parent. The behaviour is attached the same way
		// Interaction.Behaviors does it — via a DependencyObjectCollection whose parent is the host —
		// so ThemeResourceReference.GetThemeResolutionParent follows Store.Parent up to the
		// Light-pinned Border without reflecting for an "AssociatedObject" property.
		//
		// Before the reflection was removed, ResolveInheritedBaseTheme could only hop to a property
		// literally named "AssociatedObject"; a behaviour without one (like this fake) dead-ended,
		// no Light theme was pushed, and the lookup resolved against Themes.Active (Dark).
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_NonFE_Behaviour_Inside_Light_Pinned_Subtree_Resolves_Light_ThemeResource()
		{
			using var _ = ThemeHelper.UseApplicationDarkTheme();

			var lightPinnedRoot = new Border
			{
				RequestedTheme = ElementTheme.Light,
				Width = 150,
				Height = 150,
			};

			var borderResources = new ResourceDictionary();
			borderResources.ThemeDictionaries["Light"] = new ResourceDictionary
			{
				["OwnerThemePushTestBrush"] = new SolidColorBrush(Colors.LightYellow),
			};
			borderResources.ThemeDictionaries["Dark"] = new ResourceDictionary
			{
				["OwnerThemePushTestBrush"] = new SolidColorBrush(Colors.DarkSlateBlue),
			};
			lightPinnedRoot.Resources = borderResources;

			WindowHelper.WindowContent = lightPinnedRoot;
			await WindowHelper.WaitForLoaded(lightPinnedRoot);

			// Attach the behaviour the way Interaction.Behaviors does: a DependencyObjectCollection
			// whose parent is the host element. Adding the behaviour wires its Store.Parent to the
			// host (DependencyObjectCollection.OnAdded -> SetParent(collection.GetParent() ?? collection)).
			var behavior = new FakeThemeBehavior();
			var behaviorCollection = new DependencyObjectCollection(lightPinnedRoot);
			behaviorCollection.Add(behavior);

			Assert.AreSame(
				lightPinnedRoot,
				behavior.GetParent(),
				"Behaviour's Store.Parent should be wired to the host element, like Interaction.Behaviors.");

			// Register a {ThemeResource}-style binding on the behaviour's property (mirrors how a
			// library-compiled {ThemeResource} binding is set up) and resolve it out of any theme walk.
			var resourceKey = new SpecializedResourceDictionary.ResourceKey("OwnerThemePushTestBrush");
			var store = ((IDependencyObjectStoreProvider)behavior).Store;
			store.SetResourceBinding(
				FakeThemeBehavior.TestBrushProperty,
				resourceKey,
				ResourceUpdateReason.ThemeResource,
				context: null,
				precedence: null,
				setterBindingPath: null);

			store.UpdateResourceBindings(
				ResourceUpdateReason.ThemeResource,
				resourceContextProvider: lightPinnedRoot);

			var brush = behavior.TestBrush as SolidColorBrush;
			Assert.IsNotNull(brush, "Behaviour's TestBrush should be resolved");
			Assert.AreEqual(
				Colors.LightYellow,
				brush.Color,
				"The behaviour inherits Light from its host element via Store.Parent; the {ThemeResource} " +
				"must resolve from the Light sub-dictionary, not from Themes.Active (Dark).");
		}
#endif
	}

#if HAS_UNO
	// Minimal stand-in for a Microsoft.Xaml.Behaviors-style behaviour: a non-UIElement
	// DependencyObject carrying a {ThemeResource}-bindable property. It deliberately has no
	// "AssociatedObject" property — theme resolution must reach its host purely through
	// DependencyObjectStore.Parent (wired by the Interaction.Behaviors DependencyObjectCollection).
	public partial class FakeThemeBehavior : DependencyObject
	{
		public static readonly DependencyProperty TestBrushProperty = DependencyProperty.Register(
			nameof(TestBrush),
			typeof(Brush),
			typeof(FakeThemeBehavior),
			new PropertyMetadata(null));

		public Brush TestBrush
		{
			get => (Brush)GetValue(TestBrushProperty);
			set => SetValue(TestBrushProperty, value);
		}
	}
#endif
}
