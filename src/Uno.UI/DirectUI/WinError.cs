// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Windows Kits\10\Include\10.0.22621.0\shared\winerror.h

#nullable enable

using System;

namespace DirectUI;

internal static class WinError
{
	internal static bool SUCCEEDED(Action action)
	{
		if (action is null)
		{
			throw new ArgumentNullException(nameof(action));
		}

		try
		{
			action.Invoke();
			return true;
		}
		catch
		{
			return false;
		}
	}
}
