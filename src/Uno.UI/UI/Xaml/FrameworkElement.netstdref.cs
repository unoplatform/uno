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
		bool IFrameworkElementInternal.HasLayouter => true;

		internal List<View> _children = new List<View>();

		partial void OnLoadingPartial();

		private protected virtual void OnPostLoading()
		{
		}

		internal T AddChild<T>(T child) where T : View
		{
			_children.Add(child);
			OnAddChild(child);

			return child;
		}

		internal T AddChild<T>(T child, int index) where T : View
		{
			_children.Insert(index, child);
			OnAddChild(child);

			return child;
		}

		private void OnAddChild(View child)
		{
		}

		internal T RemoveChild<T>(T child) where T : View
		{
			return child;
		}

		internal View FindFirstChild()
		{
			return _children.FirstOrDefault();
		}

		internal virtual IEnumerable<View> GetChildren()
		{
			return _children;
		}

		internal bool HasParent()
		{
			return Parent != null;
		}

		partial void OnMeasurePartial(Size slotSize)
		{
			
		}

		internal void InternalArrange(Rect frame)
		{
		}

		partial void OnGenericPropertyUpdatedPartial(DependencyPropertyChangedEventArgs args);

		public bool IsLoaded { get; private set; }

		internal void ForceLoaded()
		{
			IsLoaded = true;
			EnterTree();
		}

		private void EnterTree()
		{
		}

		internal int InvalidateMeasureCallCount { get; private set; }

		private bool IsTopLevelXamlView() => false;

		internal void SuspendRendering() => throw new NotSupportedException();

		internal void ResumeRendering() => throw new NotSupportedException();
		public IEnumerator GetEnumerator() => _children.GetEnumerator();

		public double ActualWidth => 0;

		public double ActualHeight => 0;

		internal Size UnclippedDesiredSize => new Size();

		public global::System.Uri BaseUri { get; internal set; }

		private protected virtual double GetActualWidth() => ActualWidth;
		private protected virtual double GetActualHeight() => ActualHeight;

#pragma warning disable 67
		public event RoutedEventHandler Loading;

		public event RoutedEventHandler Loaded;

		public event RoutedEventHandler Unloaded;
#pragma warning restore 67
	}
}
