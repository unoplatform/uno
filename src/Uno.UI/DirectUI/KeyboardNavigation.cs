// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//  Abstract:
//      Provides logical and directional navigation between focusable objects.

using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DirectUI
{
	internal class KeyboardNavigation
	{
		// Given a key, returns the appropriate navigation action.
		public static void TranslateKeyToKeyNavigationAction(
			FlowDirection flowDirection,
			VirtualKey key,
			out KeyNavigationAction pNavAction,
			out bool pIsValidKey)
		{
			pIsValidKey = true;

			pNavAction = KeyNavigationAction.Up;

			bool bInvertForRTL = (flowDirection == FlowDirection.RightToLeft);

			switch (key)
			{
				case VirtualKey.PageUp:
					pNavAction = KeyNavigationAction.Previous;
					break;

				case VirtualKey.PageDown:
					pNavAction = KeyNavigationAction.Next;
					break;

				case VirtualKey.Down:
					pNavAction = KeyNavigationAction.Down;
					break;

				case VirtualKey.Up:
					pNavAction = KeyNavigationAction.Up;
					break;

				case VirtualKey.Left:
					pNavAction = (bInvertForRTL
						? KeyNavigationAction.Right
						: KeyNavigationAction.Left);
					break;

				case VirtualKey.Right:
					pNavAction = (bInvertForRTL
						? KeyNavigationAction.Left
						: KeyNavigationAction.Right);
					break;

				case VirtualKey.Home:
					pNavAction = KeyNavigationAction.First;
					break;

				case VirtualKey.End:
					pNavAction = KeyNavigationAction.Last;
					break;

				default:
					pIsValidKey = false;
					break;
			}
		}
	}
}
