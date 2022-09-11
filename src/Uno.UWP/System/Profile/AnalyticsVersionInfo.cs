using System;
using Uno;

namespace Windows.System.Profile;

/// <summary>
/// Provides version information about the device family.
/// </summary>
public partial class AnalyticsVersionInfo
{
	public AnalyticsVersionInfo() => Initialize();

	partial void Initialize();

	/// <summary>
	/// Gets a string that represents the type of device the application is running on.
	/// </summary>
	public string DeviceFamily { get; private set; } = $"{Environment.OSVersion.Platform}.Desktop";

	/// <summary>
	/// Gets the version within the device family.
	/// </summary>
	/// <remarks>
	/// Needs to be parsable long number.
	/// </remarks>
	[NotImplemented("__WASM__", "__SKIA__")]
	public string DeviceFamilyVersion { get; private set; } = "0";
}
