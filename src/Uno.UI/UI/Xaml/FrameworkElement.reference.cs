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

		internal bool HasParent() => throw new NotSupportedException("Reference assembly");

		internal void InternalArrange(Rect frame) => throw new NotSupportedException("Reference assembly");

		internal void ForceLoaded() => throw new NotSupportedException("Reference assembly");

		internal int InvalidateMeasureCallCount => throw new NotSupportedException("Reference assembly");

		private bool IsTopLevelXamlView() => throw new NotSupportedException("Reference assembly");

		internal void SuspendRendering() => throw new NotSupportedException("Reference assembly");

		internal void ResumeRendering() => throw new NotSupportedException();
		public IEnumerator GetEnumerator() => _children.GetEnumerator();

		public double ActualWidth => throw new NotSupportedException("Reference assembly");

		public double ActualHeight => throw new NotSupportedException("Reference assembly");

		internal Size UnclippedDesiredSize => throw new NotSupportedException("Reference assembly");

#pragma warning disable 67
		private event TypedEventHandler<FrameworkElement, object> _loading;
		private event RoutedEventHandler _loaded;
		private event RoutedEventHandler _unloaded;
		public event TypedEventHandler<FrameworkElement, object> Loading;
		public event RoutedEventHandler Loaded;
		public event RoutedEventHandler Unloaded;
#pragma warning restore 67
	}
}
