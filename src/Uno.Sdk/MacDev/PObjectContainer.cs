// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;
using System.IO;

namespace Uno.Sdk.MacDev;

public abstract class PObjectContainer : PObject
{
	public abstract int Count { get; }

	public bool Reload(string fileName)
	{
		using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
		using var ctx = PropertyListFormat.CreateReadContext(stream);
		if (ctx is null)
		{
			return false;
		}

		return Reload(ctx);
	}

	protected abstract bool Reload(PropertyListFormat.ReadWriteContext ctx);

	protected void OnChildAdded(string? key, PObject child)
	{
		child.Parent = this;

		OnCollectionChanged(PObjectContainerAction.Added, key, null, child);
	}

	internal void OnCollectionChanged(string key, PObject child) => OnCollectionChanged(PObjectContainerAction.Changed, key, null, child);

	protected void OnChildRemoved(string? key, PObject child)
	{
		child.Parent = null;

		OnCollectionChanged(PObjectContainerAction.Removed, key, child, null);
	}

	protected void OnChildReplaced(string? key, PObject oldChild, PObject newChild)
	{
		oldChild.Parent = null;
		newChild.Parent = this;

		OnCollectionChanged(PObjectContainerAction.Replaced, key, oldChild, newChild);
	}

	protected void OnCleared() => OnCollectionChanged(PObjectContainerAction.Cleared, null, null, null);

	protected void OnCollectionChanged(PObjectContainerAction action, string? key, PObject? oldChild, PObject? newChild)
	{
		if (SuppressChangeEvents)
		{
			return;
		}

		var handler = CollectionChanged;
		if (handler is not null)
		{
			handler(this, new PObjectContainerEventArgs(action, key, oldChild, newChild));
		}

		OnChanged(EventArgs.Empty);

		Parent?.OnCollectionChanged(Key!, this);
	}

	public event EventHandler<PObjectContainerEventArgs>? CollectionChanged;
}
