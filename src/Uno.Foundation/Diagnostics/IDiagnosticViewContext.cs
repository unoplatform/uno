#nullable enable
using System;
using System.Linq;

namespace Uno.Diagnostics.UI;

/// <summary>
/// Some information on the context where the diagnostic view is being rendered.
/// </summary>
/// <remarks>This is mainly an abstraction of the dispatcher that ensure to not overload the UI thread for diagnostics.</remarks>
public interface IDiagnosticViewContext
{
	/// <summary>
	/// Schedule an update of the diagnostic.
	/// </summary>
	/// <param name="action">Update action.</param>
	void Schedule(Action action);

	void ScheduleRecurrent(Action action);

	void AbortRecurrent(Action action);

	void Notify(DiagnosticViewNotification notification);
}
