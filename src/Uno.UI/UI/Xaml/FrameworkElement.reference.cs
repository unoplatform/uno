using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Collections;
using Uno.UI;
using Windows.Foundation;

using View = Microsoft.UI.Xaml.UIElement;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement : IEnumerable
	{

		public string Name { get; set; }

		bool IFrameworkElementInternal.HasLayouter => throw new NotSupportedException("Reference assembly");

		internal T AddChild<T>(T child) where T : View => throw new NotSupportedException("Reference assembly");

		internal T AddChild<T>(T child, int index) where T : View => throw new NotSupportedException("Reference assembly");

		internal T RemoveChild<T>(T child) where T : View => throw new NotSupportedException("Reference assembly");

		internal View FindFirstChild() => throw new NotSupportedException("Reference assembly");

		internal MaterializableList<View> GetChildren() => throw new NotSupportedException("Reference assembly");

		private bool IsTopLevelXamlView() => throw new NotSupportedException("Reference assembly");

		internal void SuspendRendering() => throw new NotSupportedException("Reference assembly");

		internal void ResumeRendering() => throw new NotSupportedException();
		public IEnumerator GetEnumerator() => _children.GetEnumerator();

#pragma warning disable 67
#pragma warning disable IDE0051
		private event RoutedEventHandler _loaded;
		private event RoutedEventHandler _unloaded;
		public event TypedEventHandler<FrameworkElement, object> Loading;
		public event RoutedEventHandler Loaded;
		public event RoutedEventHandler Unloaded;
#pragma warning restore IDE0051
#pragma warning restore 67
	}
}
