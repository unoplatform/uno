#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Resolves the <see cref="ValidationPropertyAttribute"/> declared by a control type to the
/// <see cref="DependencyProperty"/> it names.
/// </summary>
internal static class ValidationPropertyResolver
{
	/// <summary>
	/// Per-type validation-property memoization. Weak-keyed: a <see cref="Type"/> key roots the
	/// LoaderAllocator of its AssemblyLoadContext, so a strongly keyed process-lifetime cache would keep a
	/// collectible context resident forever as soon as one of its controls was bound. The cache is a pure
	/// memoization (evicted entries simply rebuild), which makes weak keys sufficient — no unload hook or
	/// ALC-scoped purge is needed.
	/// </summary>
	private static readonly ConditionalWeakTable<Type, Entry> _cache = new();

	/// <summary>
	/// Gets the validation property of <paramref name="type"/>, or null when it declares none.
	/// </summary>
	/// <remarks>Must be called from the UI thread: <see cref="DependencyProperty.GetProperty"/> throws otherwise.</remarks>
	internal static DependencyProperty? GetValidationProperty(Type type)
		=> _cache.GetValue(type, static t => new Entry(Resolve(t))).Property;

	private static DependencyProperty? Resolve(Type type)
		=> type.GetCustomAttribute<ValidationPropertyAttribute>(inherit: true) is { Name.Length: > 0 } attribute
			? DependencyProperty.GetProperty(type, attribute.Name)
			: null;

	/// <summary>
	/// Boxes the nullable result, which <see cref="ConditionalWeakTable{TKey, TValue}"/> cannot store on its
	/// own. Caching the negative answer is what keeps a non-participating control from re-walking its
	/// attributes on every value change.
	/// </summary>
	private sealed class Entry
	{
		public Entry(DependencyProperty? property) => Property = property;

		public DependencyProperty? Property { get; }
	}
}
