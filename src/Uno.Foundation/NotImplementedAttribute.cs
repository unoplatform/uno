#nullable enable


namespace Uno;

/// <summary>
/// Marks a member or symbol as not implemented by Uno Platform.
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
public sealed class NotImplementedAttribute : Attribute
{
	/// <summary>
	/// Creates an instance
	/// </summary>
	public NotImplementedAttribute() { }

	/// <summary>
	/// Creates an instance with the Uno build flavors for which the symbol is not implemented.
	/// </summary>
	/// <param name="platforms">
	/// The build flavors, named by the symbol their build defines. In the per-flavor assemblies (Uno.WinRT,
	/// Uno.Foundation, Uno.UI.Dispatching): <c>__ANDROID__</c>, <c>__IOS__</c>, <c>__TVOS__</c>,
	/// <c>__APPLE_UIKIT__</c> (iOS and tvOS), <c>__WASM__</c>, <c>__SKIA__</c> (desktop) and
	/// <c>__NETSTD_REFERENCE__</c> (the reference assembly). Uno.UI and the libraries built on it have a single
	/// Skia build, used by every Uno target, so <c>__SKIA__</c> there means every Uno target; other values there are
	/// matched against the consuming project's conditional compilation symbols.
	/// </param>
	public NotImplementedAttribute(params string[] platforms)
	{
		Platforms = platforms;
	}

	/// <summary>
	/// The build flavors that are not implemented. When empty, all platforms are not implemented.
	/// </summary>
	public string[]? Platforms { get; }
}
