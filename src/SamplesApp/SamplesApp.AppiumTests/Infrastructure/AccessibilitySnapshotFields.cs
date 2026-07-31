#nullable enable

using System;

namespace SamplesApp.AppiumTests.Infrastructure;

[Flags]
public enum AccessibilitySnapshotFields
{
	None = 0,
	Patterns = 1 << 0,
	Value = 1 << 1,
	Enabled = 1 << 2,
	KeyboardFocusable = 1 << 3,
	Focused = 1 << 4,
	Offscreen = 1 << 5,
	ToggleState = 1 << 6,
	Selected = 1 << 7,
	Expanded = 1 << 8,
	Required = 1 << 9,
	Level = 1 << 10,
	Landmark = 1 << 11,
	RoleDescription = 1 << 12,
	LiveSetting = 1 << 13,
	Description = 1 << 14,
}

