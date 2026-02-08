// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// CDependencyObject.h, dependencyobject.cpp

#nullable enable

using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Input;

namespace Uno.UI.Extensions
{
	internal static partial class DependencyObjectExtensions
	{
		internal static Uno.UI.Xaml.Core.CoreServices GetContext(this DependencyObject dependencyObject)
		{
			//TODO Uno: Our implementation currently uses a simplified approach.
			return Uno.UI.Xaml.Core.CoreServices.Instance;
			//return m_sharedState->Value().GetCoreServices();
		}

		internal static DependencyObject? GetParentInternal(this DependencyObject dependencyObject, bool publicParentOnly = true)
		{
			//TODO Uno: Currently we return any parent, regardless of its "publicness".
			return VisualTreeHelper.GetParent(dependencyObject);
			// If the parent is for the inheritance context only the m_pParent field is being repurposed to store
			// a weakref and is invalid.
			// If we're asking for the public parent we don't return a parent if it's marked nonpublic
			//if (m_bitFields.fParentIsInheritanceContextOnly
			//	|| (publicParentOnly && !m_bitFields.fParentIsPublic))
			//{
			//	return nullptr;
			//}
			//else
			//{
			//	return m_pParent;
			//}
		}

		internal static bool SetFocusedElement(
			this DependencyObject sourceElement, // In WinUI this parameter does not exist, as the method is static on DependencyObject
			DependencyObject? pElementToFocus,
			FocusState focusState,
			bool animateIfBringIntoView,
			bool isProcessingTab = false,
			bool isShiftPressed = false,
			bool forceBringIntoView = false)
		{

			FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None;
			if (isProcessingTab)
			{
				if (isShiftPressed)
				{
					focusNavigationDirection = FocusNavigationDirection.Previous;
				}
				else
				{
					focusNavigationDirection = FocusNavigationDirection.Next;
				}
			}

			return sourceElement.SetFocusedElementWithDirection(
				pElementToFocus,
				focusState,
				animateIfBringIntoView,
				focusNavigationDirection,
				forceBringIntoView);
		}


		internal static bool SetFocusedElementWithDirection(
			this DependencyObject sourceElement, // In WinUI this parameter does not exist, as the method is static on DependencyObject
			DependencyObject? pFocusedElement,
			FocusState focusState,
			bool animateIfBringIntoView,
				FocusNavigationDirection focusNavigationDirection,
				bool forceBringIntoView = false,
				Uno.UI.Xaml.Input.InputActivationBehavior inputActivationBehavior = Uno.UI.Xaml.Input.InputActivationBehavior.RequestActivation) // default to request activation to match legacy behavior
		{
			return FocusManager.SetFocusedElementWithDirection(
				pFocusedElement,
				focusState,
				animateIfBringIntoView,
				forceBringIntoView,
				focusNavigationDirection,
				(inputActivationBehavior == Uno.UI.Xaml.Input.InputActivationBehavior.RequestActivation));
		}

		/// <summary>
		/// Gets the focused element.
		/// </summary>
		/// <param name="referenceElement">Reference element.</param>
		/// <returns>Focused element.</returns>
		internal static DependencyObject? GetFocusedElement(this DependencyObject referenceElement)
		{
			var focusManager = VisualTree.GetFocusManagerForElement(referenceElement);
			return focusManager?.FocusedElement;
		}
	}
}
