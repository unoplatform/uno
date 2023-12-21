#nullable enable

using System;
using System.Runtime.CompilerServices;
using Uno.Buffers;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// A <see cref="DependencyPropertyDetails"/> collection
	/// </summary>
	partial class DependencyPropertyDetailsCollection : IDisposable
	{
		private readonly Type _ownerType;
		private readonly ManagedWeakReference _ownerReference;
		private object? _hardOwnerReference;
		private readonly DependencyProperty _dataContextProperty;
		private readonly DependencyProperty _templatedParentProperty;

		private DependencyPropertyDetails? _dataContextPropertyDetails;
		private DependencyPropertyDetails? _templatedParentPropertyDetails;

		private readonly static ArrayPool<short> _offsetsPool = ArrayPool<short>.Shared;
		private readonly static LinearArrayPool<DependencyPropertyDetails?> _pool = LinearArrayPool<DependencyPropertyDetails?>.CreateAutomaticallyManaged(BucketSize, 16);

		private static readonly DependencyPropertyDetails?[] _empty = Array.Empty<DependencyPropertyDetails?>();

		private DependencyPropertyDetails?[] _entries;
		private short[]? _entryOffsets;

		private const int BucketSize = 16;

		private object? Owner => _hardOwnerReference ?? _ownerReference.Target;

		/// <summary>
		/// Creates an instance using the specified DependencyObject <see cref="Type"/>
		/// </summary>
		/// <param name="ownerType">The owner type</param>
		public DependencyPropertyDetailsCollection(Type ownerType, ManagedWeakReference ownerReference, DependencyProperty dataContextProperty, DependencyProperty templatedParentProperty)
		{
			_ownerType = ownerType;
			_ownerReference = ownerReference;

			_dataContextProperty = dataContextProperty;
			_templatedParentProperty = templatedParentProperty;

			_entries = _empty;
		}

		public void Dispose()
		{
			var entries = _entries;

			var entriesLength = entries.Length;

			for (var i = 0; i < entriesLength; i++)
			{
				entries[i]?.Dispose();
			}

			ReturnEntriesAndOffsetsToPools();
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
			if (forceCreate)
			{
				// Since BucketSize is a power of 2 we can shift and mask to divide and modulo respectively
				// Both operations(div/mod) are still expensive on modern hardware (~20+ cycles)
				// This is not a concern for RyuJIT or LLVM backends as they will emit optimized code for it.
				// The main concern is the Mono interpreter which may or may not do so.
				// See: libdivide and fastmod projects
				var bucketIndex = property.UniqueId >> 4;
				var bucketRemainder = property.UniqueId & 15;

				var entryOffsets = _entryOffsets;

				// Offsets have not been initialized or need to be resized
				if (entryOffsets == null || bucketIndex >= entryOffsets.Length)
				{
					// Rent the next multiple of BucketSize available : 0 -> 16, 16 -> 32, 32 -> 64 ...
					var newOffsets = _offsetsPool.Rent((bucketIndex * BucketSize) + 1);

					// Since newOffsets is an Int16 array we can memset it with 0xFFs, 0xFFFF is -1, regardless of endianness
					// This avoids the slow path in Span<T>.Fill()
					Unsafe.InitBlockUnaligned(ref Unsafe.As<short, byte>(ref newOffsets[0]), 0xFF, (uint)newOffsets.Length * 2);

					if (entryOffsets != null)
					{
						entryOffsets.AsSpan().CopyTo(newOffsets);

						_offsetsPool.Return(entryOffsets);
					}

					_entryOffsets = entryOffsets = newOffsets;
				}

				var entries = _entries;

				var offset = entryOffsets[bucketIndex];

				// Offset -1 represents an unallocated bucket, -1 was chosen because 0 is a valid offset
				// We need to resize the entries array to fit a new bucket
				if (offset == -1)
				{
					entryOffsets[bucketIndex] = offset = (short)entries.Length;

					var newEntries = _pool.Rent(entries.Length + BucketSize);

					if (entries != _empty)
					{
						entries.AsSpan().CopyTo(newEntries);

						_pool.Return(entries, clearArray: true);
					}

					_entries = entries = newEntries;
				}

				ref var propertyEntry = ref entries[offset + bucketRemainder];

				if (propertyEntry == null)
				{
					propertyEntry = new DependencyPropertyDetails(property, _ownerType, property == _dataContextProperty || property == _templatedParentProperty);

					if (TryResolveDefaultValueFromProviders(property, out var value))
					{
						propertyEntry.SetDefaultValue(value);
					}
				}

				return propertyEntry;
			}
			else
			{
				if (_entries != _empty)
				{
					// See above
					var bucketIndex = property.UniqueId >> 4;

					if (bucketIndex < _entryOffsets!.Length)
					{
						var offset = _entryOffsets[bucketIndex];

						return offset != -1 ? _entries[offset + (property.UniqueId & 15)] : null;
					}
				}

				return null;
			}
		}

		private bool TryResolveDefaultValueFromProviders(DependencyProperty property, out object? value)
		{
			// Replicate the WinUI behavior of DependencyObject::GetDefaultValue2 specifically for UIElement.
			if (Owner is UIElement uiElement)
			{
				return uiElement.GetDefaultValue2(property, out value);
			}

			value = null;
			return false;
		}

		private void ReturnEntriesAndOffsetsToPools()
		{
			if (_entries != _empty)
			{
				_pool.Return(_entries, clearArray: true);
			}

			if (_entryOffsets != null)
			{
				_offsetsPool.Return(_entryOffsets);
			}
		}

		internal DependencyPropertyDetails?[] GetAllDetails() => _entries;

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
