#nullable enable

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
	/// <remarks>
	/// This implementation uses an O(1) lookup for the dependency properties of a DependencyObject. This assumes that
	/// <see cref="DependencyProperty.GetPropertiesForType"/> returns an ordered list, and creates an array based on
	/// the min and max UniqueIDs found in the object's properties.
	///
	/// This approach can cost more in storage for some types, if the array is mostly empty.
	/// </remarks>
	partial class DependencyPropertyDetailsCollection : IDisposable
	{
		private static readonly DependencyPropertyDetails[] Empty = new DependencyPropertyDetails[0];

		private readonly Type _ownerType;
		private readonly ManagedWeakReference _ownerReference;
		private readonly DependencyProperty _dataContextProperty;
		private readonly DependencyProperty _templatedParentProperty;

		private DependencyPropertyDetails? _dataContextPropertyDetails;
		private DependencyPropertyDetails? _templatedParentPropertyDetails;

		private readonly static ArrayPool<DependencyPropertyDetails> _pool = ArrayPool<DependencyPropertyDetails>.Create(500, 100);

		private DependencyPropertyDetails[] _entries = Empty;
		private int _entriesLength;
		private int _minId;
		private int _maxId;

		/// <summary>
		/// Creates an instance using the specified DependencyObject <see cref="Type"/>
		/// </summary>
		/// <param name="ownerType">The owner type</param>
		public DependencyPropertyDetailsCollection(Type ownerType, ManagedWeakReference ownerReference, DependencyProperty dataContextProperty, DependencyProperty templatedParentProperty)
		{
			_ownerType = ownerType;
			_ownerReference = ownerReference;

			var propertiesForType = DependencyProperty.GetPropertiesForType(ownerType);

			if (propertiesForType.Length != 0)
			{
				_minId = propertiesForType[0].UniqueId;
				_maxId = propertiesForType[propertiesForType.Length - 1].UniqueId;

				var entriesLength = _maxId - _minId + 1;
				var entries = _pool.Rent(entriesLength);

				// Entries are pre-sorted by the DependencyProperty.GetPropertiesForType method
				AssignEntries(entries, entriesLength);
			}

			_dataContextProperty = dataContextProperty;
			_templatedParentProperty = templatedParentProperty;
		}

		public void Dispose()
		{
			for (var i = 0; i < _entriesLength; i++)
			{
				_entries![i]?.Dispose();
			}

			ReturnEntriesToPool();
		}

		public DependencyPropertyDetails DataContextPropertyDetails
			=> _dataContextPropertyDetails ??= GetPropertyDetails(_dataContextProperty);

		public DependencyPropertyDetails TemplatedParentPropertyDetails
			=> _templatedParentPropertyDetails ??= GetPropertyDetails(_templatedParentProperty);

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
		public DependencyPropertyDetails FindPropertyDetails(DependencyProperty property)
			=> TryGetPropertyDetails(property, forceCreate: false)!;

		private DependencyPropertyDetails? TryGetPropertyDetails(DependencyProperty property, bool forceCreate)
		{
			var propertyId = property.UniqueId;

			var entryIndex = propertyId - _minId;

			// https://stackoverflow.com/a/17095534/26346
			var isInRange = (uint)entryIndex <= (_maxId - _minId);

			if (isInRange)
			{
				ref var propertyEntry = ref _entries![entryIndex];

				if (forceCreate && propertyEntry == null)
				{
					propertyEntry = new DependencyPropertyDetails(property, _ownerType);
				}

				return propertyEntry;
			}
			else
			{
				if (forceCreate)
				{
					int newEntriesSize;
					DependencyPropertyDetails[] newEntries;

					if (entryIndex < 0)
					{
						newEntriesSize = _maxId - propertyId + 1;
						newEntries = _pool.Rent(newEntriesSize);
						Array.Copy(_entries, 0, newEntries, _minId - propertyId, _entriesLength);

						_minId = propertyId;

						AssignEntries(newEntries, newEntriesSize);
					}
					else
					{
						newEntriesSize = propertyId - _minId + 1;

						newEntries = _pool.Rent(newEntriesSize);
						Array.Copy(_entries, 0, newEntries, 0, _entriesLength);

						AssignEntries(newEntries, newEntriesSize);
					}

					ref var propertyEntry = ref _entries![property.UniqueId - _minId];
					return propertyEntry = new DependencyPropertyDetails(property, _ownerType);
				}
				else
				{
					return null;
				}
			}
		}

		private void AssignEntries(DependencyPropertyDetails[] newEntries, int newSize)
		{
			ReturnEntriesToPool();

			_entries = newEntries;
			_entriesLength = newEntries.Length;

			// Array size returned by Rend may be larger than the requested size
			// Adjust the max to that new value.
			_maxId = _entriesLength + _minId - 1;
		}

		private void ReturnEntriesToPool()
		{
			if (_entries != null)
			{
				_pool.Return(_entries, clearArray: true);
			}
		}
	}
}
