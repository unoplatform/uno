using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Provides data for the ContainerContentChanging event.
	/// </summary>
	public partial class ContainerContentChangingEventArgs
	{
		/// <summary>
		/// Initializes a new instance of the ContainerContentChangingEventArgs class.
		/// </summary>
		public ContainerContentChangingEventArgs()
		{
		}

		internal ContainerContentChangingEventArgs(object item, SelectorItem itemContainer, int itemIndex)
		{
			Item = item;
			ItemContainer = itemContainer;
			ItemIndex = itemIndex;
		}

		/// <summary>
		/// Gets or sets a value that marks the routed event as handled.
		/// A true value for Handled prevents most handlers along the event
		/// route from handling the same event again.
		/// </summary>
		public bool Handled { get; set; }

		/// <summary>
		/// Gets a value that indicates whether this container is in the recycle queue
		/// of the ListViewBase and is not being used to visualize a data item.
		/// </summary>
		/// <remarks>Currently always false in Uno.</remarks>
		public bool InRecycleQueue { get; }

		/// <summary>
		/// Gets the data item associated with this container.
		/// </summary>
		public object Item { get; }

		/// <summary>
		/// Gets the UI container used to display the current data item.
		/// </summary>
		public SelectorItem ItemContainer { get; }

		/// <summary>
		/// Gets the index in the ItemsSource of the data item associated with this container.
		/// </summary>
		public int ItemIndex { get; }
	}
}
