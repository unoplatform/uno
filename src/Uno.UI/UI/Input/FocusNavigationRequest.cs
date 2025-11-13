using System;
using Windows.Foundation;

namespace Microsoft.UI.Input;

/// <summary>
/// Provides details for focus navigation events.
/// </summary>
public partial class FocusNavigationRequest
{
	internal FocusNavigationRequest(FocusNavigationReason reason)
	{
		Reason = reason;
	}

	/// <summary>
	/// Gets the unique ID generated when a focus movement event is initiated.
	/// </summary>
	public Guid CorrelationId { get; init; }

	/// <summary>
	/// Gets the reason for a focus navigation event.
	/// </summary>
	public FocusNavigationReason Reason { get; }

	/// <summary>
	/// Gets the bounding rectangle used to identify the focus candidates most likely to receive navigation focus.
	/// </summary>
	public Rect? HintRect { get; init; }

	/// <summary>
	/// Creates an instance of FocusNavigationRequest using the specified FocusNavigationReason.
	/// </summary>
	/// <param name="reason">The reason for a focus navigation event.</param>
	/// <returns>A FocusNavigationRequest object.</returns>
	public static FocusNavigationRequest Create(FocusNavigationReason reason) => new(reason);

	/// <summary>
	/// Creates an instance of FocusNavigationRequest using the specified FocusNavigationReason and hint Rect.
	/// </summary>
	/// <param name="reason">The reason for a focus navigation event.</param>
	/// <param name="hintRect">The bounding rectangle used to identify the focus candidates most likely to receive navigation focus.</param>
	/// <returns>A FocusNavigationRequest object.</returns>
	public static FocusNavigationRequest Create(FocusNavigationReason reason, Rect hintRect) => new(reason)
	{
		HintRect = hintRect
	};

	/// <summary>
	/// Creates an instance of FocusNavigationRequest using the specified FocusNavigationReason, hint Rect, and unique identifier.
	/// </summary>
	/// <param name="reason">The reason for a focus navigation event.</param>
	/// <param name="hintRect">The bounding rectangle used to identify the focus candidates most likely to receive navigation focus.</param>
	/// <param name="correlationId">The unique ID generated when a focus movement event is initiated.</param>
	/// <returns>A FocusNavigationRequest object.</returns>
	public static FocusNavigationRequest Create(FocusNavigationReason reason, Rect hintRect, Guid correlationId) => new(reason)
	{
		HintRect = hintRect,
		CorrelationId = correlationId
	};
}
