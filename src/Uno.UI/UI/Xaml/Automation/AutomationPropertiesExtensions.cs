using Uno;

namespace Microsoft.UI.Xaml.Automation;

/// <summary>
/// Provides Uno-specific attached properties that extend the standard
/// <see cref="AutomationProperties"/> metadata for accessibility.
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
/// xmlns:uno="using:Uno.UI.Xaml.Automation"
/// &lt;Button uno:AutomationPropertiesExtensions.Role="tab" /&gt;
/// </code>
/// </remarks>
[UnoOnly]
public static class AutomationPropertiesExtensions
{
	/// <summary>
	/// Identifies the <c>Role</c> attached dependency property, which lets
	/// callers override the platform accessibility role for an element.
	/// </summary>
	/// <remarks>
	/// When set, the value takes precedence over the role that
	/// <see cref="AutomationProperties.FindHtmlRole"/> would normally derive
	/// from the element type or its <see cref="Microsoft.UI.Xaml.Automation.Peers.AutomationPeer"/>.
	/// </remarks>
	public static DependencyProperty RoleProperty { get; } =
		DependencyProperty.RegisterAttached(
			"Role",
			typeof(string),
			typeof(AutomationPropertiesExtensions),
			new FrameworkPropertyMetadata(default(string), OnRoleChanged));

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
#if __WASM__
		if (dependencyObject is UIElement uiElement)
		{
			var role = (string)args.NewValue;
			if (!string.IsNullOrEmpty(role))
			{
				uiElement.SetAttribute("role", role);
			}
			else
			{
				// Clearing the override — re-derive from the element type.
				var defaultRole = AutomationProperties.FindHtmlRole(uiElement);
				if (defaultRole != null)
				{
					uiElement.SetAttribute("role", defaultRole);
				}
				else
				{
					uiElement.RemoveAttribute("role");
				}
			}
		}
#endif
		// Skia: the accessibility tree reads the role via FindHtmlRole,
		//       which already checks this property. No extra push needed.
		// Android / iOS: platform-specific mapping is applied through
		//       FrameworkElementAutomationPeer on the respective platform files.
	}
}
