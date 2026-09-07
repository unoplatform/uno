#if HAS_UNO
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// Covers the opt-in perf2026 control styles, enabled through
/// <see cref="FeatureConfiguration.Style.UseDefaultStyleOptimizations"/>.
///
/// The optimized dictionaries replace the visual state storyboards that only carry zero-duration
/// <see cref="DiscreteObjectKeyFrame"/>s by <see cref="VisualState.Setters"/>, which apply at the
/// same (Animations) precedence. The tests assert both that nothing changes when the feature is
/// off, and that the optimized styles carry the very same states and applied values.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_DefaultStyleOptimizations
{
	[TestMethod]
	public void When_Default_Then_Optimizations_Are_Disabled()
	{
		// The optimizations must stay opt-in: enabling them by default would change the
		// visual tree of every app.
		Assert.IsFalse(FeatureConfiguration.Style.UseDefaultStyleOptimizations);
		Assert.IsFalse(FeatureConfiguration.Style.DeferOverriddenSetterValues);
		Assert.IsFalse(FeatureConfiguration.Perf2026.EnableAll);
	}

	[TestMethod]
	public void When_Disabled_Then_Resources_Are_Not_Overlaid()
	{
		var resources = CreateResources(optimized: false);

		var style = GetStyle(resources, typeof(Button));

		Assert.IsNotNull(style);
		Assert.AreEqual(typeof(Button), style.TargetType);
	}

	[TestMethod]
	public void When_Enabled_Then_Non_Optimized_Keys_Still_Resolve()
	{
		var resources = CreateResources(optimized: true);

		// TextBox has no optimized variant: the overlay must leave the base entry in place.
		var textBoxStyle = GetStyle(resources, typeof(TextBox));
		Assert.AreEqual(typeof(TextBox), textBoxStyle.TargetType);

		Assert.IsTrue(resources.TryGetValue("DefaultTextBoxStyle", out var defaultTextBoxStyle, shouldCheckSystem: false));
		Assert.IsInstanceOfType(defaultTextBoxStyle, typeof(Style));

		// Keys defined by the same source files as the optimized styles, but not redefined by
		// them, keep resolving too.
		Assert.IsTrue(resources.TryGetValue("ScrollBarSize", out var scrollBarSize, shouldCheckSystem: false));
		Assert.IsNotNull(scrollBarSize);
	}

	[TestMethod]
	public void When_Enabled_Then_Optimized_Styles_Are_Overlaid()
	{
		var defaults = CreateResources(optimized: false);
		var optimized = CreateResources(optimized: true);

		object[] keys =
		[
			typeof(Button),
			typeof(CheckBox),
			typeof(ComboBox),
			typeof(ScrollBar),
			typeof(Slider),
			typeof(ToggleSwitch),
			typeof(AppBarButton),
			typeof(AppBarToggleButton),
			typeof(ComboBoxItem),
			typeof(CommandBar),
			"DefaultButtonStyle",
			"AccentButtonStyle",
			"NavigationBackButtonNormalStyle",
			"NavigationBackButtonSmallStyle",
			"TabViewCloseButtonStyle",
		];

		foreach (var key in keys)
		{
			var defaultStyle = GetStyle(defaults, key);
			var optimizedStyle = GetStyle(optimized, key);

			// The overlay must actually have replaced the entry, ...
			Assert.AreNotSame(defaultStyle, optimizedStyle, $"{key} was not overlaid");

			// ... while keeping the very same contract.
			Assert.AreEqual(defaultStyle.TargetType, optimizedStyle.TargetType, $"TargetType changed for {key}");
			Assert.AreEqual(defaultStyle.BasedOn?.TargetType, optimizedStyle.BasedOn?.TargetType, $"BasedOn changed for {key}");
			var addedProperty = defaultStyle.TargetType == typeof(ComboBox)
				? ComboBoxHelper.KeepInteriorCornersSquareProperty
				: null;
			Assert.AreEqual(GetSetterProperties(defaultStyle, addedProperty), GetSetterProperties(optimizedStyle, addedProperty), $"Setters changed for {key}");
		}

		var comboBoxStyle = GetStyle(optimized, "DefaultComboBoxStyle");
		Assert.IsTrue(comboBoxStyle.Setters.OfType<Setter>().Any(setter =>
			setter.Property == ComboBoxHelper.KeepInteriorCornersSquareProperty && Equals(setter.Value, true)));
	}

	[TestMethod]
	public async Task When_Enabled_Then_Storyboards_Are_Replaced_By_Setters()
	{
		var defaults = CreateResources(optimized: false);
		var optimized = CreateResources(optimized: true);

		var controls = new (Control Default, Control Optimized)[]
		{
			(new Button { Content = "Button" }, new Button { Content = "Button" }),
			(new CheckBox { Content = "CheckBox" }, new CheckBox { Content = "CheckBox" }),
			(new ComboBox(), new ComboBox()),
			(new Slider(), new Slider()),
			(new ToggleSwitch(), new ToggleSwitch()),
		};

		var panel = new StackPanel();
		foreach (var (defaultControl, optimizedControl) in controls)
		{
			defaultControl.Style = GetStyle(defaults, defaultControl.GetType());
			optimizedControl.Style = GetStyle(optimized, optimizedControl.GetType());

			panel.Children.Add(defaultControl);
			panel.Children.Add(optimizedControl);
		}

		await UITestHelper.Load(panel);

		foreach (var (defaultControl, optimizedControl) in controls)
		{
			var name = defaultControl.GetType().Name;

			var defaultStates = GetStates(defaultControl);
			var optimizedStates = GetStates(optimizedControl);

			// The optimization must not add, remove or rename any visual state.
			CollectionAssert.AreEqual(
				defaultStates.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray(),
				optimizedStates.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray(),
				$"Visual states changed for {name}");

			// Every zero-duration discrete object animation is expected to become a setter, ...
			var convertible = CountTimelines(defaultStates.Values, IsConvertibleTimeline);
			Assert.IsTrue(convertible > 0, $"{name} was expected to carry convertible animations by default");
			Assert.AreEqual(0, CountTimelines(optimizedStates.Values, IsConvertibleTimeline), $"{name} still uses convertible animations when optimized");

			var gainedSetters = optimizedStates.Values.Sum(state => state.Setters.Count) - defaultStates.Values.Sum(state => state.Setters.Count);
			Assert.IsTrue(gainedSetters > 0, $"{name} did not gain any visual state setter");

			// ... while the animations that cannot be expressed as a setter (durations, easings,
			// color/double animations, ...) are kept as-is.
			Assert.AreEqual(
				CountTimelines(defaultStates.Values, timeline => !IsConvertibleTimeline(timeline)),
				CountTimelines(optimizedStates.Values, timeline => !IsConvertibleTimeline(timeline)),
				$"{name} lost non-convertible animations");
		}
	}

	[TestMethod]
	public async Task When_Enabled_Then_State_Values_Are_Unchanged()
	{
		var defaultButton = new Button { Content = "Button", Style = GetStyle(CreateResources(optimized: false), typeof(Button)) };
		var optimizedButton = new Button { Content = "Button", Style = GetStyle(CreateResources(optimized: true), typeof(Button)) };

		var panel = new StackPanel();
		panel.Children.Add(defaultButton);
		panel.Children.Add(optimizedButton);

		await UITestHelper.Load(panel);

		var defaultRoot = FindContentPresenter(defaultButton);
		var optimizedRoot = FindContentPresenter(optimizedButton);

		foreach (var state in new[] { "PointerOver", "Pressed", "Disabled", "Normal" })
		{
			VisualStateManager.GoToState(defaultButton, state, false);
			VisualStateManager.GoToState(optimizedButton, state, false);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(ColorOf(defaultRoot.Background), ColorOf(optimizedRoot.Background), $"Background differs in {state}");
			Assert.AreEqual(ColorOf(defaultRoot.BorderBrush), ColorOf(optimizedRoot.BorderBrush), $"BorderBrush differs in {state}");
			Assert.AreEqual(ColorOf(defaultRoot.Foreground), ColorOf(optimizedRoot.Foreground), $"Foreground differs in {state}");
		}
	}

	[TestMethod]
	public async Task When_Optimized_CheckBox_Returns_To_Normal_Then_TemplateBindings_Are_Restored()
	{
		var background = new SolidColorBrush(Microsoft.UI.Colors.Red);
		var foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
		var border = new SolidColorBrush(Microsoft.UI.Colors.Blue);
		var checkBox = new CheckBox
		{
			Content = "Source parity",
			Background = background,
			Foreground = foreground,
			BorderBrush = border,
			Style = GetStyle(CreateResources(optimized: true), typeof(CheckBox)),
		};

		await UITestHelper.Load(checkBox);
		var root = checkBox.GetTemplateChild("RootGrid") as Grid;
		var presenter = checkBox.GetTemplateChild("ContentPresenter") as ContentPresenter;
		var glyph = checkBox.GetTemplateChild("CheckGlyph") as AnimatedIcon;
		Assert.IsNotNull(root);
		Assert.IsNotNull(presenter);
		Assert.IsNotNull(glyph);
		Assert.AreEqual("NormalOff", AnimatedIcon.GetState(glyph));

		foreach (var state in new[] { "CheckedNormal", "UncheckedPointerOver", "IndeterminateNormal", "UncheckedDisabled" })
		{
			Assert.IsTrue(VisualStateManager.GoToState(checkBox, state, false));
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(VisualStateManager.GoToState(checkBox, "UncheckedNormal", false));
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreSame(background, root.Background, $"Background template binding was lost after {state}.");
			Assert.AreSame(border, root.BorderBrush, $"Border template binding was lost after {state}.");
			Assert.AreSame(foreground, presenter.Foreground, $"Foreground template binding was lost after {state}.");
			Assert.AreEqual("NormalOff", AnimatedIcon.GetState(glyph), $"Animated icon state was not restored after {state}.");
		}
	}

	[TestMethod]
	public async Task When_Optimized_AppBarButton_Uses_Label_Font_Resource()
	{
		var button = new AppBarButton
		{
			Label = "Resource override",
			Style = GetStyle(CreateResources(optimized: true), typeof(AppBarButton)),
		};
		button.Resources["AppBarButtonLabelFontSize"] = 18d;

		await UITestHelper.Load(button);

		var label = button.GetTemplateChild("TextLabel") as TextBlock;
		Assert.IsNotNull(label);
		Assert.AreEqual(18d, label.FontSize);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
#if !HAS_RENDER_TARGET_BITMAP
	[Ignore("Cannot take screenshot on this platform.")]
#endif
	public async Task When_Optimized_CommandBar_Opens_With_Visible_Ellipsis(bool noGrid)
	{
		using var _ = FeatureConfigurationHelper.UseIconElementNoGridContainer(noGrid);
		var commandBar = new CommandBar
		{
			Width = 320,
			Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green),
			Style = GetStyle(CreateResources(optimized: true), typeof(CommandBar)),
		};
		commandBar.PrimaryCommands.Add(new AppBarButton { Label = "Primary", Icon = new SymbolIcon(Symbol.Add) });
		commandBar.SecondaryCommands.Add(new AppBarButton { Label = "Secondary" });
		await UITestHelper.Load(commandBar);

		try
		{
			var moreButton = commandBar.GetTemplateChild("MoreButton") as Button;
			var ellipsis = commandBar.GetTemplateChild("EllipsisIcon") as FontIcon;
			Assert.IsNotNull(moreButton);
			Assert.IsNotNull(ellipsis);
			Assert.AreEqual(20d, ellipsis.FontSize);
			Assert.AreEqual(3d, ellipsis.Height);

			var screenshot = await UITestHelper.ScreenShot(moreButton);
			ImageAssert.HasColorInRectangle(
				screenshot,
				new System.Drawing.Rectangle(0, 0, screenshot.Width, screenshot.Height),
				Microsoft.UI.Colors.Green);

			commandBar.IsOpen = true;
			await TestServices.WindowHelper.WaitForIdle();

			var popup = commandBar.GetTemplateChild("OverflowPopup") as Popup;
			var items = commandBar.GetTemplateChild("SecondaryItemsControl") as ItemsControl;
			Assert.IsNotNull(popup);
			Assert.IsNotNull(items);
			Assert.IsTrue(popup.IsOpen);
			Assert.AreEqual(1, items.Items.Count);
		}
		finally
		{
			commandBar.IsOpen = false;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[RequiresFullWindow]
	[DataRow(VerticalAlignment.Top, true)]
	[DataRow(VerticalAlignment.Bottom, false)]
	public async Task When_Optimized_Editable_ComboBox_Updates_Interior_Corners(VerticalAlignment alignment, bool opensDown)
	{
		var controlRadius = new CornerRadius(12);
		var overlayRadius = new CornerRadius(8);
		var comboBox = new ComboBox
		{
			IsEditable = true,
			Width = 240,
			Margin = new Thickness(24),
			VerticalAlignment = alignment,
			MaxDropDownHeight = 120,
			CornerRadius = controlRadius,
			ItemsSource = new[] { "First", "Second", "Third", "Fourth", "Fifth" },
			Style = GetStyle(CreateResources(optimized: true), typeof(ComboBox)),
		};
		comboBox.Resources["OverlayCornerRadius"] = overlayRadius;
		await UITestHelper.Load(new Grid { Children = { comboBox } });
		Assert.IsTrue(ComboBoxHelper.GetKeepInteriorCornersSquare(comboBox));

		try
		{
			var popupBorder = comboBox.GetTemplateChild("PopupBorder") as Border;
			var textBox = comboBox.GetTemplateChild("EditableText") as TextBox;
			Assert.IsNotNull(popupBorder);
			Assert.IsNotNull(textBox);

			comboBox.IsDropDownOpen = true;
			await UITestHelper.WaitForLoaded(popupBorder);
			await TestServices.WindowHelper.WaitForIdle();

			var offset = popupBorder.TransformToVisual(textBox).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;
			Assert.AreEqual(opensDown, offset > 0);
			Assert.AreEqual(opensDown ? new CornerRadius(0, 0, 8, 8) : new CornerRadius(8, 8, 0, 0), popupBorder.CornerRadius);
			Assert.AreEqual(opensDown ? new CornerRadius(12, 12, 0, 0) : new CornerRadius(0, 0, 12, 12), textBox.CornerRadius);

			comboBox.IsDropDownOpen = false;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(overlayRadius, popupBorder.CornerRadius);
			Assert.AreEqual(controlRadius, textBox.CornerRadius);

			ComboBoxHelper.SetKeepInteriorCornersSquare(comboBox, false);
			comboBox.IsDropDownOpen = true;
			await UITestHelper.WaitForLoaded(popupBorder);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(overlayRadius, popupBorder.CornerRadius, "Disabling the helper must revoke the open handler.");
			Assert.AreEqual(controlRadius, textBox.CornerRadius);
		}
		finally
		{
			comboBox.IsDropDownOpen = false;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Enabled_Then_Default_Style_Channel_Uses_Optimized_Style()
	{
		var previous = FeatureConfiguration.Style.UseDefaultStyleOptimizations;

		try
		{
			FeatureConfiguration.Style.UseDefaultStyleOptimizations = true;
			_ = new XamlControlsResources();

			var button = new Button
			{
				Content = "Button",
				Style = Style.GetDefaultStyleForType(typeof(Button)),
			};
			await UITestHelper.Load(button);

			var states = GetStates(button);
			Assert.AreEqual(
				0,
				CountTimelines(states.Values, IsConvertibleTimeline),
				"The optimized default-style channel should select the registered Perf2026 style.");
		}
		finally
		{
			FeatureConfiguration.Style.UseDefaultStyleOptimizations = previous;
		}
	}

	[TestMethod]
	public void When_OverlayFrom_Then_Only_Matching_Keys_Are_Replaced()
	{
		var target = new ResourceDictionary
		{
			["Kept"] = "kept",
			["Replaced"] = "base",
		};
		target.MergedDictionaries.Add(new ResourceDictionary { ["Merged"] = "merged" });

		var source = new ResourceDictionary { ["Replaced"] = "optimized" };
		source.MergedDictionaries.Add(new ResourceDictionary
		{
			["Added"] = "added",
			// Values of the source dictionary itself take precedence over its merged dictionaries.
			["Replaced"] = "merged-optimized",
		});

		target.OverlayFrom(source);

		Assert.IsTrue(target.TryGetValue("Kept", out var kept, shouldCheckSystem: false));
		Assert.AreEqual("kept", kept);

		Assert.IsTrue(target.TryGetValue("Merged", out var merged, shouldCheckSystem: false));
		Assert.AreEqual("merged", merged);

		Assert.IsTrue(target.TryGetValue("Replaced", out var replaced, shouldCheckSystem: false));
		Assert.AreEqual("optimized", replaced);

		Assert.IsTrue(target.TryGetValue("Added", out var added, shouldCheckSystem: false));
		Assert.AreEqual("added", added);
	}

	[TestMethod]
	public void When_OverlayFrom_Then_Theme_Dictionaries_Are_Merged_Without_Mutating_Base()
	{
		var originalLight = new ResourceDictionary
		{
			["Kept"] = "kept",
			["Replaced"] = "base",
		};
		var target = new ResourceDictionary();
		target.ThemeDictionaries["Light"] = originalLight;

		var sourceLight = new ResourceDictionary
		{
			["Replaced"] = "optimized",
			["Added"] = "added",
		};
		var source = new ResourceDictionary();
		source.ThemeDictionaries["Light"] = sourceLight;

		target.OverlayFrom(source);

		var overlaidLight = (ResourceDictionary)target.ThemeDictionaries["Light"];
		Assert.AreNotSame(originalLight, overlaidLight, "Overlaying must not mutate a globally shared base theme dictionary.");
		Assert.AreEqual("kept", overlaidLight["Kept"]);
		Assert.AreEqual("optimized", overlaidLight["Replaced"]);
		Assert.AreEqual("added", overlaidLight["Added"]);

		Assert.AreEqual("base", originalLight["Replaced"]);
		Assert.IsFalse(originalLight.ContainsKey("Added", shouldCheckSystem: false));
	}

	private static XamlControlsResources CreateResources(bool optimized)
	{
		var previous = FeatureConfiguration.Style.UseDefaultStyleOptimizations;
		try
		{
			FeatureConfiguration.Style.UseDefaultStyleOptimizations = optimized;

			// The optimized dictionaries are overlaid by the XamlControlsResources constructor.
			return new XamlControlsResources();
		}
		finally
		{
			FeatureConfiguration.Style.UseDefaultStyleOptimizations = previous;
		}
	}

	private static Style GetStyle(ResourceDictionary resources, object key)
	{
		Assert.IsTrue(resources.TryGetValue(key, out var value, shouldCheckSystem: false), $"{key} was not found");
		return (Style)value;
	}

	private static string GetSetterProperties(Style style, DependencyProperty? excludedProperty = null)
		=> string.Join(
			";",
			style.Setters
				.OfType<Setter>()
				.Where(setter => excludedProperty is null || setter.Property != excludedProperty)
				.Select(setter => setter.Property?.Name ?? "?")
				.OrderBy(name => name, StringComparer.Ordinal));

	private static FrameworkElement GetTemplateRoot(Control control)
	{
		var root = VisualTreeHelper.GetChild(control, 0) as FrameworkElement;
		Assert.IsNotNull(root, $"{control.GetType().Name} did not materialize its template");
		return root;
	}

	private static ContentPresenter FindContentPresenter(Control control)
	{
		var presenter = FindDescendant<ContentPresenter>(GetTemplateRoot(control));
		Assert.IsNotNull(presenter, $"{control.GetType().Name} does not contain a ContentPresenter");
		return presenter;
	}

	private static T? FindDescendant<T>(DependencyObject element)
		where T : class
	{
		if (element is T match)
		{
			return match;
		}

		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
		{
			if (FindDescendant<T>(VisualTreeHelper.GetChild(element, i)) is { } childMatch)
			{
				return childMatch;
			}
		}

		return null;
	}

	private static Dictionary<string, VisualState> GetStates(Control control)
	{
		var states = new Dictionary<string, VisualState>(StringComparer.Ordinal);

		foreach (var group in VisualStateManager.GetVisualStateGroups(GetTemplateRoot(control)))
		{
			foreach (var state in group.States)
			{
				states[$"{group.Name}.{state.Name}"] = state;
			}
		}

		Assert.AreNotEqual(0, states.Count, $"{control.GetType().Name} does not define any visual state");

		return states;
	}

	/// <summary>
	/// Counts the timelines matching a predicate across all the storyboards of the provided states.
	/// </summary>
	private static int CountTimelines(IEnumerable<VisualState> states, Func<Timeline, bool> predicate)
		=> states.Sum(state => state.Storyboard is { } storyboard ? storyboard.Children.Count(predicate) : 0);

	/// <summary>
	/// Gets whether the timeline is a zero-duration discrete object animation, which is exactly what
	/// the optimized styles replace by visual state setters.
	/// </summary>
	private static bool IsConvertibleTimeline(Timeline timeline)
		=> timeline is ObjectAnimationUsingKeyFrames { KeyFrames: { Count: 1 } keyFrames }
			&& keyFrames[0] is DiscreteObjectKeyFrame
			&& keyFrames[0].KeyTime.TimeSpan == TimeSpan.Zero;

	private static Windows.UI.Color? ColorOf(Brush brush)
		=> brush is SolidColorBrush solidColorBrush ? solidColorBrush.Color : null;
}
#endif
