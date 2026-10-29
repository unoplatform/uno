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
			for (int i = 0; i < _entries.Length; i++)
			{
				if (_entries[i] is { Property: { } oldDP } oldDetails)
				{
					var newDP = DependencyProperty.GetProperty(oldDP.OwnerType, oldDP.Name);
					if (newDP is null)
					{
						continue;
					}

					if (other.GetPropertyDetails(newDP) is { } newDetails)
					{
						oldDetails.CloneToForHotReload(newDetails);

						// This may not work well for x:Bind, we will investigate proper support for x:Bind.
						// Though, anything will be done now for x:Bind will need to be re-worked if we refactored
						// x:Bind to be fully compiled, as in WinUI.
						if (oldDetails.GetBinding() is { ParentBinding: { } binding })
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

							otherStore.SetBindingInternal(newDP, newBinding);
						}
					}
				}
			}
		}

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

		/// <summary>
		/// Returns the backing table: sparse, in hash order (not registration order), and a snapshot that may
		/// or may not reflect properties added while it is being walked.
		/// </summary>
		internal DependencyPropertyDetails?[] GetAllDetails() => _entries;

	}
}
