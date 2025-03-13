using DirectUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Uno.Disposables;
using Windows.UI;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	private void ClearRootScrollViewer()
	{
		if (m_RootScrollViewer is null)
		{
			// nothing to do
			return;
		}

		if (m_tokRootScrollViewerSizeChanged.Disposable is not null)
		{
			// remove our SizeChanged event handler from the current RSV
			m_tokRootScrollViewerSizeChanged.Disposable = null;
		}

		if (m_isUwpWindowContent)
		{
			// clear core's reference to the current RSV
			// TODO: MZ:
			// CoreImports.Application_SetRootScrollViewer(DXamlCore.Current.GetHandle(), null /*pRootScrollViewer*/, null/*pRootScrollContentPresenter*/);
		}

		m_RootScrollViewer = null;

		if (m_RootSVContentPresenter is not null)
		{
			m_RootSVContentPresenter = null;
		}
	}

	private void CreateRootScrollViewer(UIElement pContent)
	{
		Microsoft.UI.Xaml.Controls.ScrollContentPresenter spRootSVContentPresenter;
		SolidColorBrush spTransparentBrush;
		Color transparentColor;

		MUX_ASSERT(m_RootScrollViewer is null);
		MUX_ASSERT(m_RootSVContentPresenter is null);

		var windowBounds = DXamlCore.Current.GetContentBoundsForElement(m_owner as DependencyObject);

		// Create a new ScrollViewer and set it as root
		RootScrollViewer spRootScrollViewer = new();

		UIElement pRootScrollViewerNativePeerNoRef = spRootScrollViewer;

		// Create the ScrollContentPresenter for the root ScrollViewer.
		spRootSVContentPresenter = new();

		// Create the root border to ensure the content position under ScrollViewer.
		// Without the border, the content can be compressed by changing the ScrollViewer height.
		Border spRootBorder = new();

		// Set appropriate properties of the RSV
		spRootScrollViewer.ZoomMode = ZoomMode.Disabled;
		spRootScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
		spRootScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
		spRootScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
		spRootScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
		spRootScrollViewer.IsTabStop = true;

		if (pContent is FrameworkElement spContentAsFE)
		{
			// Specify the root ScrollViewer and Border width/height with Windows size.
			spRootScrollViewer.Height = windowBounds.Height;
			spRootScrollViewer.Width = windowBounds.Width;
			spRootScrollViewer.HorizontalContentAlignment = HorizontalAlignment.Left;
			spRootScrollViewer.VerticalContentAlignment = VerticalAlignment.Top;

			var runtimeEnabledFeatureDetector = RuntimeFeatureBehavior.GetRuntimeEnabledFeatureDetector();
			if (!runtimeEnabledFeatureDetector.IsFeatureEnabled(RuntimeEnabledFeature.DoNotSetRootScrollViewerBackground))
			{
				// Create the transparent brush to set it on the Border as the background property.
				transparentColor = Color.FromArgb(0, 255, 255, 255);
				spTransparentBrush = new() { Color = transparentColor };
				spRootBorder.Background = spTransparentBrush;
			}

			spRootBorder.Height = windowBounds.Height;
			spRootBorder.Width = windowBounds.Width;
		}

		m_RootScrollViewer = spRootScrollViewer;
		m_RootSVContentPresenter = spRootSVContentPresenter;

		// Set the ScrollContentPresenter manually into the root ScrollViewer to avoid
		// the applying a template that cause the startup time delaying.
		spRootScrollViewer.SetRootScrollContentPresenter(spRootSVContentPresenter);

		// Set the content of root ScrollViewer
		spRootScrollViewer.Content = spRootBorder;

		// Attach the RootVisual size changed event handler
		spRootScrollViewer.SizeChanged += OnRootScrollViewerSizeChanged;
		m_tokRootScrollViewerSizeChanged.Disposable = Disposable.Create(() => spRootScrollViewer.SizeChanged -= OnRootScrollViewerSizeChanged);

		// TODO: MZ:
		//// Root SV will apply the template to bind the content with page content when the root SV is entered in the tree.
		//// This is for firing the loaded event on the right time stead of delaying with Measure happening.
		//CoreImports.ScrollContentControl_SetRootScrollViewerSetting((CScrollContentControl*)(pRootScrollViewerNativePeerNoRef), 0x01 /* RootScrollViewerSetting_ApplyTemplate */, true /* fApplyTemplate */);
		//CoreImports.ScrollContentControl_SetRootScrollViewerOriginalHeight((CScrollContentControl*)(pRootScrollViewerNativePeerNoRef), windowBounds.Height);

		//if (m_isUwpWindowContent)
		//{
		//	// The m_owner can be a XAML Window or a XamlIslandRoot.  The content for the window is special, in that it triggers
		//	// the initialization of the application.

		//	// Set the root ScrollViewer
		//	(CoreImports.Application_SetRootScrollViewer(
		//		DXamlCore.GetCurrent().GetHandle(),
		//		(CScrollContentControl*)(pRootScrollViewerNativePeerNoRef),
		//		spRootSVContentPresenter ? (CContentPresenter*)(spRootSVContentPresenter.GetHandle()) : null));
		//}

		// Cleanup:
		//if (spRootScrollViewer)
		//{
		//	// UnpegNoRef is necessary because we did a CreateInstance and didn't add the ScrollViewer to the tree.
		//	spRootScrollViewer.UnpegNoRef();
		//}
		//if (spRootSVContentPresenter)
		//{
		//	// UnpegNoRef is necessary because we did a CreateInstance and didn't add the ScrollContentPresenter to the tree.
		//	spRootSVContentPresenter.UnpegNoRef();
		//}
	}

	private void OnWindowSizeChanged()
	{

		var pCore = DXamlCore.Current;
		if (pCore is null)
		{
			// The XamlCore is gone.
			// This can happen at shutdown when multiple CoreWindows are active in a single process.
			return;
		}

		// Note that when calling get_Bounds in the scope of a window size changed event,
		// the returned size is the current (new) size.
		var windowBounds = pCore.GetContentBoundsForElement(m_owner as DependencyObject);

		var spRootScrollViewerAsFE = m_RootScrollViewer as FrameworkElement;
		if (spRootScrollViewerAsFE is not null)
		{
			spRootScrollViewerAsFE.Height = windowBounds.Height;
			spRootScrollViewerAsFE.Width = windowBounds.Width;
		}

		var spRootScrollViewerAsCC = m_RootScrollViewer as ContentControl;
		if (spRootScrollViewerAsCC is not null)
		{
			var spContent = spRootScrollViewerAsCC.Content;
			var spRootBorder = spContent as Border;
			if (spRootBorder is not null)
			{
				var spRootBorderAsFE = spRootBorder as FrameworkElement;
				if (spRootBorderAsFE is not null)
				{
					spRootBorderAsFE.Height = windowBounds.Height;
					spRootBorderAsFE.Width = windowBounds.Width;
				}
			}
		}

		// TODO: MZ
		//UIElement spRootScrollViewerAsUE = m_RootScrollViewer;
		//if (spRootScrollViewerAsUE is not null)
		//{
		//	UIElement pRootScrollViewerNativePeerNoRef = spRootScrollViewerAsUE;
		//	CoreImports.ScrollContentControl_SetRootScrollViewerSetting(
		//		(CScrollContentControl*)(pRootScrollViewerNativePeerNoRef),
		//		0x02 /* RootScrollViewerSetting_ProcessWindowSizeChanged */,
		//		true /* window size changed */));
		//	CoreImports.ScrollContentControl_SetRootScrollViewerOriginalHeight((CScrollContentControl*)(pRootScrollViewerNativePeerNoRef), windowBounds.Height);
		//}
	}

	private void OnRootScrollViewerSizeChanged(
		 object pSender,
		 SizeChangedEventArgs pArgs)
	{
		//if (pSender is UIElement spRootScrollViewerAsUE)
		//{
		//	bool bIsProcessWindowSizeChanged = false;

		//	UIElement pRootScrollViewerNativePeerNoRef = spRootScrollViewerAsUE;

		//	(CoreImports.ScrollContentControl_GetRootScrollViewerSetting(
		//			(CScrollContentControl*)(pRootScrollViewerNativePeerNoRef),
		//			0x02 /* RootScrollViewerSetting_ProcessWindowSizeChanged */,
		//			bIsProcessWindowSizeChanged));

		//	if (bIsProcessWindowSizeChanged)
		//	{
		//		(CoreImports.ScrollContentControl_SetRootScrollViewerSetting(
		//				(CScrollContentControl*)(pRootScrollViewerNativePeerNoRef),
		//				0x02 /* RootScrollViewerSetting_ProcessWindowSizeChanged */,
		//				false /* window size changed */));
		//	}
		//}
	}
}
