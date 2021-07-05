#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Represents a tree of XAML content and information about the context in which it is hosted.
	/// </summary>
	/// <remarks>
	/// Effectively a public API wrapper around VisualTree.
	/// </remarks>
	public sealed partial class XamlRoot
	{
		private XamlRoot() { }

		internal static XamlRoot Current { get; } = new XamlRoot();

		public event TypedEventHandler<XamlRoot, XamlRootChangedEventArgs>? Changed;

		public UIElement? Content => Window.Current?.Content;

		public Size Size => Content?.RenderSize ?? Size.Empty;

		internal void NotifyChanged()
		{
			Changed?.Invoke(this, new XamlRootChangedEventArgs());
		}
	}
}
