#if !IS_UNIT_TESTS && !UNO_REFERENCE_API && !__MACOS__
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using Uno;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativeListViewBase : IFrameworkElement, DependencyObject, IScrollSnapPointsInfo
	{
		internal bool UseNativeSnapping { get; private set; }

		public bool AreHorizontalSnapPointsRegular => ((IScrollSnapPointsInfo)NativeLayout).AreHorizontalSnapPointsRegular;

		public bool AreVerticalSnapPointsRegular => ((IScrollSnapPointsInfo)NativeLayout).AreVerticalSnapPointsRegular;

		private ManagedWeakReference _xamlParentWeakReference;

		internal ListViewBase XamlParent
		{
			get => _xamlParentWeakReference?.Target as ListViewBase;
			set
			{
				WeakReferencePool.ReturnWeakReference(this, _xamlParentWeakReference);
				_xamlParentWeakReference = WeakReferencePool.RentWeakReference(this, value);
			}
		}

		[NotImplemented]
		public event EventHandler<object> HorizontalSnapPointsChanged
		{
			add
			{
				((IScrollSnapPointsInfo)NativeLayout).HorizontalSnapPointsChanged += value;
			}

			remove
			{
				((IScrollSnapPointsInfo)NativeLayout).HorizontalSnapPointsChanged -= value;
			}
		}

		[NotImplemented]
		public event EventHandler<object> VerticalSnapPointsChanged
		{
			add
			{
				((IScrollSnapPointsInfo)NativeLayout).VerticalSnapPointsChanged += value;
			}

			remove
			{
				((IScrollSnapPointsInfo)NativeLayout).VerticalSnapPointsChanged -= value;
			}
		}

		public IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)
		{
			return ((IScrollSnapPointsInfo)NativeLayout).GetIrregularSnapPoints(orientation, alignment);
		}

		public float GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset)
		{
			return ((IScrollSnapPointsInfo)NativeLayout).GetRegularSnapPoints(orientation, alignment, out offset);
		}

		internal double ItemsPresenterMinWidth
		{
			get => NativeLayout?.ItemsPresenterMinWidth ?? double.NaN;
			set
			{
				if (NativeLayout != null)
				{
					NativeLayout.ItemsPresenterMinWidth = value;
				}
			}
		}

		internal double ItemsPresenterMinHeight
		{
			get => NativeLayout?.ItemsPresenterMinHeight ?? double.NaN;
			set
			{
				if (NativeLayout != null)
				{
					NativeLayout.ItemsPresenterMinHeight = value;
				}
			}
		}
	}
}

#endif
