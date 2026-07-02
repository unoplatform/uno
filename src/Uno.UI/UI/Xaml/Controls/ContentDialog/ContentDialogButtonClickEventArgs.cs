using System;
using Uno.Helpers;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the button click event.
/// </summary>
public partial class ContentDialogButtonClickEventArgs
{
	internal ContentDialogButtonClickEventArgs(Action<ContentDialogButtonClickEventArgs> complete)
	{
		DeferralManager = new(h => new ContentDialogButtonClickDeferral(h));
		DeferralManager.Completed += (s, e) => complete(this);
	}

	/// <summary>
	/// Gets or sets a value that can cancel the button click.
	/// A true value for Cancel cancels the default behavior.
	/// </summary>
	public bool Cancel { get; set; }

	internal DeferralManager<ContentDialogButtonClickDeferral> DeferralManager { get; }

	/// <summary>
	/// Gets a ContentDialogButtonClickDeferral that the app can use to
	/// respond asynchronously to the button click event.
	/// </summary>
	/// <returns>A deferral object.</returns>
	public ContentDialogButtonClickDeferral GetDeferral() => DeferralManager.GetDeferral();
}
