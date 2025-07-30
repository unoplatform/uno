// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RefreshVisualizerEventArgs.cpp, commit 11df1aa

using Uno.Helpers;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides event data for RefreshRequested events.
/// </summary>
public sealed partial class RefreshRequestedEventArgs
{
	private readonly Deferral _deferral;
	private int _deferralCount;

	internal RefreshRequestedEventArgs(Deferral deferral)
	{
		_deferral = deferral;
	}

	/// <summary>
	/// Gets a deferral object for managing the work done in the RefreshRequested event handler.
	/// </summary>
	/// <returns>A deferral object.</returns>
	public Deferral GetDeferral()
	{
		_deferralCount++;

		var deferral = new Deferral(() =>
		{
			// CheckThread();
			DecrementDeferralCount();
		});

		return deferral;
	}

	internal void DecrementDeferralCount()
	{
		MUX_ASSERT(_deferralCount >= 0);
		_deferralCount--;
		if (_deferralCount == 0)
		{
			_deferral.Complete();
		}
	}

	internal void IncrementDeferralCount()
	{
		_deferralCount++;
	}
}
