// MUX Reference TeachingTipClosingEventArgs.cpp, commit 11df1aa

#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the TeachingTip.Closing event.
/// </summary>
public partial class TeachingTipClosingEventArgs
{
	private readonly object _syncLock = new object();

	private int m_deferralCount;

	internal TeachingTipClosingEventArgs(TeachingTipCloseReason reason) =>
		Reason = reason;

	/// <summary>
	/// Gets or sets a value that indicates whether the Closing event should be canceled.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Gets a constant that specifies whether the cause of the Closing event was due to user
	/// interaction (Close button click), light-dismissal, or programmatic closure.
	/// </summary>
	public TeachingTipCloseReason Reason { get; } = TeachingTipCloseReason.CloseButton;

	internal Deferral? Deferral { get; set; }

	/// <summary>
	/// Gets a deferral object for managing the work done in the Closing event handler.
	/// </summary>
	/// <returns></returns>
	public Deferral GetDeferral()
	{
		IncrementDeferralCount();

		var instance = new Deferral(() =>
		{
			DecrementDeferralCount();
		});

		return instance;
	}

	internal void DecrementDeferralCount()
	{
		if (m_deferralCount <= 0)
		{
			throw new InvalidOperationException("No deferrals to remove");
		}

		lock (_syncLock)
		{
			m_deferralCount--;
			if (m_deferralCount == 0)
			{
				Deferral!.Complete();
			}
		}
	}

	internal void IncrementDeferralCount()
	{
		lock (_syncLock)
		{
			m_deferralCount++;
		}
	}
}
