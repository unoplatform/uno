using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno;
using Uno.Logging;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using View = Windows.UI.Xaml.UIElement;
using System.Collections;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement : IEnumerable
	{
		internal List<View> _children = new List<View>();

		partial void OnLoadingPartial();

		public View AddChild(View child)
		{
			_children.Add(child);
			child.SetParent(this);

			return child;
		}

		public View RemoveChild(View child)
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

			if(DesiredSizeSelector != null)
			{
				DesiredSize = DesiredSizeSelector(slotSize);
				RequestedDesiredSize = DesiredSize;
			}
			else if(RequestedDesiredSize != null)
			{
				DesiredSize = RequestedDesiredSize.Value;
			}
		}

		static partial void OnGenericPropertyUpdatedPartial(object dependencyObject, DependencyPropertyChangedEventArgs args);

		public bool IsLoaded { get; private set; }

		public void ForceLoaded()
		{
			IsLoaded = true;
			OnLoaded();
		}

		public int InvalidateMeasureCallCount { get; private set; }

		private bool IsTopLevelXamlView() => false;

		internal void SuspendRendering() => throw new NotSupportedException();

		internal void ResumeRendering() => throw new NotSupportedException();
		public IEnumerator GetEnumerator() => _children.GetEnumerator();
	}
}
