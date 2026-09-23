#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Maps a control type to the dependency property that carries its input, i.e. the property whose binding
/// source is inspected for <see cref="System.ComponentModel.INotifyDataErrorInfo"/> errors.
/// </summary>
/// <remarks>
/// Reading a type that has no entry resolves its <see cref="InputValidationPropertyAttribute"/> and memoizes
/// the answer, the negative one included. Writing one overrides that, which is how a control that cannot
/// carry the attribute — sealed, or from a library that does not reference Uno — takes part:
/// <code>
/// FeatureConfiguration.InputValidation.ValidationProperties[typeof(ThirdPartyEntry)] = ThirdPartyEntry.TextProperty;
/// </code>
/// <para>
/// Register before the control is bound: participation is decided when the binding is set, so a registration
/// arriving afterwards is only picked up on the next rebind.
/// </para>
/// </remarks>
public sealed class InputValidationPropertyMap
{
	/// <summary>
	/// Weak-keyed: a <see cref="Type"/> key roots the LoaderAllocator of its AssemblyLoadContext, so a
	/// strongly keyed process-lifetime map would keep a collectible context resident forever as soon as one
	/// of its controls was bound. Every entry is either a memoized attribute lookup or a registration made by
	/// code that unloads with the context, so eviction is always safe — no unload hook or ALC-scoped purge is
	/// needed.
	/// </summary>
	private readonly ConditionalWeakTable<Type, Entry> _entries = new();

	internal InputValidationPropertyMap()
	{
	}

	/// <summary>
	/// Gets the validation property of <paramref name="type"/>, resolving its attribute on first read, or
	/// sets the property it is to use — null to opt it out of an attribute it would otherwise inherit.
	/// </summary>
	/// <remarks>
	/// The getter must be called from the UI thread: <see cref="DependencyProperty.GetProperty"/> throws
	/// otherwise. A write supersedes an already memoized answer, and belongs on the UI thread too — the map
	/// follows the single-threaded discipline of the property system rather than being a synchronization
	/// point of its own.
	/// </remarks>
	public DependencyProperty? this[Type type]
	{
		get => _entries.GetValue(type, static t => new Entry(Resolve(t))).Property;
		set => _entries.AddOrUpdate(type, new Entry(value));
	}

	/// <summary>
	/// Drops the entry of <paramref name="type"/>, so that the next read resolves its attribute again.
	/// Returns false when there was none.
	/// </summary>
	/// <remarks>Expected on the UI thread, as the indexer is.</remarks>
	public bool Remove(Type type) => _entries.Remove(type);

	private static DependencyProperty? Resolve(Type type)
		=> type.GetCustomAttribute<InputValidationPropertyAttribute>(inherit: true) is { Name.Length: > 0 } attribute
			? DependencyProperty.GetProperty(type, attribute.Name)
			: null;

	/// <summary>
	/// Boxes the nullable property, which <see cref="ConditionalWeakTable{TKey, TValue}"/> cannot store on
	/// its own. Holding the negative answer is what keeps a non-participating control from re-walking its
	/// attributes on every value change.
	/// </summary>
	private sealed class Entry
	{
		public Entry(DependencyProperty? property) => Property = property;

		public DependencyProperty? Property { get; }
	}
}
