// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\AppBar_Partial.h, tag winui3/release/1.7.1, commit 5f27a786ac96c

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBar
{
	public AppBar()
	{
		m_Mode = AppBarMode.Inline;
		m_onLoadFocusState = FocusState.Unfocused;
		m_savedFocusState = FocusState.Unfocused;
		m_isInOverlayState = false;
		m_isChangingOpenedState = false;
		m_hasUpdatedTemplateSettings = false;
		m_minCompactHeight = 0d;
		m_compactHeight = 0d;
		m_minimalHeight = 0d;
		m_openedWithExpandButton = false;
		m_contentHeight = 0d;
		m_isOverlayVisible = false;

		DefaultStyleKey = typeof(AppBar);

#if HAS_UNO // Prepare state is not called automatically yet.
		PrepareState();
#endif
	}

	// TODO:MZ: Cleanup to unloaded
	//~AppBar()
	//{
	//	auto xamlRoot = XamlRoot::GetForElementStatic(this);
	//	if (m_xamlRootChangedEventHandler && xamlRoot)
	//	{
	//		VERIFYHR(m_xamlRootChangedEventHandler.DetachEventHandler(xamlRoot.Get()));
	//	}

	//	if (DXamlCore::GetCurrent() != nullptr)
	//	{
	//		VERIFYHR(BackButtonIntegration_UnregisterListener(this));
	//	}
	//}

	protected virtual void PrepareState()
	{
		//base.PrepareState();

		Loaded += OnLoaded;
		m_loadedEventHandler.Disposable = Disposable.Create(() => Loaded -= OnLoaded);
		Unloaded += OnUnloaded;
		m_unloadedEventHandler.Disposable = Disposable.Create(() => Unloaded -= OnUnloaded);
		SizeChanged += OnSizeChanged;
		m_sizeChangedEventHandler.Disposable = Disposable.Create(() => SizeChanged -= OnSizeChanged);

		TemplateSettings = new AppBarTemplateSettings();
	}

	// Note that we need to wait for OnLoaded event to set focus.
	// When we get the on opened event children of AppBar will not be populated
	// yet which will prevent them from getting focus.
	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (m_layoutUpdatedEventHandler.Disposable is null)
		{
			this.LayoutUpdated += OnLayoutUpdated;
			m_layoutUpdatedEventHandler.Disposable = Disposable.Create(() => LayoutUpdated -= OnLayoutUpdated);
		}

		//register for XamlRoot.Changed events
		var xamlRoot = XamlRoot.GetForElement(this);
		if (m_xamlRootChangedEventHandler.Disposable is null && xamlRoot is not null)
		{
			xamlRoot.Changed += OnXamlRootChanged;
			m_xamlRootChangedEventHandler.Disposable = Disposable.Create(() => xamlRoot.Changed -= OnXamlRootChanged);
		}

		// register the app bar if it is floating
		if (m_Mode == AppBarMode.Floating)
		{
			var xamlRootImpl = xamlRoot;
			var applicationBarService = xamlRootImpl.GetApplicationBarService();
			MUX_ASSERT(applicationBarService is not null);
			applicationBarService.RegisterApplicationBar(this, m_Mode);
		}

		// If it's a top or bottom bar, make sure the bounds are correct if we haven't set them yet
		if (m_Mode == AppBarMode.Top || m_Mode == AppBarMode.Bottom)
		{
			var xamlRootImpl = xamlRoot;
			var applicationBarService = xamlRootImpl.GetApplicationBarService();
			MUX_ASSERT(applicationBarService is not null);
			applicationBarService.OnBoundsChanged();
		}

		// OnIsOpenChanged handles focus and other changes
		bool isOpen = IsOpen;
		if (isOpen)
		{
			OnIsOpenChanged(true);
		}

		// Update the visual state to make sure our calculations for what
		// direction to open in are correct.
		UpdateVisualState();
	}

	private void OnUnloaded(object sender, RoutedEventArgs args)
	{
		if (m_layoutUpdatedEventHandler.Disposable is not null)
		{
			m_layoutUpdatedEventHandler.Disposable = null;
		}

		if (m_isInOverlayState)
		{
			TeardownOverlayState();
		}

		if (m_Mode == AppBarMode.Floating)
		{
			if (XamlRoot.GetImplementationForElement(this) is { } xamlRoot)
			{
				var applicationBarService = xamlRoot.GetApplicationBarService(applicationBarService);
				applicationBarService.UnregisterApplicationBar(this);
			}
		}

		// Make sure we're not still registered for back button events when no longer
		// in the tree.
		BackButtonIntegration.UnregisterListener(this);
	}

	private void OnLayoutUpdated(object sender, object args)
	{
		if (m_layoutTransitionElement is not null)
		{
			PositionLTEs();
		}
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		RefreshContentHeight(null /*didChange*/);
		UpdateTemplateSettings();

		Page pageOwner;
		if (SUCCEEDED(GetOwner(&pageOwner)) && pageOwner)
		{
			pageOwner.AppBarClosedSizeChanged();
		}
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		if (args.Property == IsOpenProperty)
		{
			bool isOpen = (bool)args.NewValue;
			OnIsOpenChanged(isOpen);

			if (EventEnabledAppBarOpenBegin() && isOpen)
			{
				TraceAppBarOpenBegin((uint)(m_Mode));
			}
			if (EventEnabledAppBarClosedBegin() && !isOpen)
			{
				TraceAppBarClosedBegin((uint)(m_Mode));
			}

			OnIsOpenChangedForAutomation(args);

			if (EventEnabledAppBarOpenEnd() && isOpen)
			{
				TraceAppBarOpenEnd();
			}
			if (EventEnabledAppBarClosedEnd() && !isOpen)
			{
				TraceAppBarClosedEnd();
			}

			UpdateVisualState();
		}
		else if (args.Property == IsStickyProperty)
		{
			OnIsStickyChanged();
		}
		else if (args.Property == ClosedDisplayModeProperty)
		{
			if (m_Mode != AppBarMode.Inline)
			{
				if (XamlRoot.GetImplementationForElement(this) is { } xamlRoot)
				{
					var applicationBarService = xamlRoot.GetApplicationBarService(applicationBarService);
					applicationBarService.HandleApplicationBarClosedDisplayModeChange(this, m_Mode);
				}
			}

			InvalidateMeasure();
			UpdateVisualState();
		}
		else if (args.Property == LightDismissOverlayModeProperty)
		{
			ReevaluateIsOverlayVisible();
		}
		else if (args.Property == IsEnabledProperty)
		{
			UpdateVisualState();
		}
	}

	private protected override void OnVisibilityChanged()
	{
		Page pageOwner;
		if (SUCCEEDED(GetOwner(&pageOwner)) && pageOwner)
		{
			pageOwner.AppBarClosedSizeChanged();
		}
	}

	protected override void OnApplyTemplate()
	{
		//TODO:MZ
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var returnValue = base.MeasureOverride(availableSize);

#if HAS_UNO
		if (_isNativeTemplate)
		{
			return returnValue;
		}
#endif

		if (m_Mode == AppBarMode.Top || m_Mode == AppBarMode.Bottom)
		{
			// regardless of what we desire, settings of alignment or fixed size content, we will always take up full width
			returnValue.Width = availableSize.Width;
		}

		// Make sure our returned height matches the configured state.
		var closedDisplayMode = ClosedDisplayMode;

		switch (closedDisplayMode)
		{
			case AppBarClosedDisplayMode.Compact:
				{
					double oldCompactHeight = CompactHeight;

					bool hasRightLabelDynamicPrimaryCommand = HasRightLabelDynamicPrimaryCommand;
					bool hasNonLabeledDynamicPrimaryCommand = HasNonLabeledDynamicPrimaryCommand;

					if (hasRightLabelDynamicPrimaryCommand || hasNonLabeledDynamicPrimaryCommand)
					{
						bool isOpen = IsOpen;
						if (!isOpen)
						{
							CompactHeight = Math.Max(MinCompactHeight, returnValue.Height);
						}
					}
					else
					{
						CompactHeight = MinCompactHeight;
					}

					double newCompactHeight = CompactHeight;

					if (oldCompactHeight != newCompactHeight)
					{
						UpdateTemplateSettings();
					}

					returnValue.Height = newCompactHeight;
					break;
				}
			case AppBarClosedDisplayMode.Minimal:
				{
					returnValue.Height = MinimalHeight;
					break;
				}
			default:
			case AppBarClosedDisplayMode.Hidden:
				{
					returnValue.Height = 0.0;
					break;
				}
		}

		return returnValue;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var arrangeSize = finalSize;
		var layoutRootDesiredSize = new Size();
		if (m_tpLayoutRoot is { })
		{
			layoutRootDesiredSize = m_tpLayoutRoot.DesiredSize;
		}
		else
		{
			layoutRootDesiredSize = arrangeSize;
		}

		var returnValue = base.ArrangeOverride(new Size(finalSize.Width, layoutRootDesiredSize.Height));

		returnValue.Height = arrangeSize.Height;

		return baseSize;
	}
}
