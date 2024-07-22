#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Diagnostics.UI;

/// <summary>
/// Interface that a diagnostic element can implement to enhance support for the diagnostic overlay.
/// </summary>
/// <remarks>Type that implements this interface expected to inherit from Control</remarks>
/// <typeparam name="TState">Type of the state exposed by this element.</typeparam>
public interface IDiagnosticViewElement<in TState>
{
	/// <summary>
	/// Update the state of the diagnostic element.
	/// </summary>
	/// <param name="state"></param>
	public void Update(TState state);

	/// <summary>
	/// Request to the element to show its details.
	/// </summary>
	/// <param name="ct">Cancellation token which is cancelled once the details should no longer be visible.</param>
	/// <returns></returns>
	public ValueTask ShowDetails(CancellationToken ct);
}
