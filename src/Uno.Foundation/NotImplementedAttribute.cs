#nullable enable

using System;

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
	/// Creates an instance with C# constants for which the symbol is not implemented.
	/// </summary>
	/// <param name="platforms">The list of not-implemented platforms</param>
	public NotImplementedAttribute(params string[] platforms)
	{
		Platforms = platforms;
	}

	/// <summary>
	/// The list of platforms that are not implemented. When empty, all platforms are not implemented.
	/// </summary>
	public string[]? Platforms { get; }
}
