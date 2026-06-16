using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Temporary record of an <see cref="INotifyCollectionChanged"/> operation.
	/// </summary>
	internal class CollectionChangedOperation
	{
		public Uno.UI.IndexPath StartingIndex { get; }
		public Uno.UI.IndexPath? NewStartingIndex { get; }
		public int Range { get; }
		public NotifyCollectionChangedAction Action { get; }
		public Element ElementType { get; }

		public Uno.UI.IndexPath EndIndex => ElementType == Element.Item ?
			Uno.UI.IndexPath.FromRowSection(StartingIndex.Row + Range - 1, StartingIndex.Section) :
			//Group change
			Uno.UI.IndexPath.FromRowSection(StartingIndex.Row, StartingIndex.Section + Range - 1);

		public CollectionChangedOperation(Uno.UI.IndexPath startingIndex, int range, NotifyCollectionChangedAction action, Element elementType)
		{
			StartingIndex = startingIndex;
			Range = range;
			Action = action;
			ElementType = elementType;
		}

		public CollectionChangedOperation(Uno.UI.IndexPath startingIndex, Uno.UI.IndexPath newStartingIndex, int range, NotifyCollectionChangedAction action, Element elementType)
		{
			StartingIndex = startingIndex;
			NewStartingIndex = newStartingIndex;
			Range = range;
			Action = action;
			ElementType = elementType;
		}

		/// <summary>
		/// Apply the offset to a collection index resulting from this collection operation.
		/// </summary>
		/// <param name="indexPath">The index in the collection prior to the operation</param>
		/// <returns>The offset position, or null if this position is no longer valid (ie because it has been removed by the operation).</returns>
		public Uno.UI.IndexPath? Offset(Uno.UI.IndexPath indexPath)
		{
			var section = indexPath.Section;
			var row = indexPath.Row;

			switch (this)
			{
				case var itemAdd when itemAdd.ElementType == CollectionChangedOperation.Element.Item &&
							itemAdd.Action == NotifyCollectionChangedAction.Add &&
							itemAdd.StartingIndex.Section == section &&
							itemAdd.EndIndex.Row <= row:
					row += itemAdd.Range;
					break;

				case var itemRemove when itemRemove.ElementType == CollectionChangedOperation.Element.Item &&
							itemRemove.Action == NotifyCollectionChangedAction.Remove &&
							itemRemove.StartingIndex.Section == section &&
							itemRemove.EndIndex.Row < row:
					row -= itemRemove.Range;
					break;

				case var thisItemRemoved when thisItemRemoved.ElementType == CollectionChangedOperation.Element.Item &&
							(thisItemRemoved.Action == NotifyCollectionChangedAction.Remove || thisItemRemoved.Action == NotifyCollectionChangedAction.Replace) &&
							thisItemRemoved.StartingIndex.Section == section &&
							thisItemRemoved.StartingIndex.Row <= row && thisItemRemoved.EndIndex.Row >= row:
					// This item has been removed or replaced, the index is no longer valid
					return null;

				case var itemMove when itemMove.ElementType == CollectionChangedOperation.Element.Item &&
							itemMove.Action == NotifyCollectionChangedAction.Move &&
							itemMove.StartingIndex.Section == section &&
							itemMove.NewStartingIndex is Uno.UI.IndexPath newStart &&
							newStart.Section == section:
					var oldMoveStart = itemMove.StartingIndex.Row;
					var oldMoveEnd = oldMoveStart + itemMove.Range - 1;
					var newMoveStart = newStart.Row;

					if (row >= oldMoveStart && row <= oldMoveEnd)
					{
						// This item was moved — remap to new position preserving relative offset
						row = newMoveStart + (row - oldMoveStart);
					}
					else if (oldMoveStart < newMoveStart)
					{
						// Forward move: items in the gap (oldMoveEnd, newMoveStart+Range] shift down
						if (row > oldMoveEnd && row <= newMoveStart + itemMove.Range - 1)
						{
							row -= itemMove.Range;
						}
					}
					else
					{
						// Backward move: items in the gap [newMoveStart, oldMoveStart) shift up
						if (row >= newMoveStart && row < oldMoveStart)
						{
							row += itemMove.Range;
						}
					}
					break;

				// Group operations are currently unsupported
				case var groupAdd when groupAdd.ElementType == CollectionChangedOperation.Element.Group &&
							groupAdd.Action == NotifyCollectionChangedAction.Add &&
							groupAdd.EndIndex.Section <= section:
				case var groupRemove when groupRemove.ElementType == CollectionChangedOperation.Element.Group &&
							groupRemove.Action == NotifyCollectionChangedAction.Remove &&
							groupRemove.EndIndex.Section < section:
				case var thisGroupRemoved when thisGroupRemoved.ElementType == CollectionChangedOperation.Element.Group &&
							(thisGroupRemoved.Action == NotifyCollectionChangedAction.Remove || thisGroupRemoved.Action == NotifyCollectionChangedAction.Replace) &&
							thisGroupRemoved.StartingIndex.Section <= section && thisGroupRemoved.EndIndex.Section >= section:
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().LogWarning("Collection change not supported");
					}
					break;
			}

			return Uno.UI.IndexPath.FromRowSection(row, section);
		}

		/// <summary>
		/// Sequentially applies offsets to a collection index resulting from multiple collection operations.
		/// </summary>
		/// <param name="index">The index in the collection prior to the operation</param>
		/// <param name="collectionChanges">The changes to be applied, in order from oldest to newest.</param>
		/// <returns>The offset position, or null if this position is no longer valid (ie because it has been removed by one of the operations).</returns>
		public static Uno.UI.IndexPath? Offset(Uno.UI.IndexPath index, IEnumerable<CollectionChangedOperation> collectionChanges)
		{
			Uno.UI.IndexPath? newIndex = index;

			foreach (var change in collectionChanges)
			{
				if (newIndex is Uno.UI.IndexPath newIndexValue)
				{
					newIndex = change.Offset(newIndexValue);
				}
				else
				{
					break;
				}
			}

			return newIndex;
		}

		public static int? Offset(int index, IEnumerable<CollectionChangedOperation> collectionChanges) => Offset(Uno.UI.IndexPath.FromRowSection(index, section: 0), collectionChanges)?.Row;

		public enum Element
		{
			Item,
			Group
		}
	}
}
