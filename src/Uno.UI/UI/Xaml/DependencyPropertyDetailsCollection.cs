using System;
using System.Collections.Generic;
using System.Text;
using Uno.Buffers;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// A <see cref="DependencyPropertyDetails"/> collection
	/// </summary>
    partial class DependencyPropertyDetailsCollection : IDisposable
    {
		private readonly Type _ownerType;
		private readonly ManagedWeakReference _ownerReference;

		public DependencyPropertyDetails DataContextPropertyDetails { get; }
		public DependencyPropertyDetails TemplatedParentPropertyDetails { get; }

		private readonly static ArrayPool<PropertyEntry> _pool = ArrayPool<PropertyEntry>.Create(100, 100);

		private PropertyEntry[] _entries;
		private int _entriesLength;

		private PropertyEntry None = new PropertyEntry(-1, null);

		/// <summary>
		/// Creates an instance using the specified DependencyObject <see cref="Type"/>
		/// </summary>
		/// <param name="ownerType">The owner type</param>
		public DependencyPropertyDetailsCollection(Type ownerType, ManagedWeakReference ownerReference, DependencyProperty dataContextProperty, DependencyProperty templatedParentProperty)
		{
			_ownerType = ownerType;
			_ownerReference = ownerReference;

			var propertiesForType = DependencyProperty.GetPropertiesForType(ownerType);

			var entries = _pool.Rent(propertiesForType.Length);

			for (int i = 0; i < propertiesForType.Length; i++)
			{
				ref var entry = ref entries[i];
				entry.Id = propertiesForType[i].UniqueId;
				entry.Details = null;
			}

			// Entries are pre-sorted by the DependencyProperty.GetPropertiesForType method
			AssignEntries(entries, propertiesForType.Length, sort: false);

			// Prefetch known properties for faster access
			DataContextPropertyDetails = GetPropertyDetails(dataContextProperty);
			TemplatedParentPropertyDetails = GetPropertyDetails(templatedParentProperty);
		}

		public void Dispose()
		{
			for (var i = 0; i < _entriesLength; i++)
			{
				_entries[i].Details?.Dispose();
			}

			ReturnEntriesToPool();
		}

		/// <summary>
		/// Gets the <see cref="DependencyPropertyDetails"/> for a specific <see cref="DependencyProperty"/>
		/// </summary>
		/// <param name="property">A dependency property</param>
		/// <returns>The details of the property</returns>
		public DependencyPropertyDetails GetPropertyDetails(DependencyProperty property)
			=> TryGetPropertyDetails(property, forceCreate: true);

		/// <summary>
		/// Finds the <see cref="DependencyPropertyDetails"/> for a specific <see cref="DependencyProperty"/> if it exists.
		/// </summary>
		/// <param name="property">A dependency property</param>
		/// <returns>The details of the property if it exists, otherwise null.</returns>
		public DependencyPropertyDetails FindPropertyDetails(DependencyProperty property)
			=> TryGetPropertyDetails(property, forceCreate: false);

		private DependencyPropertyDetails TryGetPropertyDetails(DependencyProperty property, bool forceCreate)
		{
			ref var propertyEntry = ref GetEntry(property.UniqueId);

			if (propertyEntry.Id == -1)
			{
				if (forceCreate)
				{
					// The property was not known at startup time, add it.
					var newEntriesSize = _entriesLength + 1;
					var newEntries = _pool.Rent(newEntriesSize);

					if (_entriesLength != 0)
					{
						Array.Copy(_entries, 0, newEntries, 0, _entriesLength);
					}

					ref var newEntry = ref newEntries[_entriesLength];

					var details = new DependencyPropertyDetails(property, _ownerType);

					newEntry.Id = property.UniqueId;
					newEntry.Details = details;

					AssignEntries(newEntries, newEntriesSize, sort: true);

					return details;
				}
				else
				{
					return null;
				}
			}
			else
			{
				if (propertyEntry.Details == null)
				{
					propertyEntry.Details = new DependencyPropertyDetails(property, _ownerType);
				}

				return propertyEntry.Details;
			}
		}

		private void AssignEntries(PropertyEntry[] newEntries, int newSize, bool sort)
		{
			ReturnEntriesToPool();

			_entries = newEntries;
			_entriesLength = newSize;

			if (sort)
			{
				Array.Sort(newEntries, 0, newSize, PropertyEntryComparer.Instance);
			}
		}

		private void ReturnEntriesToPool()
		{
			if (_entries != null)
			{
				_pool.Return(_entries);
			}
		}

		private ref PropertyEntry GetEntry(int propertyId)
		{
			// 6 is based based on some perf comparisons on Android.
			// This may need to be adjusted based on some hardware specific properties.
			const int LinearSearchThreshold = 6;

			int min = 0;
			int max = _entriesLength - 1;

			if (max >= 0)
			{
				while (max - min > LinearSearchThreshold)
				{
					int mid = (min + max) / 2;
					ref var midValue = ref _entries[mid];

					if (propertyId == midValue.Id)
					{
						return ref midValue;
					}
					else if (propertyId < midValue.Id)
					{
						max = mid - 1;
					}
					else
					{
						min = mid + 1;
					}
				}

				// If the remaining items reaches LinearSearchThreshold, switch to linear search
				while (min <= max)
				{
					ref var midValue = ref _entries[min];

					if (midValue.Id == propertyId)
					{
						return ref midValue;
					}

					if (midValue.Id > propertyId)
					{
						break;
					}

					min++;
				}
			}

			// Should be readonly, see C# 7.2 and up.
			return ref None;
		}

		class PropertyEntryComparer : IComparer<PropertyEntry>
		{
			public static readonly PropertyEntryComparer Instance = new PropertyEntryComparer();

			public int Compare(PropertyEntry x, PropertyEntry y) => x.Id - y.Id;
		}

		private struct PropertyEntry
		{
			public PropertyEntry(int id, DependencyPropertyDetails details)
			{
				Id = id;
				Details = details;
			}

			public int Id;
			public DependencyPropertyDetails Details;
		}
	}
}
