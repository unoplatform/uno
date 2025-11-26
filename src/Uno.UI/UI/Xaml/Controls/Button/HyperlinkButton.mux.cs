// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// HyperlinkButton_Partial.h, HyperlinkButton_Partial.cpp

#nullable enable

using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class HyperlinkButton : ButtonBase
	{
		private const string ContentPresenterName = "ContentPresenter";
		//private const string ContentPresenterLegacyName = "Text";
		private const string HyperlinkUnderlineVisibleKey = "HyperlinkUnderlineVisible";

		private protected override void Initialize()
		{
			base.Initialize();

			// TODO Uno: Set cursor properly
			// SetCursor(MouseCursorHand)
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			if (args.Property == ContentProperty ||
				args.Property == ContentTemplateProperty)
			{
				// We need to call InvalidateMeasure() to cover the case
				// where the new content has the same size as the old content,
				// to make sure MeasureOverride() is called again and the
				// text content is underlined.
				InvalidateMeasure();
			}
		}

		/// <summary>
		/// Change to the correct visual state.
		/// </summary>
		/// <param name="useTransitions">Use transitions.</param>
		private protected override void ChangeVisualState(bool useTransitions)
		{
			var isEnabled = IsEnabled;
			var isPressed = IsPressed;
			var isPointerOver = IsPointerOver;
			var focusState = FocusState;

			// Update the Interaction state group
			if (!isEnabled)
			{
				GoToState(useTransitions, "Disabled");
			}
			else if (isPressed)
			{
				GoToState(useTransitions, "Pressed");
			}
			else if (isPointerOver)
			{
				GoToState(useTransitions, "PointerOver");
			}
			else
			{
				GoToState(useTransitions, "Normal");
			}

			// Update the Focus group
			if (focusState != FocusState.Unfocused && isEnabled)
			{
				if (focusState == FocusState.Pointer)
				{
					GoToState(useTransitions, "PointerFocused");
				}
				else
				{
					GoToState(useTransitions, "Focused");
				}
			}
			else
			{
				GoToState(useTransitions, "Unfocused");
			}
		}

		protected override AutomationPeer OnCreateAutomationPeer() => new HyperlinkButtonAutomationPeer(this);

		protected override Size MeasureOverride(Size availableSize)
		{
			var size = base.MeasureOverride(availableSize);

			UnderlineContentPresenterText();

			// We don't want to override the foreground when BackPlate is active to allow hyperlink color changes.
			SetHyperlinkForegroundOverrideForBackPlate();

			return size;
		}

		private protected override async void OnClick()
		{
			var hasAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.InvokePatternOnInvoked);
			if (hasAutomationListener)
			{
				var automationPeer = GetOrCreateAutomationPeer();
				if (automationPeer != null)
				{
					automationPeer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
				}
			}

			base.OnClick();

			// If NavigateUri is set, launch it in external app.
			var uri = NavigateUri;
			if (uri != null)
			{
				await Launcher.LaunchUriAsync(uri);
			}
		}

		private void UnderlineContentPresenterText()
		{
			ContentPresenter? contentPresenter = null;

			var contentPresenterPart = GetTemplateChild(ContentPresenterName);

			if (contentPresenterPart != null)
			{
				var contentPresenterPartDO = contentPresenterPart;
				if (contentPresenterPartDO != null)
				{
					contentPresenter = contentPresenterPartDO as ContentPresenter;
				}

				// If the ContentPresenter in the default template is present, and the TextBlock is one which is
				// generated in ContentPresenter code behind as default, only then do we proceed with the underline.
				if (contentPresenter != null && contentPresenter.IsUsingDefaultTemplate)
				{
					// TODO Uno: Using ContentPresenter.ContentTemplateRoot instead of this.ContentTemplateRoot, which is null.
					var contentTemplateRootAsIUIE = contentPresenter.ContentTemplateRoot;
					var contentTemplateRootAsITextBlock = contentTemplateRootAsIUIE as TextBlock;
					if (contentTemplateRootAsITextBlock != null)
					{
						// Only apply underline if HighContrast is enabled OR HyperlinkUnderlineVisible is true
						if (ShouldUnderlineHyperlink())
						{
							contentTemplateRootAsITextBlock.TextDecorations = TextDecorations.Underline;
						}
					}
				}
			}
		}

		private bool ShouldUnderlineHyperlink()
		{
			// Check if high contrast is enabled
			var accessibilitySettings = new AccessibilitySettings();
			if (accessibilitySettings.HighContrast)
			{
				return true;
			}

			// Check if HyperlinkUnderlineVisible resource is set to true
			if (Application.Current?.Resources.TryGetValue(HyperlinkUnderlineVisibleKey, out var underlineVisible) == true
				&& underlineVisible is bool boolValue
				&& boolValue)
			{
				return true;
			}

			return false;
		}

		private void SetHyperlinkForegroundOverrideForBackPlate()
		{
			// TODO Uno: BackPlate on TextBlock is not supported, terminate early

#if false
			var contentPresenterPart = GetTemplateChild(ContentPresenterName);

			// If HyperLinkButton is not using the current template try to get the ContentPresenter by its name in TextBlockButtonStyle.
			// TextBlockButtonStyle isn't normally used with HyperlinkButton. Today it is sometimes used to remove the underline on the HyperlinkButton.
			// The style originally existed just to support some of the VS templates that shipped with Win8.
			if (contentPresenterPart == null)
			{
				contentPresenterPart = GetTemplateChild(ContentPresenterLegacyName);
			}

			if (contentPresenterPart != null)
			{
				if (contentPresenterPart is ContentPresenter contentPresenter)
				{
					SetHyperlinkForegroundOverrideForBackPlateRecursive(contentPresenter);
				}
			}
#endif
		}

		//private void SetHyperlinkForegroundOverrideForBackPlateRecursive(UIElement pElement)
		//{
		//	var pChildren = pElement.GetChildren();
		//	if (pChildren != null)
		//	{
		//		foreach (var child in pChildren)
		//		{
		//			var childUIE = child;
		//			if (childUIE != null)
		//			{
		//				var childTextBlock = childUIE as TextBlock;
		//				if (childTextBlock != null)
		//				{
		//					childTextBlock.SetUseHyperlinkForegroundOnBackPlate(true);
		//				}
		//				else
		//				{
		//					SetHyperlinkForegroundOverrideForBackPlateRecursive(childUIE);
		//				}
		//			}
		//		}
		//	}
		//}
	}
}
