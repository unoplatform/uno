namespace Windows.ApplicationModel;

/// <summary>
/// Provides information about the package's signature and the kind of certificate used to create it.
/// </summary>
public enum PackageSignatureKind
{
	/// <summary>
	/// The package is not signed. For example, a Visual Studio project that is running from layout (F5).
	/// </summary>
	None = 0,

	/// <summary>
	/// The package is signed with a trusted certificate that is not categorized as Enterirse, Store, or System.
	/// For example, an application signed by an ISV for distrubution outside of the application store.
	/// </summary>
	Developer = 1,

	/// <summary>
	/// The package is signed using a certificate issued by a root authority that has higher verification requirements than general public authorities.
	/// </summary>
	Enterprise = 2,

	/// <summary>
	/// The package is signed by an application store.
	/// </summary>
	Store = 3,

	/// <summary>
	/// The package is signed by a certificate that's also used to sign the operating system.
	/// These packages can have additional capabilities not granted to normal apps.
	/// For example, the built-in system apps.
	/// </summary>
	System = 4,
}
