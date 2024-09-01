#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Data;
using Uno.Buffers;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// A <see cref="DependencyPropertyDetails"/> collection
	/// </summary>
	partial class DependencyPropertyDetailsCollection : IDisposable
	{
		private readonly ManagedWeakReference _ownerReference;
		private object? _hardOwnerReference;
		private readonly DependencyProperty _dataContextProperty;
		private readonly DependencyProperty _templatedParentProperty;

		private DependencyPropertyDetails? _dataContextPropertyDetails;
		private DependencyPropertyDetails? _templatedParentPropertyDetails;

		private DependencyPropertyDetails[] _entries = Array.Empty<DependencyPropertyDetails>();
		private int _entriesCount;

		private const int InitialCapacity = 8;

		private object? Owner => _hardOwnerReference ?? _ownerReference.Target;

		/// <summary>
		/// Creates an instance using the specified DependencyObject <see cref="Type"/>
		/// </summary>
		public DependencyPropertyDetailsCollection(ManagedWeakReference ownerReference, DependencyProperty dataContextProperty, DependencyProperty templatedParentProperty)
		{
			_ownerReference = ownerReference;

			_dataContextProperty = dataContextProperty;
			_templatedParentProperty = templatedParentProperty;
		}

		internal void CloneToForHotReload(DependencyPropertyDetailsCollection other, DependencyObjectStore store, DependencyObjectStore otherStore)
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
							if (newSource is IDependencyObjectStoreProvider { Store: { } oldStore } && oldStore == store)
							{
								newSource = otherStore.ActualInstance;
							}

							newBinding.Source = newSource;
							newBinding.Mode = binding.Mode;
							newBinding.TargetNullValue = binding.TargetNullValue;
							newBinding.ElementName = binding.ElementName;
							newBinding.FallbackValue = binding.FallbackValue;
							if (binding.RelativeSource is { } relativeSource)
							{
								newBinding.RelativeSource = new RelativeSource(relativeSource.Mode);
							}

							otherStore.SetBinding(newDP, newBinding);
						}
					}
				}
			}
		}

		public void Dispose()
		{
			var entries = _entries;

			for (var i = 0; i < _entriesCount; i++)
			{
				entries[i].Dispose();
			}

			_entries = null!;
			_entriesCount = 0;
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
		public DependencyPropertyDetails? FindPropertyDetails(DependencyProperty property)
			=> TryGetPropertyDetails(property, forceCreate: false);

		private DependencyPropertyDetails? TryGetPropertyDetails(DependencyProperty property, bool forceCreate)
		{
			if (_entries is null)
			{
				return null;
			}

			if (forceCreate)
			{
				if (_entries.Length == 0)
				{
					_entries = new DependencyPropertyDetails[InitialCapacity];
				}

				// The behavior of BinarySearchForEqualsOrGreater is as follows. Assume unique ids are:
				// [1, 3, 5]
				// Searching for 0 produces index 0 (value 0 is not found, and next greater is 1 which is at index 0)
				// Searching for 1 produces index 0 (value 1 is found and its index is returned)
				// Searching for 2 produces index 1 (value 2 isn't found, and next greater is 3 which is at index 1)
				// Searching for 3 produces index 1 (value 3 is found and its index is returned)
				// Searching for 4 produces index 2 (value 4 isn't found, and next greater is 5 which is at index 2)
				// Searching for 5 produces index 2 (value 5 is found and its index is returned)
				// Searching for 6 produces index 3 (value 6 isn't found, and next greater doesn't exist, so it returns an "imaginary" position where the next greater would have existed, which is the count in this case)
				var index = BinarySearchForEqualsOrGreater(property);
				if (index < _entriesCount && _entries[index].Property.UniqueId == property.UniqueId)
				{
					// Value already exists.
					return _entries[index];
				}

				var newEntry = new DependencyPropertyDetails(property, property == _dataContextProperty || property == _templatedParentProperty);

				// Value is not found. We need to force create.
				// If there is a space in the array, we can only shift elements.
				// If there is no space, we need to resize and copy.
				if (_entriesCount < _entries.Length)
				{
					// We have space in _entries.
					// We want to insert the new entry at "index".
					// So, we shift elements starting at index.
					var source = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), index), _entriesCount - index);
					var dest = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), index + 1), _entries.Length - index - 1);
					source.CopyTo(dest);
					_entries[index] = newEntry;
				}
				else
				{
					var newEntries = GC.AllocateUninitializedArray<DependencyPropertyDetails>(_entries.Length * 2);

					var sourceLeft = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_entries), index);
					var destLeft = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(newEntries), newEntries.Length);
					sourceLeft.CopyTo(destLeft);

					newEntries[index] = newEntry;

					var sourceRight = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entries), index), _entriesCount - index);
					var destRight = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(newEntries), index + 1), newEntries.Length - index - 1);
					sourceRight.CopyTo(destRight);

					_entries = newEntries;
				}

				_entriesCount++;

				return newEntry;
			}
			else
			{
				var index = BinarySearchForEqualsOrGreater(property);
				if (index < _entriesCount && _entries[index].Property.UniqueId == property.UniqueId)
				{
					return _entries[index];
				}

				return null;
			}
		}

		private int BinarySearchForEqualsOrGreater(DependencyProperty property)
		{
			var low = 0;
			var high = _entriesCount - 1;
			var target = property.UniqueId;
			while (low <= high)
			{
				var mid = low + ((high - low) >> 1);
				var current = _entries[mid].Property.UniqueId;
				if (target == current)
				{
					// We found the property!
					return mid;
				}
				else if (target > current)
				{
					// The value we are looking for is greater than current.
					// We should search the right subarray.
					low = mid + 1;
				}
				else
				{
					// The value we are looking for is smaller than current.
					// We should search the left subarray.
					high = mid - 1;
				}
			}

			return low;
		}

		internal ReadOnlySpan<DependencyPropertyDetails> GetAllDetails()
			=> (_entries ?? Array.Empty<DependencyPropertyDetails>()).AsSpan().Slice(0, _entriesCount);

		internal void TryEnableHardReferences()
		{
			_hardOwnerReference = _ownerReference.Target;
		}

		internal void DisableHardReferences()
		{
			_hardOwnerReference = null;
		}
	}
}
