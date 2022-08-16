using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Hosting;

/// <summary>
/// Provides information about a request to give focus to a DesktopWindowXamlSource object.
/// </summary>
public partial class XamlSourceFocusNavigationRequest
{
	/// <summary>
	/// Initializes a new instance of the XamlSourceFocusNavigationRequest
	/// class with the reason for the navigation request.
	/// </summary>
	/// <param name="reason">A value that indicates the reason for the navigation request.</param>
	public XamlSourceFocusNavigationRequest(XamlSourceFocusNavigationReason reason)
	{
		Reason = reason;
	}

	/// <summary>
	/// Initializes a new instance of the XamlSourceFocusNavigationRequest class
	/// with the reason for the navigation request and the bounding rectangle
	/// that will receive navigation focus.
	/// </summary>
	/// <param name="reason">A value that indicates the reason for the navigation request.</param>
	/// <param name="hintRect">The bounding rectangle of the element in the desktop application that is losing focus.</param>
	public XamlSourceFocusNavigationRequest(XamlSourceFocusNavigationReason reason, Rect hintRect)
	{
		Reason = reason;
		HintRect = hintRect;
	}

	/// <summary>
	/// Initializes a new instance of the XamlSourceFocusNavigationRequest class
	/// with the reason for the navigation request, the bounding rectangle that
	/// will receive navigation focus, and the unique correlation ID for the request.
	/// </summary>
	/// <param name="reason">A value that indicates the reason for the navigation request.</param>
	/// <param name="hintRect">The bounding rectangle of the element in the desktop application that is losing focus.</param>
	/// <param name="correlationId">The unique identifier for the navigation request. You can use this parameter
	/// for logging purposes, or if you have an existing correlation ID from an in-progress focus movement
	/// already in progress and you want to connect that focus movement with the current navigation request.</param>
	public XamlSourceFocusNavigationRequest(XamlSourceFocusNavigationReason reason, Rect hintRect, Guid correlationId)
	{
		Reason = reason;
		HintRect = hintRect;
		CorrelationId = correlationId;
	}

	/// <summary>
	/// Gets the unique identifier for the navigation request. You can use this value for logging purposes,
	/// or if you have an existing correlation ID from an in-progress focus movement already in progress
	/// and you want to connect that focus movement with a new navigation request.
	/// </summary>
	public XamlSourceFocusNavigationReason Reason { get; }

	/// <summary>
	/// Gets the bounding rectangle of the element in the desktop application that is losing focus
	/// (that is, the element that had focus before the DesktopWindowXamlSource received focus).
	/// </summary>
	public Rect HintRect { get; }

	/// <summary>
	/// Gets the unique identifier for the navigation request.
	/// You can use this value for logging purposes, or if you
	/// have an existing correlation ID from an in-progress focus movement
	/// already in progress and you want to connect that focus movement
	/// with a new navigation request.
	/// </summary>
	public Guid CorrelationId { get; }
}
