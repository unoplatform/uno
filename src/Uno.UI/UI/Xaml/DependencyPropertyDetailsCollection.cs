#nullable enable

using System;
using Microsoft.UI.Xaml.Data;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// A <see cref="DependencyPropertyDetails"/> collection
	/// </summary>
	partial class DependencyPropertyDetailsCollection
	{
		// The owning DependencyObject is held strongly, which is safe only because this collection never
		// escapes it: the DO <-> collection cycle is then unreachable as a unit and the tracing GC
		// collects it whole. Holding it strongly avoids renting a pooled weak self-handle
		// (ManagedGCHandle) per DependencyObject.
		// INVARIANT: never store this collection, or a closure capturing it, outside the owning
		// DependencyObject. Doing so would transitively pin the owner and every object it references.
		private readonly DependencyObject _owner;
		// Null when the owner is not a FrameworkElement (DataContext is FrameworkElement-only).
		private readonly DependencyProperty? _dataContextProperty;
		private DependencyPropertyDetails? _dataContextPropertyDetails;

		// Open-addressed by DependencyProperty.UniqueId with linear probing, kept at most half full so
		// probe chains stay short. Arrays are allocated, never pooled: there is no disposal path to return
		// them, and an array that is replaced while GetAllDetails is being enumerated must stay intact.
		private DependencyPropertyDetails?[] _entries = _noEntries;
		private int _count;

		// The first two details sit in fields until a third arrives: most objects (a segment, a brush, a transform)
		// set one or two properties, and the table's array would cost more than the details themselves.
		private DependencyPropertyDetails? _slot0;
		private DependencyPropertyDetails? _slot1;

		// A shared single empty slot rather than an empty array, so a lookup needs no length check. It is never
		// written to: the first Add always grows.
		private static readonly DependencyPropertyDetails?[] _noEntries = new DependencyPropertyDetails?[1];

		private const int MinimumCapacity = 4;

		private DependencyObject Owner => _owner;

		/// <summary>
		/// Creates an instance using the specified DependencyObject <see cref="Type"/>
		/// </summary>
		public DependencyPropertyDetailsCollection(DependencyObject owner, DependencyProperty? dataContextProperty)
		{
			_owner = owner;

			_dataContextProperty = dataContextProperty;
		}

		internal void CloneToForHotReload(DependencyPropertyDetailsCollection other, DependencyObject store, DependencyObject otherStore)
		{
			// Bindings are re-created only once every value is cloned: a binding reads its DataContext when it is set,
			// and the cloned values are written without notifying it. Entries are in hash order, so a single pass
			// would make that depend on the properties' UniqueIds.
			foreach (var oldDetails in GetAllDetails())
			{
				if (oldDetails is not null && GetHotReloadTarget(other, oldDetails) is { } newDetails)
				{
					oldDetails.CloneToForHotReload(newDetails);
				}
			}

			foreach (var oldDetails in GetAllDetails())
			{
				// This may not work well for x:Bind, we will investigate proper support for x:Bind.
				// Though, anything will be done now for x:Bind will need to be re-worked if we refactored
				// x:Bind to be fully compiled, as in WinUI.
				if (oldDetails is not null
					&& oldDetails.GetBinding() is { ParentBinding: { } binding }
					&& GetHotReloadTarget(other, oldDetails) is { } newDetails)
				{
					var newBinding = new Binding(binding.Path, binding.Converter, binding.ConverterParameter);
					var newSource = binding.Source;
					if (newSource is DependencyObject oldStore && oldStore == store)
					{
						newSource = otherStore.ActualInstance;
					}

					newBinding.Source = newSource;
					newBinding.Mode = binding.Mode;
					newBinding.TargetNullValue = binding.TargetNullValue;
					newBinding.ElementName = binding.ElementName;
					newBinding.ElementNameSubject = binding.ElementNameSubject;
					newBinding.FallbackValue = binding.FallbackValue;
					if (binding.RelativeSource is { } relativeSource)
					{
						newBinding.RelativeSource = new RelativeSource(relativeSource.Mode);
					}

					otherStore.SetBindingInternal(newDetails.Property, newBinding);
				}
			}
		}

		private static DependencyPropertyDetails? GetHotReloadTarget(DependencyPropertyDetailsCollection other, DependencyPropertyDetails oldDetails)
			=> DependencyProperty.GetProperty(oldDetails.Property.OwnerType, oldDetails.Property.Name) is { } newDP
				? other.GetPropertyDetails(newDP)
				: null;

		public DependencyPropertyDetails? DataContextPropertyDetails
			=> _dataContextProperty is { } dataContextProperty
				? _dataContextPropertyDetails ??= GetPropertyDetails(dataContextProperty)
				: null;

		/// <summary>
		/// Gets the <see cref="DependencyPropertyDetails"/> for a specific <see cref="DependencyProperty"/>
		/// </summary>
		/// <param name="property">A dependency property</param>
		/// <returns>The details of the property</returns>
		public DependencyPropertyDetails GetPropertyDetails(DependencyProperty property)
			=> TryGetPropertyDetails(property, forceCreate: true)!;

		/// <summary>
		/// Finds the <see cref="DependencyPropertyDetails"/> for a specific <see cref="DependencyProperty"/> if it exists.
		/// </summary>
		/// <param name="property">A dependency property</param>
		/// <returns>The details of the property if it exists, otherwise null.</returns>
		public DependencyPropertyDetails? FindPropertyDetails(DependencyProperty property)
			=> TryGetPropertyDetails(property, forceCreate: false);

		private DependencyPropertyDetails? TryGetPropertyDetails(DependencyProperty property, bool forceCreate)
		{
			var entries = _entries;
			if (ReferenceEquals(entries, _noEntries))
			{
				if (_slot0 is { } slot0 && ReferenceEquals(slot0.Property, property))
				{
					return slot0;
				}

				if (_slot1 is { } slot1 && ReferenceEquals(slot1.Property, property))
				{
					return slot1;
				}

				return forceCreate ? Add(property) : null;
			}

			// The capacity is a power of two, so masking is the modulo.
			var mask = entries.Length - 1;
			var index = property.UniqueId & mask;

			while (entries[index] is { } entry)
			{
				if (ReferenceEquals(entry.Property, property))
				{
					return entry;
				}

				index = (index + 1) & mask;
			}

			return forceCreate ? Add(property) : null;
		}

		private DependencyPropertyDetails Add(DependencyProperty property)
		{
			if (ReferenceEquals(_entries, _noEntries))
			{
				if (_slot0 is null)
				{
					_count++;
					return _slot0 = new DependencyPropertyDetails(property, property == _dataContextProperty);
				}

				if (_slot1 is null)
				{
					_count++;
					return _slot1 = new DependencyPropertyDetails(property, property == _dataContextProperty);
				}

				// A third property: the slotted details move into the table, which holds every entry from here on.
				var spilled = new DependencyPropertyDetails?[8];
				Insert(spilled, _slot0);
				Insert(spilled, _slot1);
				_entries = spilled;
				_slot0 = null;
				_slot1 = null;
			}

			if ((_count + 1) * 2 > _entries.Length)
			{
				Grow();
			}

			var entries = _entries;
			var mask = entries.Length - 1;
			var index = property.UniqueId & mask;

			while (entries[index] is not null)
			{
				index = (index + 1) & mask;
			}

			_count++;

			return entries[index] = new DependencyPropertyDetails(property, property == _dataContextProperty);
		}

		private void Grow()
		{
			var oldEntries = _entries;
			var newEntries = new DependencyPropertyDetails?[Math.Max(MinimumCapacity, oldEntries.Length * 2)];
			var mask = newEntries.Length - 1;

			foreach (var entry in oldEntries)
			{
				if (entry is not null)
				{
					var index = entry.Property.UniqueId & mask;

					while (newEntries[index] is not null)
					{
						index = (index + 1) & mask;
					}

					newEntries[index] = entry;
				}
			}

			_entries = newEntries;
		}

		private static void Insert(DependencyPropertyDetails?[] entries, DependencyPropertyDetails details)
		{
			var mask = entries.Length - 1;
			var index = details.Property.UniqueId & mask;

			while (entries[index] is not null)
			{
				index = (index + 1) & mask;
			}

			entries[index] = details;
		}

		/// <summary>
		/// Returns every entry: sparse (nulls included), in no particular order, and a snapshot that may or may not
		/// reflect properties added while it is being walked.
		/// </summary>
		internal AllDetails GetAllDetails() => new(_slot0, _slot1, _entries);

		internal readonly struct AllDetails
		{
			private readonly DependencyPropertyDetails? _slot0;
			private readonly DependencyPropertyDetails? _slot1;
			private readonly DependencyPropertyDetails?[] _entries;

			public AllDetails(DependencyPropertyDetails? slot0, DependencyPropertyDetails? slot1, DependencyPropertyDetails?[] entries)
			{
				_slot0 = slot0;
				_slot1 = slot1;
				_entries = entries;
			}

			public Enumerator GetEnumerator() => new(this);

			internal struct Enumerator
			{
				private readonly AllDetails _all;
				private int _index;

				public Enumerator(AllDetails all)
				{
					_all = all;
					_index = -1;
				}

				public DependencyPropertyDetails? Current { get; private set; }

				public bool MoveNext()
				{
					var entries = _all._entries ?? Array.Empty<DependencyPropertyDetails?>();
					_index++;
					if (_index == 0)
					{
						Current = _all._slot0;
						return true;
					}

					if (_index == 1)
					{
						Current = _all._slot1;
						return true;
					}

					if (_index - 2 < entries.Length)
					{
						Current = entries[_index - 2];
						return true;
					}

					return false;
				}
			}
		}

	}
}
