#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace SamplesApp.AppiumTests.Infrastructure;

public sealed record BaselineElementDefinition(string Id, string AutomationId)
{
	public IReadOnlyCollection<AppiumPlatform>? Platforms { get; init; }

	public AccessibilitySnapshotFields DefaultFields { get; init; } = AccessibilitySnapshotFields.None;

	public AccessibilitySnapshotFields? WindowsFields { get; init; }

	public AccessibilitySnapshotFields? MacFields { get; init; }

	public AccessibilitySnapshotFields? WasmFields { get; init; }

	public bool AppliesTo(AppiumPlatform platform)
		=> Platforms is null || Platforms.Contains(platform);

	public AccessibilitySnapshotFields FieldsFor(AppiumPlatform platform)
		=> platform switch
		{
			AppiumPlatform.Windows => WindowsFields ?? DefaultFields,
			AppiumPlatform.Mac => MacFields ?? DefaultFields,
			AppiumPlatform.Wasm => WasmFields ?? DefaultFields,
			_ => DefaultFields,
		};
}

public sealed record AccessibilitySnapshotDefinition(
	string Sample,
	string SnapshotId,
	IReadOnlyList<BaselineElementDefinition> Elements)
{
	public IEnumerable<BaselineElementDefinition> ElementsFor(AppiumPlatform platform)
		=> Elements.Where(element => element.AppliesTo(platform));
}

internal static class AccessibilityScreenReaderIds
{
	public const string AccessibilityPageHeading = "AccessibilityPageHeading";
	public const string PhotosSearchInput = "PhotosSearchInput";
	public const string HelpTextButton = "HelpTextButton";
	public const string NavigationLandmarkRegion = "NavigationLandmarkRegion";
	public const string MainLandmarkRegion = "MainLandmarkRegion";
	public const string CustomLandmarkRegion = "CustomLandmarkRegion";
	public const string EnableNotificationsCheckBox = "EnableNotificationsCheckBox";
	public const string SizeSmallRadioButton = "SizeSmallRadioButton";
	public const string SizeMediumRadioButton = "SizeMediumRadioButton";
	public const string SizeLargeRadioButton = "SizeLargeRadioButton";
	public const string FavoriteColorComboBox = "FavoriteColorComboBox";
	public const string FavoriteColorOptionRed = "FavoriteColorOptionRed";
	public const string FavoriteColorOptionGreen = "FavoriteColorOptionGreen";
	public const string FavoriteColorOptionBlue = "FavoriteColorOptionBlue";
	public const string CommentsTextBox = "CommentsTextBox";
	public const string RequiredFullNameTextBox = "RequiredFullNameTextBox";
	public const string LiveRegionText = "LiveRegionText";
	public const string LiveRegionUpdateButton = "LiveRegionUpdateButton";
	public const string VisibilityTargetButton = "VisibilityTargetButton";
	public const string PlainTextBlock = "PlainTextBlock";
	public const string CombinedTextBox = "CombinedTextBox";
	public const string CombinedDisableButton = "CombinedDisableButton";
	public const string CombinedEnableButton = "CombinedEnableButton";
}

internal static class AccessibilityScreenReaderSnapshotDefinition
{
	private static readonly AppiumPlatform[] s_allPlatforms =
	{
		AppiumPlatform.Windows,
		AppiumPlatform.Mac,
		AppiumPlatform.Wasm,
	};

	private static readonly AppiumPlatform[] s_windowsAndWasm =
	{
		AppiumPlatform.Windows,
		AppiumPlatform.Wasm,
	};

	private static readonly AppiumPlatform[] s_wasmOnly =
	{
		AppiumPlatform.Wasm,
	};

	public const string SampleName = "Automation/Accessibility_ScreenReader";
	public const string SampleQuery = "sample=Automation/Accessibility_ScreenReader";

