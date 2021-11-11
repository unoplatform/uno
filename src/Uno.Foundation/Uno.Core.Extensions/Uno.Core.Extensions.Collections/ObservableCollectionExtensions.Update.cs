// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.Extensions;

namespace Uno.Extensions
{
	internal static class ObservableCollectionExtensions
	{
		/// <summary>
		/// Updates an ObservableCollection using the provided enumerable, resulting in equal sequences.
		/// </summary>
		/// <param name="collection">The collection to update</param>
		/// <param name="updated">The enumerable to update from</param>
		/// <param name="tryDispose">Tells the method to try disposing removed items and new items that were not added.
		/// <param name="comparer"></param>
		/// <em>ONLY PASS true WHEN USING DIFFERENT INSTANCES THAT USE EQUALS TO MATCH EXISTING INSTANCES. Matching items are not compared by reference.</em></param>
		/// <remarks>If items come from an AVVM, make sure it's not configured to automatically dispose all previous items. Otherwise, this extension
		/// will keep items in the collection that are getting disposed by the AVVM.</remarks>
		public static void Update<T>(this IList<T> collection, IEnumerable<T> updated, bool tryDispose = false, IEqualityComparer<T> comparer = null)
		{
			collection.InternalUpdate(updated, tryDispose, comparer: comparer);
		}

		/// <summary>
		/// Updates an ObservableCollection using the provided enumerable, resulting in equal sequences.
		/// </summary>
		/// <param name="collection">The collection to update</param>
		/// <param name="updated">The enumerable to update from</param>
		/// <param name="tryDispose">Tells the method to try disposing removed items and new items that were not added.
		/// <param name="comparer"></param>
		/// <em>ONLY PASS true WHEN USING DIFFERENT INSTANCES THAT USE EQUALS TO MATCH EXISTING INSTANCES. Matching items are not compared by reference.</em></param>
		/// <remarks>If items come from an AVVM, make sure it's not configured to automatically dispose all previous items. Otherwise, this extension
		/// will keep items in the collection that are getting disposed by the AVVM.</remarks>
		/// <returns>A instance of <see cref="ObservableCollectionUpdateResults{T}"/> which details what the update has done.</returns>
		public static ObservableCollectionUpdateResults<T> UpdateWithResults<T>(this IList<T> collection, IEnumerable<T> updated, bool tryDispose = false, IEqualityComparer<T> comparer = null)
		{
			var results = collection.InternalUpdate(updated, tryDispose, comparer: comparer);

			return new ObservableCollectionUpdateResults<T>(results.added, results.moved, results.removed);
		}

		/// <summary>
		/// Updates an ObservableCollection using the provided enumerable, resulting in equal sequences. For any item that was
		/// kept for an equal new instance, UpdateAsync is called if it implements IUpdatable.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="collection"></param>
		/// <param name="ct"></param>
		/// <param name="updated"></param>
		/// <param name="tryDispose"></param>
		/// <param name="comparer"></param>
		/// <returns></returns>
		public static Task UpdateAsync<T>(this IList<T> collection, CancellationToken ct, IEnumerable<T> updated, bool tryDispose = false, IEqualityComparer<T> comparer = null)
		{
			var updatables = collection
				.InternalUpdate(updated, tryDispose, true, comparer)
				.kept
				.Select(info => new { Updatable = info.OldItem as IUpdatable<T>, Update = info.NewItem })
				.ToArray();

			// Avoid "if" inside.
			var tasks = tryDispose
				? updatables
					.Select(async updateInfo =>
						{
							if (updateInfo.Updatable != null)
							{
								await updateInfo.Updatable.UpdateAsync(ct, updateInfo.Update);
							}

							TryDispose(updateInfo.Update);
						})
					.ToArray()
				: updatables
					.Select(async updateInfo =>
						{
							if (updateInfo.Updatable != null)
							{
								await updateInfo.Updatable.UpdateAsync(ct, updateInfo.Update);
							}
						})
					.ToArray();

			return Task.WhenAll(tasks);
		}


