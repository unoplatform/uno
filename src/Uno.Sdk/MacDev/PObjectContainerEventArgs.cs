// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;

namespace Uno.Sdk.MacDev;

public sealed class PObjectContainerEventArgs : EventArgs
{
	internal PObjectContainerEventArgs(PObjectContainerAction action, string? key, PObject? oldItem, PObject? newItem)
	{
		Action = action;
		Key = key;
		OldItem = oldItem;
		NewItem = newItem;
	}

	public PObjectContainerAction Action { get; private set; }

	public string? Key { get; private set; }

	public PObject? OldItem { get; private set; }

	public PObject? NewItem { get; private set; }
}
