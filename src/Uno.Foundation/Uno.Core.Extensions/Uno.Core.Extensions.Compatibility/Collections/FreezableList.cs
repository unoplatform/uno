#nullable enable

#if NET7_0_OR_GREATER

namespace Uno.Collections;

/// <summary>
/// A common problem we have in DependencyObjectStore is the ability to loop through a collection while having a possibility that it gets mutated.
/// For example, in DependencyObjectStore we have a list of DP changed callbacks, and at some point we want to loop through them and executing these callbacks.
/// When a callback is executed, there is a possibility for it to modify the list of DP changed callbacks, which will be problematic.
/// Historically, we did use an ImmutableList for this case, and if the list has changed during iterating over it, a new list is created and the original list is the same.
/// This has cost in that adding a new item to the list will allocate a new array then copy from the old array to the new one.
/// This class allows for a "temporary" freeze of the collection, and any changes are queued until the collection is unfrozen.
/// So, the usage will be that before iterating, the collection is frozen, and after finishing the iterations, it's unfrozen back so that queued updates are applied.
/// </summary>
/// <remarks>
/// Important: DON'T cast this collection to List<typeparamref name="T"/>.
/// The way it's implemented is by shadowing "Add".
/// Casting it will bypass the _freezeCount checks.
/// Also note that currently, this collection only supports Add(T item) and Remove(T item) methods.
/// Any other method will execute code from List<typeparamref name="T"/> directly and bypass the _freezeCount check.
/// </remarks>
internal class FreezableList<T> : List<T>
{
	private enum FrozenOperationKind : byte
	{
		Add,
		Remove,
	}

	private readonly struct QueuedOperation
	{
		public FrozenOperationKind FrozenOperationKind { get; }

		public T Item { get; }

		public QueuedOperation(FrozenOperationKind kind, T item)
		{
			FrozenOperationKind = kind;
			Item = item;
		}
	}

	private int _freezeCount;

	private List<QueuedOperation>? _queuedOperations;

	public new void Add(T item)
	{
		if (_freezeCount > 0)
		{
			(_queuedOperations ??= new()).Add(new QueuedOperation(FrozenOperationKind.Add, item));
		}
		else
		{
			base.Add(item);
		}
	}

	public new void Remove(T item)
	{
		if (_freezeCount > 0)
		{
			(_queuedOperations ??= new()).Add(new QueuedOperation(FrozenOperationKind.Remove, item));
		}
		else
		{
			base.Remove(item);
		}
	}

	public void Freeze() => _freezeCount++;

	public void Unfreeze()
	{
		_freezeCount--;
		if (_queuedOperations is null || _freezeCount > 0)
		{
			return;
		}

		foreach (var queuedOperation in _queuedOperations)
		{
			switch (queuedOperation.FrozenOperationKind)
			{
				case FrozenOperationKind.Add:
					base.Add(queuedOperation.Item);
					break;

				case FrozenOperationKind.Remove:
					base.Remove(queuedOperation.Item);
					break;
			}
		}

		_queuedOperations.Clear();
	}
}
#endif
