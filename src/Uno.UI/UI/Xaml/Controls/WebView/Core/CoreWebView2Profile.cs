#nullable enable

using System;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Provides access to the profile the WebView is running under.
/// </summary>
/// <remarks>
/// This is a stateless facade over the owning <see cref="CoreWebView2"/>: the native view it resolves is
/// replaced whenever the control is re-templated, so the capability is looked up on every access rather than
/// captured once.
/// </remarks>
public partial class CoreWebView2Profile
{
	private const string TypeName = "Microsoft.Web.WebView2.Core.CoreWebView2Profile";

	private readonly CoreWebView2 _owner;

	internal CoreWebView2Profile(CoreWebView2 owner) => _owner = owner;

	/// <summary>
	/// Gets the name of the profile, which is empty when the host did not request a named profile.
	/// </summary>
	/// <remarks>
	/// On Windows this is currently always empty: the WebView is created without controller options, so no
	/// profile name is requested, even though <see cref="ProfilePath"/> resolves to the default profile
	/// directory. Use <see cref="ProfilePath"/> to identify the profile.
	/// </remarks>
	public string ProfileName => Native(nameof(ProfileName)).ProfileName;

	/// <summary>
	/// Gets the full path of the profile directory.
	/// </summary>
	public string ProfilePath => Native(nameof(ProfilePath)).ProfilePath;

	/// <summary>
	/// Gets whether the profile is in InPrivate mode.
	/// </summary>
	public bool IsInPrivateModeEnabled => Native(nameof(IsInPrivateModeEnabled)).IsInPrivateModeEnabled;

	/// <summary>
	/// Gets or sets the default download folder path.
	/// </summary>
	public string DefaultDownloadFolderPath
	{
		get => Native(nameof(DefaultDownloadFolderPath)).DefaultDownloadFolderPath;
		set => Native(nameof(DefaultDownloadFolderPath)).DefaultDownloadFolderPath =
			value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Gets or sets the preferred color scheme for the WebViews associated with this profile.
	/// </summary>
	public CoreWebView2PreferredColorScheme PreferredColorScheme
	{
		get => Native(nameof(PreferredColorScheme)).PreferredColorScheme;
		set => Native(nameof(PreferredColorScheme)).PreferredColorScheme = value;
	}

	/// <summary>
	/// Clears every kind of browsing data from the profile.
	/// </summary>
	public IAsyncAction ClearBrowsingDataAsync()
		=> ClearBrowsingDataCore("ClearBrowsingDataAsync()", null, null, null);

	/// <summary>
	/// Clears the specified kinds of browsing data from the profile.
	/// </summary>
	public IAsyncAction ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds dataKinds)
		=> ClearBrowsingDataCore("ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds dataKinds)", dataKinds, null, null);

	/// <summary>
	/// Clears the specified kinds of browsing data created between <paramref name="startTime"/> and <paramref name="endTime"/>.
	/// </summary>
	public IAsyncAction ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds dataKinds, DateTimeOffset startTime, DateTimeOffset endTime)
		=> ClearBrowsingDataCore(
			"ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds dataKinds, DateTimeOffset startTime, DateTimeOffset endTime)",
			dataKinds,
			startTime,
			endTime);

	private IAsyncAction ClearBrowsingDataCore(string memberName, CoreWebView2BrowsingDataKinds? dataKinds, DateTimeOffset? startTime, DateTimeOffset? endTime)
		=> AsyncAction.FromTask(async _ =>
		{
			await _owner.EnsureNativeWebViewAsync();

			// Resolved after the await: EnsureNativeWebViewAsync stays completed across re-templating, so the
			// native view may have been replaced (or removed) since the task was created.
			await _owner
				.RequireCapability<ISupportsBrowsingDataClearing>(TypeName, memberName)
				.ClearBrowsingDataAsync(dataKinds, startTime, endTime);
		});

	private ISupportsWebViewProfile Native(string memberName)
		=> _owner.RequireCapability<ISupportsWebViewProfile>(TypeName, memberName);
}
