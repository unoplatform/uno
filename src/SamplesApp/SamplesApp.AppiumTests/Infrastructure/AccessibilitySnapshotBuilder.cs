#nullable enable

using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;

namespace SamplesApp.AppiumTests.Infrastructure;

internal static class AccessibilitySnapshotBuilder
{
	public static AccessibilityElementSnapshot Capture(
		IWebDriver driver,
		IPlatformAdapter adapter,
		string id,
		AccessibilitySnapshotFields fields,
		IWebElement element)
	{
		var level = fields.HasFlag(AccessibilitySnapshotFields.Level)
			? adapter.GetLevel(element)
			: null;
		var landmark = fields.HasFlag(AccessibilitySnapshotFields.Landmark)
			? EmptyToNull(adapter.GetLandmark(element))
			: null;

		var snapshot = new AccessibilityElementSnapshot
		{
			Id = id,
			AutomationId = adapter.GetAutomationId(element),
			Role = CanonicalRole.Normalize(adapter.GetRole(element), adapter.Platform, level, landmark),
			Name = adapter.GetName(driver, element),
			Patterns = fields.HasFlag(AccessibilitySnapshotFields.Patterns)
				? adapter.GetSupportedPatterns(element).OrderBy(pattern => pattern, System.StringComparer.Ordinal).ToList()
				: new List<string>(),
			State = new AccessibilityElementState
			{
				Enabled = fields.HasFlag(AccessibilitySnapshotFields.Enabled) ? adapter.GetEnabled(element) : null,
				KeyboardFocusable = fields.HasFlag(AccessibilitySnapshotFields.KeyboardFocusable) ? adapter.GetKeyboardFocusable(element) : null,
				Focused = fields.HasFlag(AccessibilitySnapshotFields.Focused) ? adapter.GetFocused(driver, element) : null,
				Offscreen = fields.HasFlag(AccessibilitySnapshotFields.Offscreen) ? adapter.GetOffscreen(element) : null,
				ToggleState = fields.HasFlag(AccessibilitySnapshotFields.ToggleState) ? EmptyToNull(adapter.GetToggleState(element)) : null,
				Selected = fields.HasFlag(AccessibilitySnapshotFields.Selected) ? adapter.GetSelected(element) : null,
				Expanded = fields.HasFlag(AccessibilitySnapshotFields.Expanded) ? adapter.GetExpanded(element) : null,
				Required = fields.HasFlag(AccessibilitySnapshotFields.Required) ? adapter.GetRequired(element) : null,
				Level = level,
				Landmark = landmark,
				RoleDescription = fields.HasFlag(AccessibilitySnapshotFields.RoleDescription) ? EmptyToNull(adapter.GetRoleDescription(element)) : null,
				LiveSetting = fields.HasFlag(AccessibilitySnapshotFields.LiveSetting) ? EmptyToNull(adapter.GetLiveSetting(element)) : null,
			},
		};

		if (fields.HasFlag(AccessibilitySnapshotFields.Description))
		{
			snapshot.Description = EmptyToNull(adapter.GetDescription(driver, element));
		}

		if (fields.HasFlag(AccessibilitySnapshotFields.Value))
		{
			snapshot.Value = EmptyToNull(adapter.GetValue(element));
		}

		return snapshot;
	}

	private static string? EmptyToNull(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
