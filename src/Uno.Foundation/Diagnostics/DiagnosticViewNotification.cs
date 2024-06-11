#nullable enable
using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Diagnostics.UI;

/// <summary>
/// A notification sent by a diagnostic view to visually show an important status.
/// </summary>
/// <param name="content">The content of the notification.</param>
/// <param name="duration">Configures the duration to wait before automatically hide the notification.</param>
public class DiagnosticViewNotification(object content, TimeSpan? duration = null)
{
	[EditorBrowsable(EditorBrowsableState.Never)] // For XAML only
	public DiagnosticViewNotification() : this(default!)
	{
	}

	/// <summary>
	/// The content of the notification.
	/// </summary>
	public object Content { get; set; } = content;

	/// <summary>
	/// Configures the duration to wait before automatically hide the notification.
	/// </summary>
	public TimeSpan? Duration { get; set; } = duration;
}
