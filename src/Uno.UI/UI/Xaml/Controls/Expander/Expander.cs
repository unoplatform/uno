// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference Expander.cpp

using Uno.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls;

public partial class Expander : ContentControl
{
	private const string c_expanderHeader = "ExpanderHeader";
	private const string c_expanderContent = "ExpanderContent";
	private const string c_expanderContentClip = "ExpanderContentClip";

	private readonly SerialDisposable m_expanderContentSizeChangedRevoker = new();

	public Expander()
	{
		this.SetDefaultStyleKey();

		SetValue(TemplateSettingsProperty, new ExpanderTemplateSettings());
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new ExpanderAutomationPeer(this);

	protected override void OnApplyTemplate()
	{
		if (GetTemplateChild<Control>(c_expanderHeader) is Control toggleButton)
		{
			// We will do 2 things with the toggle button's peer:
			// 1. Set the events source of the toggle button peer to
			// the expander's automation peer. This is is because we
			// don't want to announce the toggle button's on/off property
			// changes, but the expander's expander/collapse property changes
			// (or on the events source that's set, if it's set) and
			//
			// 2. Set the expander's automation properties name to the
			// toggleButton's in case the expander doesn't have one. This just follows
			// what WPF does.
			if (FrameworkElementAutomationPeer.FromElement(toggleButton) is AutomationPeer toggleButtonPeer)
			{
				// 1. Set the events source of the toggle button peer to the expander's.
				if (FrameworkElementAutomationPeer.FromElement(this) is AutomationPeer expanderPeer)
				{
					var expanderEventsSource = expanderPeer.EventsSource != null ?
						expanderPeer.EventsSource :
						expanderPeer;
					toggleButtonPeer.EventsSource = expanderEventsSource;
				}

				// 2. If the expander doesn't have any AutomationProperties.Name set,
				// we will try setting one based on the header. This is how
				// WPF's expanders work.
				// Uno specific: GetNameCore is protected in C#; GetName forwards to it.
				if (string.IsNullOrEmpty(AutomationProperties.GetName(this))
					&& !string.IsNullOrEmpty(toggleButtonPeer.GetName()))
				{
					AutomationProperties.SetName(this, toggleButtonPeer.GetName());
				}
			}
		}

		if (GetTemplateChild<Border>(c_expanderContentClip) is Border expanderContentClip)
		{
			var visual = ElementCompositionPreview.GetElementVisual(expanderContentClip);
			visual.Clip = visual.Compositor.CreateInsetClip();
		}

		m_expanderContentSizeChangedRevoker.Disposable = null;
		if (GetTemplateChild<Border>(c_expanderContent) is Border expanderContent)
		{
			expanderContent.SizeChanged += OnContentSizeChanged;
			m_expanderContentSizeChangedRevoker.Disposable = Disposable.Create(() => expanderContent.SizeChanged -= OnContentSizeChanged);
		}

		UpdateExpandState(false);
		UpdateExpandDirection(false);
	}

	private void OnContentSizeChanged(object sender, SizeChangedEventArgs args)
	{
		var templateSettings = TemplateSettings;

		var height = args.NewSize.Height;
		templateSettings.ContentHeight = height;
		templateSettings.NegativeContentHeight = -1 * height;
	}

	private void RaiseExpandingEvent(Expander container) =>
		Expanding?.Invoke(this, null);

	private void RaiseCollapsedEvent(Expander container) =>
		Collapsed?.Invoke(this, null);

	private void OnIsExpandedPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var isExpanded = IsExpanded;

		if (isExpanded)
		{
			RaiseExpandingEvent(this);
		}
		else
		{
			// Available for a 'Collapsing' event
		}

		UpdateExpandState(true);

		if (isExpanded)
		{
			// Available for an 'Expanded' event
		}
		else
		{
			RaiseCollapsedEvent(this);
		}
	}

	private void OnExpandDirectionPropertyChanged(DependencyPropertyChangedEventArgs args) =>
		UpdateExpandDirection(true);

	private void UpdateExpandDirection(bool useTransitions)
	{
		var direction = ExpandDirection;

		switch (direction)
		{
			case ExpandDirection.Down:
				VisualStateManager.GoToState(this, "Down", useTransitions);
				break;
			case ExpandDirection.Up:
				VisualStateManager.GoToState(this, "Up", useTransitions);
				break;
		}
	}

	private void UpdateExpandState(bool useTransitions)
	{
		var isExpanded = IsExpanded;
		var direction = ExpandDirection;

		if (isExpanded)
		{
			if (direction == ExpandDirection.Down)
			{
				VisualStateManager.GoToState(this, "ExpandDown", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "ExpandUp", useTransitions);
			}
		}
		else
		{
			if (direction == ExpandDirection.Down)
			{
				VisualStateManager.GoToState(this, "CollapseUp", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "CollapseDown", useTransitions);
			}
		}

		if (FrameworkElementAutomationPeer.FromElement(this) is AutomationPeer peer)
		{
			var expanderPeer = (ExpanderAutomationPeer)peer;
			expanderPeer.RaiseExpandCollapseAutomationEvent(
				isExpanded ?
					ExpandCollapseState.Expanded :
					ExpandCollapseState.Collapsed
			);
		}
	}

}
