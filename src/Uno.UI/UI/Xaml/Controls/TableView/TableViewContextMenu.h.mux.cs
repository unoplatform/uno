// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewContextMenu.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal static partial class TableViewDetails
{
	internal enum ContextMenuTargetKind
	{
		Body,
		Header,
	}

	internal struct ContextMenuTarget
	{
		public ContextMenuTargetKind Kind;
		public TableViewRow? Row;
		public object? Item;
		public TableViewColumn? Column;
		public FrameworkElement? Anchor;
		public Panel? ScopeRoot;
	}

	internal struct ContextMenuCandidate
	{
		public FlyoutBase? Flyout;
		public FrameworkElement? Anchor;

		public static explicit operator bool(ContextMenuCandidate candidate) => candidate.Flyout is not null;
	}

	// True when a header or row cell anchor is still the visible cell for column, directly under
	// its cells host.
	internal static partial bool IsLiveCellAnchor(
		FrameworkElement? anchor,
		Panel? scopeRoot,
		TableViewColumn? column);

	// True when a request's OriginalSource is scope or inside it without crossing a nested
	// TableView or TableViewRow: a request from a nested table belongs to that table, even when it
	// left it Unhandled.
	internal static partial bool IsOwnRequestSource(DependencyObject? source, DependencyObject? scope);

	internal static partial void SuppressContextFlyoutWhileEditing(FlyoutBase flyout, TableView? owner);

	// Requests raised on the owner itself never pass through its template-root routing hook.
	// TODO Uno: A C++ template over the control. TableViewRow and TableViewGroupHeader share no base with
	// GetOwningTableView, so each gets an overload. Assign the result to a SerialDisposable's Disposable.
	internal static IDisposable? HookContextFlyoutSuppression(TableViewRow control) =>
		HookContextFlyoutSuppressionCore(control, static element => ((TableViewRow)element).GetOwningTableView());

	internal static IDisposable? HookContextFlyoutSuppression(TableViewGroupHeader control) =>
		HookContextFlyoutSuppressionCore(control, static element => ((TableViewGroupHeader)element).GetOwningTableView());

	private static IDisposable? HookContextFlyoutSuppressionCore(FrameworkElement control, Func<FrameworkElement, TableView?> getOwningTableView)
	{
		if (control.ContextFlyout is { } flyout)
		{
			WeakReference<FrameworkElement> weakThis = new(control);
			void OnOpening(object? sender, object args)
			{
				if (weakThis.Get() is { } strongThis)
				{
					var openingFlyout = (FlyoutBase)sender!;
					if (openingFlyout.Target == strongThis)
					{
						SuppressContextFlyoutWhileEditing(openingFlyout, getOwningTableView(strongThis));
					}
				}
			}

			flyout.Opening += OnOpening;
			return Disposable.Create(() => flyout.Opening -= OnOpening);
		}
		return null;
	}

	// Hooks ContextRequested on control's template root. The framework shows the control's own
	// ContextFlyout before the control's handlers run, so suppression must sit below it.
	internal static IDisposable? HookTemplateRootContextRequested(
		DependencyObject control,
		TypedEventHandler<UIElement, ContextRequestedEventArgs> handler)
	{
		if (VisualTreeHelper.GetChildrenCount(control) > 0)
		{
			if (VisualTreeHelper.GetChild(control, 0) is UIElement templateRoot)
			{
				templateRoot.ContextRequested += handler;
				return Disposable.Create(() => templateRoot.ContextRequested -= handler);
			}
		}
		return null;
	}
}
