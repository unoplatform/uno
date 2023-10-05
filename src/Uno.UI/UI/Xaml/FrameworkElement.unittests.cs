using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using View = Microsoft.UI.Xaml.UIElement;
using System.Collections;
using Uno.UI;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement : IEnumerable
	{
		bool IFrameworkElementInternal.HasLayouter => true;

		internal List<View> _children = new List<View>();

		internal bool ShouldInterceptInvalidate { get; set; }

		internal void UpdateHitTest() { }

		private protected virtual void OnPostLoading() { }

		partial void OnLoadingPartial();

		public T AddChild<T>(T child) where T : View
		{
			_children.Add(child);
			OnAddChild(child);

			return child;
		}

		public T AddChild<T>(T child, int index) where T : View
		{
			_children.Insert(index, child);
			OnAddChild(child);

			return child;
		}

		private void OnAddChild(View child)
		{
			child.SetParent(this);
			if (child is FrameworkElement fe)
			{
				fe.IsLoaded = IsLoaded;
				fe.EnterTree();
			}
		}

		public T RemoveChild<T>(T child) where T : View
		{
			_children.Remove(child);
			child.SetParent(null);

			if (child is FrameworkElement fe)
			{
				fe.OnUnloaded();
			}

			return child;
		}

		public View FindFirstChild()
		{
			return _children.FirstOrDefault();
		}

		public virtual IEnumerable<View> GetChildren()
		{
			return _children;
		}

		internal bool HasParent()
		{
			return Parent != null;
		}

		protected internal override void OnInvalidateMeasure()
		{
			InvalidateMeasureCallCount++;
			base.OnInvalidateMeasure();
		}

		internal void InternalArrange(Rect frame)
		{
			_layouter.Arrange(frame);
		}

		public bool IsLoaded { get; private set; }

		public void ForceLoaded()
		{
			IsLoaded = true;
			EnterTree();
		}

		private void EnterTree()
		{
			if (XamlRoot is null)
			{
				XamlRoot = Window.InitialWindow?.RootElement?.XamlRoot;
			}

			if (IsLoaded)
			{
				OnLoading();
				OnPostLoading();
				OnLoaded();

				foreach (var child in _children.OfType<FrameworkElement>().ToArray())
				{
					child.IsLoaded = IsLoaded;
					child.EnterTree();
				}
			}
		}

		public int InvalidateMeasureCallCount { get; private set; }

		private bool IsTopLevelXamlView() => false;

		internal void SuspendRendering() => throw new NotSupportedException();

		internal void ResumeRendering() => throw new NotSupportedException();
		public IEnumerator GetEnumerator() => _children.GetEnumerator();

		public double ActualWidth => Arranged.Width;

		public double ActualHeight => Arranged.Height;

		public Size UnclippedDesiredSize => _layouter._unclippedDesiredSize;

		private protected override double GetActualWidth() => ActualWidth;
		private protected override double GetActualHeight() => ActualHeight;
	}
}
