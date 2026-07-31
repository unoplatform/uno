#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SamplesApp.AppiumTests.Infrastructure;

public sealed class AccessibilitySnapshot
{
	[JsonPropertyName("schema")]
	public int Schema { get; set; }

	[JsonPropertyName("sample")]
	public string Sample { get; set; } = string.Empty;

	[JsonPropertyName("flavor")]
	public string Flavor { get; set; } = string.Empty;

	[JsonPropertyName("elements")]
	public List<AccessibilityElementSnapshot> Elements { get; set; } = new();
}

public sealed class AccessibilityElementSnapshot
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("automationId")]
	public string AutomationId { get; set; } = string.Empty;

	[JsonPropertyName("role")]
	public string Role { get; set; } = string.Empty;

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("description")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Description { get; set; }

	[JsonPropertyName("value")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Value { get; set; }

	[JsonPropertyName("patterns")]
	public List<string> Patterns { get; set; } = new();

	[JsonPropertyName("state")]
	public AccessibilityElementState State { get; set; } = new();
}

public sealed class AccessibilityElementState
{
	[JsonPropertyName("enabled")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Enabled { get; set; }

	[JsonPropertyName("keyboardFocusable")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? KeyboardFocusable { get; set; }

	[JsonPropertyName("focused")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Focused { get; set; }

	[JsonPropertyName("offscreen")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Offscreen { get; set; }

	[JsonPropertyName("toggleState")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ToggleState { get; set; }

	[JsonPropertyName("selected")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Selected { get; set; }

	[JsonPropertyName("expanded")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Expanded { get; set; }

	[JsonPropertyName("required")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Required { get; set; }

	[JsonPropertyName("level")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? Level { get; set; }

	[JsonPropertyName("landmark")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Landmark { get; set; }

	[JsonPropertyName("roleDescription")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RoleDescription { get; set; }

	[JsonPropertyName("liveSetting")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? LiveSetting { get; set; }
}

