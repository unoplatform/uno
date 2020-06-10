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

		public bool HasParent()
		{
			return Parent != null;
		}

		protected internal override void OnInvalidateMeasure()
		{
			InvalidateMeasureCallCount++;
			base.OnInvalidateMeasure();
		}

		partial void OnMeasurePartial(Size slotSize)
		{
			MeasureCallCount++;
			AvailableMeasureSize = slotSize;

			if (DesiredSizeSelector != null)
			{
				DesiredSize = DesiredSizeSelector(slotSize);
				RequestedDesiredSize = DesiredSize;
			}
			else if (RequestedDesiredSize != null)
			{
				DesiredSize = RequestedDesiredSize.Value;
			}
		}

		internal void InternalArrange(Rect frame)
		{
			_layouter.Arrange(frame);
		}

		static partial void OnGenericPropertyUpdatedPartial(object dependencyObject, DependencyPropertyChangedEventArgs args);

		public bool IsLoaded { get; private set; }

		public void ForceLoaded()
		{
			IsLoaded = true;
			EnterTree();
		}

		private void EnterTree()
		{
			if (IsLoaded)
			{
				ApplyCompiledBindings();
				OnLoading();
				OnLoaded();

				foreach (var child in _children.OfType<FrameworkElement>())
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

		public global::System.Uri BaseUri { get; internal set; }

		private protected virtual double GetActualWidth() => ActualWidth;
		private protected virtual double GetActualHeight() => ActualHeight;
	}
}
