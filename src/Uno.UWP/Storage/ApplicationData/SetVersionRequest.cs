using Uno.Helpers;
using Windows.Foundation;

namespace Windows.Storage;

/// <summary>
/// Provides data when an app sets the version of the application data in its app data store.
/// </summary>
public partial class SetVersionRequest
{
	internal SetVersionRequest(uint currentVersion, uint desiredVersion)
	{
		CurrentVersion = currentVersion;
		DesiredVersion = desiredVersion;
		DeferralManager = new DeferralManager<SetVersionDeferral>(c => new SetVersionDeferral(c), requiresUIThread: false);
	}

	/// <summary>
	/// Gets the current version.
	/// </summary>
	public uint CurrentVersion { get; }

	/// <summary>
	/// Gets the requested version.
	/// </summary>
	public uint DesiredVersion { get; }

	internal DeferralManager<SetVersionDeferral> DeferralManager { get; }

	/// <summary>
	/// Requests that the set version request be delayed.
	/// </summary>
	/// <returns>The set version deferral.</returns>
	public SetVersionDeferral GetDeferral() => DeferralManager.GetDeferral();
}