	public static readonly AccessibilitySnapshotDefinition Definition = new(
		SampleName,
		"Automation_AccessibilityScreenReader",
		new List<BaselineElementDefinition>
		{
			new(AccessibilityScreenReaderIds.AccessibilityPageHeading, AccessibilityScreenReaderIds.AccessibilityPageHeading)
			{
				Platforms = s_windowsAndWasm,
				WindowsFields = AccessibilitySnapshotFields.Level,
				WasmFields = AccessibilitySnapshotFields.Level,
			},
			new(AccessibilityScreenReaderIds.PhotosSearchInput, AccessibilityScreenReaderIds.PhotosSearchInput)
			{
				Platforms = s_allPlatforms,
				DefaultFields = AccessibilitySnapshotFields.Patterns,
			},
			new(AccessibilityScreenReaderIds.HelpTextButton, AccessibilityScreenReaderIds.HelpTextButton)
			{
				Platforms = s_wasmOnly,
				WasmFields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.Description,
			},
			new(AccessibilityScreenReaderIds.NavigationLandmarkRegion, AccessibilityScreenReaderIds.NavigationLandmarkRegion)
			{
				Platforms = s_windowsAndWasm,
				WindowsFields = AccessibilitySnapshotFields.Landmark,
				WasmFields = AccessibilitySnapshotFields.Landmark,
			},
			new(AccessibilityScreenReaderIds.MainLandmarkRegion, AccessibilityScreenReaderIds.MainLandmarkRegion)
			{
				Platforms = s_windowsAndWasm,
				WindowsFields = AccessibilitySnapshotFields.Landmark,
				WasmFields = AccessibilitySnapshotFields.Landmark,
			},
			new(AccessibilityScreenReaderIds.CustomLandmarkRegion, AccessibilityScreenReaderIds.CustomLandmarkRegion)
			{
				Platforms = s_windowsAndWasm,
				WindowsFields = AccessibilitySnapshotFields.Landmark,
				WasmFields = AccessibilitySnapshotFields.Landmark | AccessibilitySnapshotFields.RoleDescription,
			},
			new(AccessibilityScreenReaderIds.EnableNotificationsCheckBox, AccessibilityScreenReaderIds.EnableNotificationsCheckBox)
			{
				Platforms = s_allPlatforms,
				DefaultFields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.ToggleState | AccessibilitySnapshotFields.Enabled,
			},
			new(AccessibilityScreenReaderIds.SizeSmallRadioButton, AccessibilityScreenReaderIds.SizeSmallRadioButton)
			{
				Platforms = s_allPlatforms,
				DefaultFields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.Selected,
			},
			new(AccessibilityScreenReaderIds.SizeMediumRadioButton, AccessibilityScreenReaderIds.SizeMediumRadioButton)
			{
				Platforms = s_allPlatforms,
				DefaultFields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.Selected,
			},
			new(AccessibilityScreenReaderIds.FavoriteColorComboBox, AccessibilityScreenReaderIds.FavoriteColorComboBox)
			{
				Platforms = s_allPlatforms,
				DefaultFields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.Value,
			},
			new(AccessibilityScreenReaderIds.CommentsTextBox, AccessibilityScreenReaderIds.CommentsTextBox)
			{
				Platforms = s_allPlatforms,
				DefaultFields = AccessibilitySnapshotFields.Patterns,
			},
			new(AccessibilityScreenReaderIds.RequiredFullNameTextBox, AccessibilityScreenReaderIds.RequiredFullNameTextBox)
			{
				Platforms = s_allPlatforms,
				DefaultFields = AccessibilitySnapshotFields.Patterns,
				WindowsFields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.Required,
				WasmFields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.Required,
			},
			new(AccessibilityScreenReaderIds.LiveRegionText, AccessibilityScreenReaderIds.LiveRegionText)
			{
				Platforms = s_allPlatforms,
				WasmFields = AccessibilitySnapshotFields.LiveSetting,
			},
			new(AccessibilityScreenReaderIds.VisibilityTargetButton, AccessibilityScreenReaderIds.VisibilityTargetButton)
			{
				Platforms = s_allPlatforms,
				DefaultFields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.Enabled,
			},
			new(AccessibilityScreenReaderIds.PlainTextBlock, AccessibilityScreenReaderIds.PlainTextBlock)
			{
				Platforms = s_allPlatforms,
			},
			new(AccessibilityScreenReaderIds.CombinedTextBox, AccessibilityScreenReaderIds.CombinedTextBox)
			{
				Platforms = s_allPlatforms,
				DefaultFields = AccessibilitySnapshotFields.Patterns | AccessibilitySnapshotFields.Enabled,
			},
		});

	public static IEnumerable<AccessibilitySnapshotDefinition> All
		=> new[] { Definition };
}
