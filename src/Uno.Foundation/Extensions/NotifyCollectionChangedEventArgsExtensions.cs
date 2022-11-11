using System;
using System.Collections.Specialized;
using Windows.Foundation.Collections;

namespace Uno.Extensions;

public static class NotifyCollectionChangedEventArgsExtensions
{
	public static IVectorChangedEventArgs ToVectorChangedEventArgs(this NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
	{
		var change = notifyCollectionChangedEventArgs.Action.ToCollectionChange();

		int index;
		switch (notifyCollectionChangedEventArgs.Action)
		{
			case NotifyCollectionChangedAction.Add:
			case NotifyCollectionChangedAction.Replace:
				index = notifyCollectionChangedEventArgs.NewStartingIndex;
				break;
			case NotifyCollectionChangedAction.Remove:
				index = notifyCollectionChangedEventArgs.OldStartingIndex;
				break;
			case NotifyCollectionChangedAction.Move:
			case NotifyCollectionChangedAction.Reset:
				index = 0;
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		return new VectorChangedEventArgs(notifyCollectionChangedEventArgs, change, (uint)index);
	}

	public static CollectionChange ToCollectionChange(this NotifyCollectionChangedAction action)
	{
		switch (action)
		{
			case NotifyCollectionChangedAction.Add:
				return CollectionChange.ItemInserted;
			case NotifyCollectionChangedAction.Move:
				return CollectionChange.Reset;
			case NotifyCollectionChangedAction.Remove:
				return CollectionChange.ItemRemoved;
			case NotifyCollectionChangedAction.Replace:
				return CollectionChange.ItemChanged;
			case NotifyCollectionChangedAction.Reset:
				return CollectionChange.Reset;
		}

		throw new ArgumentOutOfRangeException();
	}

	public static NotifyCollectionChangedEventArgs ToNotifyCollectionChangedEventArgs(this IVectorChangedEventArgs vectorChangedEventArgs)
	{
		if (vectorChangedEventArgs is VectorChangedEventArgs { NotifyCollectionChanged: not null } args)
		{
			return args.NotifyCollectionChanged;
		}

		var action = vectorChangedEventArgs.CollectionChange.ToNotifyCollectionChangedAction();

		// Note: we don't populate NewItems/OldItems, but Uno only checks their lengths
		switch (action)
		{
			case NotifyCollectionChangedAction.Add:
			case NotifyCollectionChangedAction.Remove:
				return new NotifyCollectionChangedEventArgs(action, changedItem: null, index: (int)vectorChangedEventArgs.Index);
			case NotifyCollectionChangedAction.Replace:
				return new NotifyCollectionChangedEventArgs(action, newItem: null, oldItem: null, index: (int)vectorChangedEventArgs.Index);
			case NotifyCollectionChangedAction.Reset:
				return new NotifyCollectionChangedEventArgs(action);
		}

		throw new ArgumentOutOfRangeException();
	}

	public static NotifyCollectionChangedAction ToNotifyCollectionChangedAction(this CollectionChange change)
	{
		switch (change)
		{
			case CollectionChange.ItemChanged:
				return NotifyCollectionChangedAction.Replace;
			case CollectionChange.ItemInserted:
				return NotifyCollectionChangedAction.Add;
			case CollectionChange.ItemRemoved:
				return NotifyCollectionChangedAction.Remove;
			case CollectionChange.Reset:
				return NotifyCollectionChangedAction.Reset;
		}

		throw new ArgumentOutOfRangeException();
	}
}
