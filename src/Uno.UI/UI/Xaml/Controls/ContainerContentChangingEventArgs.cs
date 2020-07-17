#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
#endif
	public partial class ContainerContentChangingEventArgs
	{
		public ContainerContentChangingEventArgs()
		{
		}

		internal ContainerContentChangingEventArgs(object item, SelectorItem itemContainer, int itemIndex)
		{
			Item = item;
			ItemContainer = itemContainer;
			ItemIndex = itemIndex;
		}

		public bool Handled { get; set; }

		public bool InRecycleQueue { get; }

		public object Item { get; }

		public SelectorItem ItemContainer { get; }

		public int ItemIndex { get; }
	}
}
