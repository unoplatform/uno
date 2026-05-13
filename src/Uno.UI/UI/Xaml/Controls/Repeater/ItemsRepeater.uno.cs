// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Uno-specific additions to ItemsRepeater:
//
// - SerialDisposable revokers for layout and data-source subscriptions that are used in
//   conjunction with weak-reference-based RegisterMeasureInvalidated / RegisterArrangeInvalidated
//   to prevent the long-lived thread-local default StackLayout from keeping ItemsRepeater
//   instances alive across all Uno platforms.
// - The UIElementCollection-backed Children and IPanel implementation.
// - OnLoadedUno / OnUnloadedUno handle the load/unload cycle's re-subscription of data-source
//   and layout events. They also run on Skia with UNO_HAS_ENHANCED_LIFECYCLE defined because
//   the subscription teardown on Unload is still required to avoid leaks through the default
//   StackLayout; the only difference on enhanced-lifecycle is that the OnLoaded re-layout
//   pass is no longer forced.

using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsRepeater : IPanel
{
	private readonly SerialDisposable _layoutSubscriptionsRevoker = new SerialDisposable();
	private readonly SerialDisposable _dataSourceSubscriptionsRevoker = new SerialDisposable();

	// Uno specific: ItemsRepeater hosts its children in a managed collection so that we
	// can intercept changes for cleanup (for example, on Unloaded).
	private readonly UIElementCollection _repeaterChildren;

	UIElementCollection IPanel.Children => _repeaterChildren;

	internal IList<UIElement> Children => _repeaterChildren;

	private void OnLoadedUno()
	{
		// Uno specific: If the control was unloaded but is loaded again, reattach Layout and DataSource events.
		// Use GetEffectiveLayout so that the default StackLayout is also re-subscribed to.
		if (_layoutSubscriptionsRevoker.Disposable is null && GetEffectiveLayout() is { } layout)
		{
			InvalidateMeasure();

			var disposables = new CompositeDisposable();
			layout.RegisterMeasureInvalidated(InvalidateMeasureForLayout).DisposeWith(disposables);
			layout.RegisterArrangeInvalidated(InvalidateArrangeForLayout).DisposeWith(disposables);
			_layoutSubscriptionsRevoker.Disposable = disposables;
		}

		if (_dataSourceSubscriptionsRevoker.Disposable is null && m_itemsSourceView is not null)
		{
			m_itemsSourceView.CollectionChanged += OnItemsSourceViewChanged;
			_dataSourceSubscriptionsRevoker.Disposable = Disposable.Create(() =>
			{
				m_itemsSourceView.CollectionChanged -= OnItemsSourceViewChanged;
			});
		}
	}

	private void OnUnloadedUno()
	{
		// Uno specific: Ensure Layout subscriptions are unattached to avoid memory leaks
		// because ItemsRepeater uses a "singleton" instance of default StackLayout.
		_layoutSubscriptionsRevoker.Disposable = null;
		_dataSourceSubscriptionsRevoker.Disposable = null;
		if (m_itemsSourceView is not null)
		{
			// We will no longer receive the element changes until next load.
			// While add and remove will be detected on next layout pass, a replace would not be reflected in the UI.
			// To fix that, we send a fake reset collection changed in order to mark all containers as recyclable.
			// Note: We do it on unload rather than on load because we want to avoid multiple layout-pass on next load.
			//		 This is expected to only flag containers as recyclable and should not have any significant perf impact.
			OnItemsSourceViewChanged(m_itemsSourceView, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}
	}
}
