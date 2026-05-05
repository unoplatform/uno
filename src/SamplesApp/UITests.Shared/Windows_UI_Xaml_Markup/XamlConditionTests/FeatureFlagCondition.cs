// Adapted from the WinUI 3 Gallery sample for IXamlCondition
// (https://github.com/microsoft/WinUI-Gallery/blob/main/WinUIGallery/Samples/ControlPages/FeatureFlagCondition.cs).
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace UITests.Shared.Windows_UI_Xaml_Markup.XamlConditionTests;

// Sample implementation of IXamlCondition.
//
// A custom XAML condition is evaluated by the runtime XAML reader (e.g.
// XamlReader.Load) when it processes a conditional namespace. The result
// for a given (condition, argument) pair is cached for the lifetime of the
// process, so flipping the underlying flag at runtime will not re-evaluate
// already-parsed XAML.
//
// On Uno Platform the result of IXamlCondition is honored at runtime by
// XamlReader.Load. For pages compiled by the XAML source generator, both
// branches of a custom condition are currently included at compile time;
// the built-in IsApiContractPresent / IsTypePresent predicates remain the
// recommended way to gate compiled XAML.
public sealed partial class FeatureFlagCondition : DependencyObject, IXamlCondition
{
	public static IDictionary<string, bool> FeatureFlags { get; } = new Dictionary<string, bool>
	{
		["NewExperience"] = true,
		["LegacyMode"] = false,
	};

	public bool Evaluate(string argument)
		=> FeatureFlags.TryGetValue(argument, out var enabled) && enabled;
}
