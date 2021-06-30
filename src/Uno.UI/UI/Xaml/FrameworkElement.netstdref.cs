using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using View = Windows.UI.Xaml.UIElement;
using System.Collections;
using Uno.UI;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement : IEnumerable
	{
		bool IFrameworkElementInternal.HasLayouter => throw new NotSupportedException("Reference assembly");

		internal UIElement VisualParent => throw new NotSupportedException("Reference assembly");

		internal T AddChild<T>(T child) where T : View => throw new NotSupportedException("Reference assembly");

		internal T AddChild<T>(T child, int index) where T : View => throw new NotSupportedException("Reference assembly");

		private void OnAddChild(View child) => throw new NotSupportedException("Reference assembly");

		internal T RemoveChild<T>(T child) where T : View => throw new NotSupportedException("Reference assembly");

		internal View FindFirstChild() => throw new NotSupportedException("Reference assembly");

		internal virtual IEnumerable<View> GetChildren() => throw new NotSupportedException("Reference assembly");

		internal bool HasParent() => throw new NotSupportedException("Reference assembly");

		partial void OnMeasurePartial(Size slotSize) => throw new NotSupportedException("Reference assembly");

		internal void InternalArrange(Rect frame) => throw new NotSupportedException("Reference assembly");

		partial void OnGenericPropertyUpdatedPartial(DependencyPropertyChangedEventArgs args);

		internal void ForceLoaded() => throw new NotSupportedException("Reference assembly");

		private void EnterTree() => throw new NotSupportedException("Reference assembly");

		internal int InvalidateMeasureCallCount => throw new NotSupportedException("Reference assembly");

		private bool IsTopLevelXamlView() => throw new NotSupportedException("Reference assembly");

		internal void SuspendRendering() => throw new NotSupportedException("Reference assembly");

		internal void ResumeRendering() => throw new NotSupportedException();
		public IEnumerator GetEnumerator() => _children.GetEnumerator();

		public double ActualWidth => throw new NotSupportedException("Reference assembly");

		public double ActualHeight => throw new NotSupportedException("Reference assembly");

		internal Size UnclippedDesiredSize => throw new NotSupportedException("Reference assembly");

		public global::System.Uri BaseUri { get; internal set; }

#pragma warning disable 67
		private event RoutedEventHandler _loading;
		private event RoutedEventHandler _loaded;
		private event RoutedEventHandler _unloaded;
		public event RoutedEventHandler Loading;
		public event RoutedEventHandler Loaded;
		public event RoutedEventHandler Unloaded;
#pragma warning restore 67
	}
}
