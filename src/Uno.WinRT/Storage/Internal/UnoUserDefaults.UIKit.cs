#nullable enable

using Foundation;

namespace Uno.Storage.Internal;

/// <summary>
/// The dedicated <see cref="NSUserDefaults"/> suite backing Uno Platform application settings.
/// </summary>
internal static class UnoUserDefaults
{
	/// <summary>
	/// Name of the suite, persisted as <c>Library/Preferences/UnoApplicationData.plist</c>.
	/// </summary>
	internal const string SuiteName = "UnoApplicationData";

	private static readonly NSDictionary _empty = new NSDictionary();

	internal static NSUserDefaults Instance { get; } = new NSUserDefaults(SuiteName, NSUserDefaultsType.SuiteName);

	/// <summary>
	/// The keys the suite itself owns.
	/// </summary>
	/// <remarks>
	/// <see cref="NSUserDefaults.ToDictionary"/> (<c>dictionaryRepresentation</c>) returns the union of every
	/// domain in the instance's search list, which for a suite still includes the application and global
	/// domains. <c>persistentDomainForName:</c> is scoped to the suite, so unrelated native keys stay invisible.
	/// </remarks>
	internal static NSDictionary Domain => Instance.PersistentDomainForName(SuiteName) ?? _empty;
}
