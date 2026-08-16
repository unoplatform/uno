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
			typeof(CommandBar),
			"DefaultButtonStyle",
			"AccentButtonStyle",
			"NavigationBackButtonNormalStyle",
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
			Assert.AreEqual(GetSetterProperties(defaultStyle), GetSetterProperties(optimizedStyle), $"Setters changed for {key}");
		}
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

	private static string GetSetterProperties(Style style)
		=> string.Join(
			";",
			style.Setters
				.OfType<Setter>()
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
