// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// CCOntrol.cpp, Control_Partial.cpp

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Control
	{
		private protected override void OnUnloaded()
		{
			RemoveFocusEngagement();

			if (IsFocused && this.GetContext() is Uno.UI.Xaml.Core.CoreServices coreServices)
			{
				var focusManager = VisualTree.GetFocusManagerForElement(this);

				if (focusManager != null)
				{
					// Set the focus on the next focusable element.
					// If we remove the currently focused element from the live tree, inside a GettingFocus or LosingFocus handler,
					// we failfast. This is being tracked by Bug 9840123
					focusManager.SetFocusOnNextFocusableElement(FocusState, true);
				}

				UpdateFocusState(FocusState.Unfocused);
			}

			base.OnUnloaded();
		}

		/// <summary>
		/// Set to the UIElement child which has the IsTemplateFocusTarget attached property set to True, if any.
		/// Otherwise it's set to null.
		/// </summary>
		internal UIElement? FocusTargetDescendant => FindFocusTargetDescendant(this); //TODO Uno: This should be set internally when the template is applied.

		private UIElement? FindFocusTargetDescendant(
#if __CROSSRUNTIME__
			// Uno docs: Intentionally passing UIElement as GetChildren(UIElement) is more performant than GetChildren(DependencyObject).
			UIElement? root
#else
			DependencyObject? root
#endif
			)
		{
			if (root == null)
			{
				return null;
			}

			var children = VisualTreeHelper.GetChildren(root);
			foreach (var child in children)
			{
				if (child == null)
				{
					continue;
				}

				if (child.GetValue(IsTemplateFocusTargetProperty) is bool value && value)
				{
					return child as UIElement;
				}

				// Search recursively
				var innerResult = FindFocusTargetDescendant(child);
				if (innerResult != null)
				{
					return innerResult;
				}
			}

			return null;
		}

		/// <summary>
		/// Sets Focus Engagement on a control, if
		///	    1. The control (or one of its descendants) already has focus
		///     2. Control has IsEngagementEnabled set to true
		/// </summary>
		private void SetFocusEngagement()
		{
			var pFocusManager = VisualTree.GetFocusManagerForElement(this);
			if (pFocusManager != null)
			{
				if (IsFocusEngaged)
				{
					bool hasFocusedElement = FocusProperties.HasFocusedElement(this);
					//Check to see if the element or any of it's descendants has focus
					if (!hasFocusedElement)
					{
						IsFocusEngaged = false;
						throw new InvalidOperationException("Can't engage focus when the control nor any of its descendants has focus.");
					}
					if (!IsFocusEngagementEnabled)
					{
						IsFocusEngaged = false;
						throw new InvalidOperationException("Can't engage focus when IsFocusEngagementEnabled is false on the control.");
					}

					//Control is focused and has IsFocusEngagementEnabled set to true
					pFocusManager.EngagedControl = this;
					UpdateEngagementState(true /*engaging*/);
				}
				else if (pFocusManager.EngagedControl != null) //prevents re-entrancy because we set the property to false above in error cases.
				{
					pFocusManager.EngagedControl = null; /*No control is now engaged*/;
					UpdateEngagementState(false /*Disengage*/);

					var popupRoot = VisualTree.GetPopupRootForElement(this);
					popupRoot?.ClearWasOpenedDuringEngagementOnAllOpenPopups();
				}
			}
		}

		/// <summary>
		/// Releases focus from the control boundaries for a control that
		/// has focus engagement (for game pad/remote interaction).
		/// </summary>
		public void RemoveFocusEngagement()
		{
			if (IsFocusEngaged)
			{
				IsFocusEngaged = false;
			}
		}

		/// <summary>
		/// Raise FocusEngaged and FocusDisengaged events and run
		/// default engagement visuals if necessary.
		/// </summary>
		/// <param name="engage">True if the control is engaging.</param>
		private void UpdateEngagementState(bool engage)
		{
			if (engage)
			{
				var focusEngagedEventArgs = new FocusEngagedEventArgs();
				focusEngagedEventArgs.OriginalSource = this;
				focusEngagedEventArgs.Handled = false;
				FocusEngaged?.Invoke(this, focusEngagedEventArgs);
			}
			else
			{
				var focusDisengagedEventArgs = new FocusDisengagedEventArgs();
				focusDisengagedEventArgs.OriginalSource = this;
				FocusDisengaged?.Invoke(this, focusDisengagedEventArgs);
			}
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			var spFocusVisualWhiteDO = GetTemplateChild("FocusVisualWhite");
			var spFocusVisualBlackDO = GetTemplateChild("FocusVisualBlack");
			if (spFocusVisualWhiteDO is Rectangle spFocusVisualWhiteDORect && spFocusVisualBlackDO is Rectangle spFocusVisualBlackDORect)
			{
				LayoutRoundRectangleStrokeThickness(spFocusVisualWhiteDORect);
				LayoutRoundRectangleStrokeThickness(spFocusVisualBlackDORect);
			}
		}

		private protected void LayoutRoundRectangleStrokeThickness(Rectangle pRectangle)
		{
			bool roundStrokeThickness = false;
			double strokeThickness = 0.0f;
			float strokeThicknessFloat = 0.0f;

			roundStrokeThickness = pRectangle.UseLayoutRounding;
			if (roundStrokeThickness)
			{
				strokeThickness = pRectangle.StrokeThickness;
				strokeThicknessFloat = (float)strokeThickness;
				strokeThicknessFloat = (float)LayoutRound(strokeThicknessFloat);
				pRectangle.StrokeThickness = strokeThicknessFloat;
			}
		}

		internal override bool IsFocusableForFocusEngagement() =>
			IsFocusEngagementEnabled && LastInputGamepad();

		private bool LastInputGamepad()
		{
			var contentRoot = VisualTree.GetContentRootForElement(this);
			return contentRoot?.InputManager.LastInputDeviceType == InputDeviceType.GamepadOrRemote;
		}

		private static void ProcessAcceleratorsIfApplicable(KeyRoutedEventArgs spArgsAsKEA, Control pSenderAsControl)
		{
			VirtualKey originalKey = spArgsAsKEA.OriginalKey;
			VirtualKeyModifiers keyModifiers = GetKeyboardModifiers();

			if (KeyboardAcceleratorUtility.IsKeyValidForAccelerators(originalKey, KeyboardAcceleratorUtility.MapVirtualKeyModifiersToIntegersModifiers(keyModifiers)))
			{
				KeyboardAcceleratorUtility.ProcessKeyboardAccelerators(
					originalKey,
					keyModifiers,
					VisualTree.GetContentRootForElement(pSenderAsControl)!.GetAllLiveKeyboardAccelerators(),
					pSenderAsControl,
					out var handled,
					out var handledShouldNotImpedeTextInput,
					null,
					false);

				if (handled)
				{
					spArgsAsKEA.Handled = true;
				}
				if (handledShouldNotImpedeTextInput)
				{
					spArgsAsKEA.HandledShouldNotImpedeTextInput = true;
				}
			}
		}

		private protected static VirtualKeyModifiers GetKeyboardModifiers() => CoreImports.Input_GetKeyboardModifiers();

		internal bool TryGetValueFromBuiltInStyle(DependencyProperty dp, out object? value)
		{
			if (Style.GetDefaultStyleForInstance(this, GetDefaultStyleKey()) is { } style)
			{
				return style.TryGetPropertyValue(dp, out value, this);
			}

			value = null;
			return false;
		}

		private protected void EnsureValidationVisuals()
		{
			// TODO Uno: Not supported yet #4839
		}

		private protected void InvokeValidationCommand(object control, string value)
		{
			// TODO Uno: Not supported yet #4839
		}
	}
}
