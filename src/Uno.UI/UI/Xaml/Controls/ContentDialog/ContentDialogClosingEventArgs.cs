using System;
using Uno.Helpers;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data for the closing event.
/// </summary>
public partial class ContentDialogClosingEventArgs
{
	internal ContentDialogClosingEventArgs(Action<ContentDialogClosingEventArgs> complete, ContentDialogResult result)
	{
		DeferralManager = new(h => new ContentDialogClosingDeferral(h));
		DeferralManager.Completed += (s, e) => complete(this);

		Result = result;
	}

	/// <summary>
	/// Gets or sets a value that can cancel the closing of the dialog.
	/// A true value for Cancel cancels the default behavior.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Gets the ContentDialogResult of the closing event.
	/// </summary>
	public ContentDialogResult Result { get; }

	internal DeferralManager<ContentDialogClosingDeferral> DeferralManager { get; }

	/// <summary>
	/// Gets a ContentDialogClosingDeferral that the app
	/// can use to respond asynchronously to the closing event.
	/// </summary>
	/// <returns>Deferral</returns>
	public ContentDialogClosingDeferral GetDeferral() => DeferralManager.GetDeferral();
}
