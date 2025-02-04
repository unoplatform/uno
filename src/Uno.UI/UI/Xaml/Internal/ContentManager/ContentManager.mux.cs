namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	private void ClearRootScrollViewer()
	{
		if (m_RootScrollViewer is null)
		{
			// nothing to do
			goto Cleanup;
		}

		if (m_tokRootScrollViewerSizeChanged.value)
		{
			// remove our SizeChanged event handler from the current RSV
			ScrollViewer pScrollViewer = (ScrollViewer)(m_RootScrollViewer);
			pScrollViewer.remove_SizeChanged(m_tokRootScrollViewerSizeChanged);
			m_tokRootScrollViewerSizeChanged.value = 0;
		}

		if (m_isUwpWindowContent)
		{
			// clear core's reference to the current RSV
			CoreImports.Application_SetRootScrollViewer(DXamlCore.GetCurrent().GetHandle(), null /*pRootScrollViewer*/, null/*pRootScrollContentPresenter*/);
		}

		m_owner.RemovePtrValue(m_RootScrollViewer);
		m_RootScrollViewer.Clear();

		if (m_RootSVContentPresenter)
		{
			m_owner.RemovePtrValue(m_RootSVContentPresenter);
			m_RootSVContentPresenter.Clear();
		}
	}

	private void CreateRootScrollViewer(UIElement* pContent)
	{
		wf.Rect windowBounds;
		RootScrollViewer spRootScrollViewer;
		ScrollContentPresenter spRootSVContentPresenter;
		Border spRootBorder;
		FrameworkElement spContentAsFE;
		ISizeChangedEventHandler spRootScrollViewerSizeChangedHandler;
		SolidColorBrush spTransparentBrush;
		wu.Color transparentColor;
		UIElement* pRootScrollViewerNativePeerNoRef = null;

		IFCEXPECT_MUX_ASSERT(!m_RootScrollViewer);
		IFCEXPECT_MUX_ASSERT(!m_RootSVContentPresenter);

		DXamlCore.GetCurrent().GetContentBoundsForElement(m_owner.GetHandle(), &windowBounds);

		// Create a new ScrollViewer and set it as root
		spRootScrollViewer = new();

		pRootScrollViewerNativePeerNoRef = (UIElement*)(spRootScrollViewer.GetHandle());

		// Create the ScrollContentPresenter for the root ScrollViewer.
		spRootSVContentPresenter = new();

		// Create the root border to ensure the content position under ScrollViewer.
		// Without the border, the content can be compressed by changing the ScrollViewer height.
		spRootBorder = new();

		// Set appropriate properties of the RSV
		spRootScrollViewer.ZoomMode = (ZoomMode_Disabled);
		spRootScrollViewer.VerticalScrollMode = (ScrollMode_Disabled);
		spRootScrollViewer.HorizontalScrollMode = (ScrollMode_Disabled);
		spRootScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility_Hidden;
		spRootScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility_Hidden;
		spRootScrollViewer.IsTabStop = true;

		if (SUCCEEDED(ctl.do_query_interface(spContentAsFE, pContent)))
		{
			// Specify the root ScrollViewer and Border width/height with Windows size.
			spRootScrollViewer.Height = windowBounds.Height;
			spRootScrollViewer.Width = windowBounds.Width;
			spRootScrollViewer.HorizontalContentAlignment = HorizontalAlignment_Left;
			spRootScrollViewer.VerticalContentAlignment = VerticalAlignment_Top;

			var runtimeEnabledFeatureDetector = RuntimeFeatureBehavior.GetRuntimeEnabledFeatureDetector();
			if (!runtimeEnabledFeatureDetector.IsFeatureEnabled(RuntimeFeatureBehavior.RuntimeEnabledFeature.DoNotSetRootScrollViewerBackground))
			{
				// Create the transparent brush to set it on the Border as the background property.
				spTransparentBrush = new();
				transparentColor.A = 0;
				transparentColor.R = 255;
				transparentColor.G = 255;
				transparentColor.B = 255;
				spTransparentBrush.Color = transparentColor;
				spRootBorder.Background = spTransparentBrush;
			}

			spRootBorder.Height = windowBounds.Height;
			spRootBorder.Width = windowBounds.Width;
		}

		m_owner.m_RootScrollViewer = spRootScrollViewer;
		m_owner.m_RootSVContentPresenter = spRootSVContentPresenter;

		// Set the ScrollContentPresenter manually into the root ScrollViewer to avoid
		// the applying a template that cause the startup time delaying.
		spRootScrollViewer.SetRootScrollContentPresenter(spRootSVContentPresenter);

		// Set the content of root ScrollViewer
		spRootScrollViewer.Content = ctl.as_iinspectable(spRootBorder);

		// Attach the RootVisual size changed event handler
		spRootScrollViewerSizeChangedHandler.Attach(
			new StaticMemberEventHandler<
				ISizeChangedEventHandler,
				object,
				ISizeChangedEventArgs>(&ContentManager.OnRootScrollViewerSizeChanged));
		spRootScrollViewer.add_SizeChanged(spRootScrollViewerSizeChangedHandler, &m_tokRootScrollViewerSizeChanged);

		// Root SV will apply the template to bind the content with page content when the root SV is entered in the tree.
		// This is for firing the loaded event on the right time stead of delaying with Measure happening.
		CoreImports.ScrollContentControl_SetRootScrollViewerSetting((CScrollContentControl*)(pRootScrollViewerNativePeerNoRef), 0x01 /* RootScrollViewerSetting_ApplyTemplate */, true /* fApplyTemplate */);
		CoreImports.ScrollContentControl_SetRootScrollViewerOriginalHeight((CScrollContentControl*)(pRootScrollViewerNativePeerNoRef), windowBounds.Height);

		if (m_isUwpWindowContent)
		{
			// The m_owner can be a XAML Window or a XamlIslandRoot.  The content for the window is special, in that it triggers
			// the initialization of the application.

			// Set the root ScrollViewer
			(CoreImports.Application_SetRootScrollViewer(
				DXamlCore.GetCurrent().GetHandle(),
				(CScrollContentControl*)(pRootScrollViewerNativePeerNoRef),
				spRootSVContentPresenter ? (CContentPresenter*)(spRootSVContentPresenter.GetHandle()) : null));
		}

	Cleanup:
		if (spRootScrollViewer)
		{
			// UnpegNoRef is necessary because we did a CreateInstance and didn't add the ScrollViewer to the tree.
			spRootScrollViewer.UnpegNoRef();
		}
		if (spRootSVContentPresenter)
		{
			// UnpegNoRef is necessary because we did a CreateInstance and didn't add the ScrollContentPresenter to the tree.
			spRootSVContentPresenter.UnpegNoRef();
		}

		RRETURN(hr);
	}

	private void OnWindowSizeChanged()
	{

		var pCore = DXamlCore.GetCurrent();
		if (!pCore)
		{
			// The XamlCore is gone.
			// This can happen at shutdown when multiple CoreWindows are active in a single process.
			return S_OK;
		}

		wf.Rect windowBounds = default;
		FrameworkElement spRootScrollViewerAsFE;
		IContentControl spRootScrollViewerAsCC;
		UIElement spRootScrollViewerAsUE;

		// Note that when calling get_Bounds in the scope of a window size changed event,
		// the returned size is the current (new) size.
		pCore.GetContentBoundsForElement(m_owner.GetHandle(), &windowBounds);

		spRootScrollViewerAsFE = m_RootScrollViewer.AsOrNull<FrameworkElement>();
		if (spRootScrollViewerAsFE)
		{
			spRootScrollViewerAsFE.Height = windowBounds.Height;
			spRootScrollViewerAsFE.Width = windowBounds.Width;
		}

		spRootScrollViewerAsCC = m_RootScrollViewer.AsOrNull<IContentControl>();
		if (spRootScrollViewerAsCC)
		{
			object spContent;
			IBorder spRootBorder;
			FrameworkElement spRootBorderAsFE;

			spContent = spRootScrollViewerAsCC.Content;
			spRootBorder = spContent.AsOrNull<IBorder>();
			if (spRootBorder)
			{
				spRootBorderAsFE = spRootBorder.AsOrNull<FrameworkElement>();
				if (spRootBorderAsFE)
				{
					spRootBorderAsFE.Height = windowBounds.Height;
					spRootBorderAsFE.Width = windowBounds.Width;
				}
			}
		}

		spRootScrollViewerAsUE = m_RootScrollViewer.AsOrNull<UIElement>();
		if (spRootScrollViewerAsUE)
		{
			UIElement* pRootScrollViewerNativePeerNoRef = null;
			pRootScrollViewerNativePeerNoRef = (UIElement*)(spRootScrollViewerAsUE.Cast<DirectUI.UIElement>().GetHandle());
			(CoreImports.ScrollContentControl_SetRootScrollViewerSetting(
				(CScrollContentControl*)(pRootScrollViewerNativePeerNoRef),
				0x02 /* RootScrollViewerSetting_ProcessWindowSizeChanged */,
				true /* window size changed */));
			CoreImports.ScrollContentControl_SetRootScrollViewerOriginalHeight((CScrollContentControl*)(pRootScrollViewerNativePeerNoRef), windowBounds.Height);
		}

	Cleanup:
		RRETURN(hr);
	}

	private void OnRootScrollViewerSizeChanged(
		 object pSender,
		 ISizeChangedEventArgs* pArgs)
	{

		UIElement spRootScrollViewerAsUE;

		if (SUCCEEDED(ctl.do_query_interface(spRootScrollViewerAsUE, pSender)))
		{
			bool bIsProcessWindowSizeChanged = false;

			UIElement* pRootScrollViewerNativePeerNoRef = (UIElement*)(spRootScrollViewerAsUE.Cast<DirectUI.UIElement>().GetHandle());

			(CoreImports.ScrollContentControl_GetRootScrollViewerSetting(
					(CScrollContentControl*)(pRootScrollViewerNativePeerNoRef),
					0x02 /* RootScrollViewerSetting_ProcessWindowSizeChanged */,
					bIsProcessWindowSizeChanged));

			if (bIsProcessWindowSizeChanged)
			{
				(CoreImports.ScrollContentControl_SetRootScrollViewerSetting(
						(CScrollContentControl*)(pRootScrollViewerNativePeerNoRef),
						0x02 /* RootScrollViewerSetting_ProcessWindowSizeChanged */,
						false /* window size changed */));
			}
		}
	}
}
