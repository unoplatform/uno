using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;

#if HAS_UNO
using Uno;
#endif

namespace Uno.UI.Toolkit;

/// <summary>
/// Provides attached properties that extend the standard
/// <see cref="Microsoft.UI.Xaml.Automation.AutomationProperties"/>
/// metadata for accessibility.
/// </summary>
/// <remarks>
/// These properties are <b>not</b> part of the WinUI contract. They exist because
/// WinUI has no direct API for setting an arbitrary accessibility role string.
/// On platforms whose native accessibility framework does not support a raw string
/// role (Android, iOS), the value is mapped to the closest native equivalent or
/// ignored with a diagnostic.
///
/// XAML usage:
/// <code>
/// xmlns:uut="using:Uno.UI.Toolkit"
/// &lt;Button uut:AutomationPropertiesExtensions.Role="tab" /&gt;
/// </code>
/// </remarks>
public static class AutomationPropertiesExtensions
{
	/// <summary>
	/// Identifies the <c>Role</c> attached dependency property, which lets
	/// callers override the platform accessibility role for an element.
	/// </summary>
	/// <remarks>
	/// When set, the value takes precedence over the role that would normally
	/// be derived from the element type or its
	/// <see cref="Microsoft.UI.Xaml.Automation.Peers.AutomationPeer"/>.
	/// On WASM the value is applied as the HTML <c>role</c> attribute.
	/// On Skia the accessibility tree reads the role at query time.
	/// On Windows (WinUI) the property is accepted but has no effect at this time.
	/// </remarks>
	public static DependencyProperty RoleProperty { get; } =
		DependencyProperty.RegisterAttached(
			"Role",
			typeof(string),
			typeof(AutomationPropertiesExtensions),
			new PropertyMetadata(default(string), OnRoleChanged));

	/// <summary>
	/// Gets the accessibility role override for the specified element.
	/// </summary>
	/// <param name="element">The element to query.</param>
	/// <returns>
	/// The role string previously set via <see cref="SetRole"/>,
	/// or <see langword="null"/> if no override has been applied.
	/// </returns>
	public static string GetRole(DependencyObject element) =>
		(string)element.GetValue(RoleProperty);

	/// <summary>
	/// Sets the accessibility role override for the specified element.
	/// </summary>
	/// <param name="element">The element whose role should be overridden.</param>
	/// <param name="value">
	/// The role string to apply (e.g. <c>"tab"</c>, <c>"tablist"</c>,
	/// <c>"navigation"</c>). Set to <see langword="null"/> or empty to
	/// clear the override and fall back to the default behaviour.
	/// </param>
	public static void SetRole(DependencyObject element, string value) =>
		element.SetValue(RoleProperty, value);

	/// <summary>
	/// Callback invoked when the <see cref="RoleProperty"/> value changes.
	/// Propagates the new role to the platform accessibility layer immediately
	/// (e.g. sets the HTML <c>role</c> attribute on WASM, updates the
	/// accessibility tree on Skia).
	/// </summary>
	private static void OnRoleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if __WASM__ || __SKIA__
		if (dependencyObject is UIElement uiElement)
		{
			var role = (string)args.NewValue;

			// Populate the internal bridge property so that AutomationProperties.FindHtmlRole
			// can resolve the override on both native WASM and Skia-WASM accessibility paths.
			AutomationProperties.SetRoleOverride(uiElement, role);

#if __WASM__
			// On native WASM the element maps to a real DOM node, so push the value
			// directly to the HTML role attribute as well.
			if (!string.IsNullOrEmpty(role))
			{
				uiElement.SetAttribute("role", role);
			}
			else
			{
				// Clearing the override — remove the explicit role attribute.
				// The default role will be re-applied by AutomationProperties
				// when the automation ID or name is next evaluated.
				uiElement.RemoveAttribute("role");
			}
#endif
		}
#endif
	}
}
