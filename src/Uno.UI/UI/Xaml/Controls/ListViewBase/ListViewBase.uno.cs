#nullable enable

using System;
using System.Collections.Specialized;
using Uno.Disposables;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBase
{
	/// <summary>
	/// When this flag is set, the ListViewBase will process every notification from <see cref="INotifyCollectionChanged"/> as if it
	/// were a 'Reset', triggering a complete refresh of the list. By default this is false.
	/// </summary>
	public bool RefreshOnCollectionChanged { get; set; }

	internal override bool IsSingleSelection => SelectionMode == ListViewSelectionMode.Single;

	// TODO Uno: The panel is created on ItemsPresenter's Loaded, after the first arrange. WinUI has it by then, so that
	// arrange can start the incremental load of an empty source.
	private protected override void OnItemsPanelRootPrepared()
	{
		base.OnItemsPanelRootPrepared();

		InvalidateArrange();
	}

	private VirtualKeyModifiers? m_currentInputModifiers;

	// TODO Uno: Pointer and key events carry their own modifiers, which input injection doesn't reflect in the
	// thread's key state read by CoreImports::Input_GetKeyboardModifiers, so the event's value wins while it's processed.
	private new VirtualKeyModifiers GetKeyboardModifiers() => m_currentInputModifiers ?? Control.GetKeyboardModifiers();

	internal IDisposable SetCurrentInputModifiers(VirtualKeyModifiers modifiers)
	{
		var previous = m_currentInputModifiers;
		m_currentInputModifiers = modifiers;

		return Disposable.Create(() => m_currentInputModifiers = previous);
	}

	// WinUI raises OnItemsChanged for ItemsSource changes too, because Items mirrors the source. Uno only does for
	// Items, so the ItemsSource changes (their sender is the source, not this control) are forwarded here.
	internal override void OnItemsSourceSingleCollectionChanged(object sender, NotifyCollectionChangedEventArgs args, int section)
	{
		if (RefreshOnCollectionChanged && args.Action != NotifyCollectionChangedAction.Reset)
		{
			args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
		}

		base.OnItemsSourceSingleCollectionChanged(sender, args, section);

		if (!ReferenceEquals(sender, this))
		{
			foreach (var change in ToVectorChanges(args, section))
			{
				OnItemsChanged(change);
			}
		}
	}
}
