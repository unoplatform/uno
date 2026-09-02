#nullable enable

#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using _ScrollContentPresenter = Microsoft.UI.Xaml.Controls.ScrollContentPresenter;
#else
using _ScrollContentPresenter = Microsoft.UI.Xaml.Controls.IScrollContentPresenter;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		protected override void OnApplyTemplate()
		{
			// Cleanup previous template
			DetachScrollBars();

			base.OnApplyTemplate();


			var scpTemplatePart = GetTemplateChild(Parts.WinUI3.Scroller) ?? GetTemplateChild(Parts.Uwp.ScrollContentPresenter);
			_presenter = scpTemplatePart as _ScrollContentPresenter;

			_isTemplateApplied = _presenter != null;

#if __WASM__ || __SKIA__
			if (_presenter != null && ForceChangeToCurrentView)
			{
				_presenter.ForceChangeToCurrentView = ForceChangeToCurrentView;
			}
#endif
			// Load new template
			_verticalScrollbar = null;
			_isVerticalScrollBarMaterialized = false;
			_horizontalScrollbar = null;
			_isHorizontalScrollBarMaterialized = false;

#if __APPLE_UIKIT__ || __ANDROID__
			if (scpTemplatePart is ScrollContentPresenter scp && scp.Native is null)
			{
				// For Android and iOS, ensure that the ScrollContentPresenter contains a native SCP,
				// which will handle the actual scrolling.
				var nativeSCP = new NativeScrollContentPresenter(this);
				scp.Content = scp.Native = nativeSCP;
				_presenter = nativeSCP;
			}
#endif

			if (scpTemplatePart is ScrollContentPresenter presenter)
			{
				presenter.ScrollOwner = this;
				Presenter = presenter;
			}
			else
			{
				Presenter = null;
			}

			// We update the scrollability properties here in order to make sure to set the right scrollbar visibility
			// on the _presenter as soon as possible
			UpdateComputedVerticalScrollability(invalidate: false);
			UpdateComputedHorizontalScrollability(invalidate: false);

			ApplyScrollContentPresenterContent(Content);

			OnApplyTemplatePartial();

			// Apply correct initial zoom settings
			OnZoomModeChanged(ZoomMode);

			OnBringIntoViewOnFocusChangeChangedPartial(BringIntoViewOnFocusChange);

			PrepareScrollIndicator();
		}

		partial void OnApplyTemplatePartial();

		void IFrameworkTemplatePoolAware.OnTemplateRecycled()
		{
			if (VerticalOffset != 0 || HorizontalOffset != 0 || ZoomFactor != 1)
			{
				ChangeView(
					horizontalOffset: 0,
					verticalOffset: 0,
					zoomFactor: 1,
					disableAnimation: true
				);
			}
		}
	}
}