		/// <summary>
		/// Private version for having a single implementation of adds, removes and updates, but be able to plug async IUpdatable.UpdateAsync call.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="collection"></param>
		/// <param name="updated"></param>
		/// <param name="tryDispose"></param>
		/// <param name="needKept">If true, returns kept item pairs. New items that were kept are <em>NOT</em> disposed.
		/// We assume the caller will perform the async update, then dispose NewItem.</param>
		/// <param name="comparer"></param>
		/// <returns></returns>
		private static (IEnumerable<T> added, IEnumerable<T> moved, IEnumerable<T> removed, KeptInfo<T>[] kept) InternalUpdate<T>(
			this IList<T> collection,
			IEnumerable<T> updated,
			bool tryDispose = false,
			bool needKept = false,
			IEqualityComparer<T> comparer = null
		)
		{
			try
			{
				comparer = comparer ?? EqualityComparer<T>.Default;

				var array = updated as T[] ?? updated.ToArray();

				{
					if (collection.SequenceKeyEqual(array))
					{
						// If the new items are key-equal to the previous items, we just need to do a replace operation for those elements 
						// which have changed.
						List<T> removedInner = null;
						List<T> addedInner = null;
						for (int i = 0; i < array.Length; i++)
						{
							if (!EqualsWithComparer(collection[i], array[i]))
							{
								removedInner = removedInner ?? new List<T>();
								addedInner = addedInner ?? new List<T>();

								removedInner.Add(collection[i]);
								addedInner.Add(array[i]);

								var old = collection[i];
								// Do a replace for modified item
								collection[i] = array[i];

								if (tryDispose)
								{
									old.TryDispose();
								}
							}
							else if (tryDispose && !needKept && !ReferenceEquals(collection[i], array[i]))
							{
								array[i].TryDispose();
							}
						}

						return (
							addedInner ?? Enumerable.Empty<T>(), 
							Enumerable.Empty<T>(), 
							removedInner ?? Enumerable.Empty<T>(), 
							GetKeptItems(collection, array, comparer)
						);
					}
				}

				// Materialize removed and added items before modifying collection.
				var removed = collection.Except(array, comparer).ToArray();
				var added = array.Except(collection, comparer).ToArray();

				// Remove items. Avoid "if" in loop.
				if (tryDispose)
				{
					removed.ForEach((T r) =>
					{
						collection.Remove(r);
						r.TryDispose();
					});
				}
				else
				{
					removed.ForEach((T r) => collection.Remove(r));
				}

				InsertNewItems(collection, array, added, comparer);

				var kept = GetKeptItems(collection, array, comparer);

				ManipulateItems(collection, kept);

				// Dispose new instances that already had an "equals" item in the collection.
				if (tryDispose && !needKept)
				{
					kept
						// Safeguard in case same instances can get returned. Would not use "tryDispose = true" on value types anyway.
						.Where(info => !object.ReferenceEquals(info.OldItem, info.NewItem))
						.ForEach(k => k.NewItem.TryDispose());
				}

				var movedItems = kept.Where(k => k.OldIndex != k.NewIndex).Select(i => i.NewItem);

				return needKept
					? (added: added, moved: movedItems, removed: removed, kept: kept)
					: (added: added, moved: movedItems, removed: removed, kept: (KeptInfo<T>[])null);
			}
			catch (ArgumentOutOfRangeException e)
			{
				if (updated.Distinct().Count() != updated.Count())
				{
					throw new InvalidOperationException(
						"There are duplicate items in the updated collection, which is not supported in ObservableCollection.Update helper.",
						e
					);
				}
				else
				{
					throw;
				}
			}

			bool EqualsWithComparer(T objA, T objB) => comparer != null ? comparer.Equals(objA, objB) : Equals(objA, objB);
		}

		private static void InsertNewItems<T>(IList<T> collection, T[] array, T[] added, IEqualityComparer<T> comparer)
		{
			// Insert new items.
			var indexes = added.Select(a => new { Index = array.IndexOf(a, comparer), Value = a });
			indexes.ForEach(i => collection.Insert(i.Index, i.Value));
		}

		private static void ManipulateItems<T>(IList<T> collection, KeptInfo<T>[] kept)
		{
			Action<int, int, T> updater;

			var observable = collection as ObservableCollection<T>;

			if (observable != null)
			{
				updater = (oldIndex, newIndex, oldItem) =>
					// All items after the new index get pushed. It's an insert at the new index, 
					// and a remove at the old.
					observable.Move(oldIndex, newIndex);
			}
			else
			{
				updater = (oldIndex, newIndex, oldItem) =>
				{
					// We must do the same as Move below.
					collection.RemoveAt(oldIndex);
					collection.Insert(newIndex, oldItem);
				};
			}


			for (int i = 0; i < kept.Length; i++)
			{
				var info = kept[i];

				if (info.OldIndex != info.NewIndex)
				{
					updater(info.OldIndex, info.NewIndex, info.OldItem);

					// Any item not already moved that had an old index after (or equal) to the new index
					// but before the old index must be adjusted.
					for (int j = i + 1; j < kept.Length; j++)
					{
						var other = kept[j];

						if ((other.OldIndex >= info.NewIndex) && (other.OldIndex < info.OldIndex))
						{
							++other.OldIndex;
						}
					}
				}
			}
		}

		private static KeptInfo<T>[] GetKeptItems<T>(IList<T> collection, T[] array, IEqualityComparer<T> comparer)
		{
			// Move existing items if required. Newly added items must have their entry, which can get
			// affected by other moves, and will adjust just like others.
			return array
				.Select((item, index) =>
				{
					var oldIndex = collection.IndexOf(item, comparer);

					return new KeptInfo<T>()
					{
						OldItem = (oldIndex == -1) ? item : collection[oldIndex],
						OldIndex = (oldIndex == -1) ? index : oldIndex,
						NewItem = item,
						NewIndex = index
					};
				})
				.OrderBy(info => info.NewIndex)
				.ToArray();
		}

		private class KeptInfo<T>
		{
			public T OldItem { get; set; }
			public T NewItem { get; set; }
			public int OldIndex { get; set; }
			public int NewIndex { get; set; }
		}

		private static bool TryDispose(object maybeDisposableObject)
		{
			var disposable = maybeDisposableObject as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
				return true;
			}
			return false;
		}
	}
}
