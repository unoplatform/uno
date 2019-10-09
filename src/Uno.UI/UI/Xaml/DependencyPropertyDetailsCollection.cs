using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// A <see cref="DependencyPropertyDetails"/> collection
	/// </summary>
    partial class DependencyPropertyDetailsCollection
    {
		private readonly Type _ownerType;
		private readonly ManagedWeakReference _ownerReference;

		public DependencyPropertyDetails DataContextPropertyDetails { get; }
		public DependencyPropertyDetails TemplatedParentPropertyDetails { get; }

		private PropertyEntry[] _entries;

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

			var entries = new PropertyEntry[propertiesForType.Length];

			for (int i = 0; i < propertiesForType.Length; i++)
			{
				entries[i].Id = propertiesForType[i].UniqueId;
			}

			// Entries are pre-sorted by the DependencyProperty.GetPropertiesForType method
			AssignEntries(entries, sort: false);

			// Prefetch known properties for faster access
			DataContextPropertyDetails = GetPropertyDetails(dataContextProperty);
			TemplatedParentPropertyDetails = GetPropertyDetails(templatedParentProperty);
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
					var newEntries = new PropertyEntry[_entries.Length + 1];

					if (_entries.Length != 0)
					{
						Array.Copy(_entries, 0, newEntries, 0, _entries.Length);
					}

					ref var newEntry = ref newEntries[_entries.Length];

					var details = new DependencyPropertyDetails(property, _ownerType);

					newEntry.Id = property.UniqueId;
					newEntry.Details = details;

					AssignEntries(newEntries, sort: true);

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

		private void AssignEntries(PropertyEntry[] newEntries, bool sort)
		{
			_entries = newEntries;

			if (sort)
			{
				Array.Sort(newEntries, (l, r) => l.Id - r.Id);
			}
		}

		private ref PropertyEntry GetEntry(int propertyId)
		{
			// 6 is based based on some perf comparisons on Android.
			// This may need to be adjusted based on some hardware specific properties.
			const int LinearSearchThreshold = 6;

			int min = 0;
			int max = _entries.Length - 1;

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
