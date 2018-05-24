using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Temporary record of an <see cref="INotifyCollectionChanged"/> operation.
	/// </summary>
	internal class CollectionChangedOperation
	{
		public IndexPath StartingIndex { get; }
		public int Range { get; }
		public NotifyCollectionChangedAction Action { get; }
		public Element ElementType { get; }

		public IndexPath EndIndex => ElementType == Element.Item ?
			IndexPath.FromRowSection(StartingIndex.Row + Range - 1, StartingIndex.Section) :
			//Group change
			IndexPath.FromRowSection(StartingIndex.Row, StartingIndex.Section + Range - 1);

		public CollectionChangedOperation(IndexPath startingIndex, int range, NotifyCollectionChangedAction action, Element elementType)
		{
			StartingIndex = startingIndex;
			Range = range;
			Action = action;
			ElementType = elementType;
		}

		public enum Element
		{
			Item,
			Group
		}
	}
}
