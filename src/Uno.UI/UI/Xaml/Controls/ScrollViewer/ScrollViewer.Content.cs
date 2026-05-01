#nullable enable

using Uno.Disposables;
using Microsoft.UI.Xaml.Controls.Primitives;

#if __ANDROID__
using View = Android.Views.View;
#elif __APPLE_UIKIT__
using View = UIKit.UIView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		#region Content forwarding to the ScrollContentPresenter
		protected override void OnContentChanged(object? oldValue, object? newValue)
		{
			base.OnContentChanged(oldValue, newValue);

			if (_presenter is not null)
			{
				ApplyScrollContentPresenterContent(newValue);
			}

			UpdateSizeChangedSubscription();

			_snapPointsInfo = newValue as IScrollSnapPointsInfo;

#if __SKIA__
			// MUX Reference ScrollViewer_Partial.cpp:9951 — OnContentChanged
			// notifies the new SCP port that the content has changed so it can
			// reset its child-actual-size special mode and re-hook scrolling
			// components.
			if (m_trElementScrollContentPresenter is not null)
			{
				m_trElementScrollContentPresenter.OnContentChanging(oldValue);
			}

			// C++ ScrollViewer_Partial.cpp:OnPropertyChanged2 ContentControl_Content case →
			// IFC(OnContentPropertyChanged()). The new port resets stretch-alignment-treated-
			// as-near flags and triggers OnManipulatabilityAffectingPropertyChanged for the
			// new content (DM-bound, no-op without DM adapter on Skia).
			OnContentPropertyChanged();
#endif
		}

		private void ApplyScrollContentPresenterContent(object? content)
		{
			// Then explicitly propagate the Content to the _presenter
			if (_presenter != null)
			{
				_presenter.Content = content as View;
			}
		}

		private void UpdateSizeChangedSubscription(bool isCleanupRequired = false)
		{
			_sizeChangedSubscription.Disposable = null;
			if (!isCleanupRequired &&
				Content is IFrameworkElement element)
			{
				element.SizeChanged += OnElementSizeChanged;
				_sizeChangedSubscription.Disposable = Disposable.Create(() =>
					element.SizeChanged -= OnElementSizeChanged
				);
			}

			void OnElementSizeChanged(object sender, SizeChangedEventArgs args)
				=> UpdateDimensionProperties();
		}
		#endregion
	}
}
