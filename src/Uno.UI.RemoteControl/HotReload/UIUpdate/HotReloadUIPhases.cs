#nullable enable

using System;

namespace Uno.HotReload.Client;

/// <summary>
/// Phases of UI update that can be paused independently while a hot-reload
/// delta is processed.
/// </summary>
[Flags]
public enum HotReloadUIPhases
{
	/// <summary>No phase is paused.</summary>
	None = 0,

	/// <summary>
	/// Pauses the application of <c>*GlobalStaticResources</c> initialization
	/// and merged resource dictionary refresh that normally fires on every
	/// hot-reload delta.
	/// </summary>
	ResourceDictionaries = 1 << 0,

	/// <summary>
	/// Pauses the visual tree traversal that swaps elements with their
	/// hot-reload-replacement type.
	/// </summary>
	VisualTree = 1 << 1,

	/// <summary>Pauses every phase.</summary>
	All = ResourceDictionaries | VisualTree,
}
