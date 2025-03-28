// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// focusmgr.h

#nullable enable

using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input
{
	internal struct FindFocusOptions
	{
		public FindFocusOptions(FocusNavigationDirection direction, bool queryOnly)
		{
			Direction = direction;
			QueryOnly = queryOnly;
		}

		public FindFocusOptions(FocusNavigationDirection direction)
		{
			Direction = direction;
			QueryOnly = true;
		}

		public FocusNavigationDirection Direction { get; }

		public bool QueryOnly { get; }
	}
}
