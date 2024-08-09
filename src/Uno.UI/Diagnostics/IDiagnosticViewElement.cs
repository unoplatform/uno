#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Diagnostics.UI;

/// <summary>
/// Defines an interface for diagnostic elements to enhance support for the diagnostic overlay.
/// </summary>
/// <remarks>
/// Implementing types are expected to inherit from the <see cref="Control"/> class.
/// </remarks>
/// <typeparam name="TState">The type of state exposed by this element.</typeparam>
public interface IDiagnosticViewElement<in TState>
{
	/// <summary>
	/// Update the state of the diagnostic element.
	/// </summary>
	/// <param name="state">The new state to be applied to the element.</param>
	public void Update(TState state);

	/// <summary>
	/// Requests the element to display its details.
	/// </summary>
	/// <param name="ct">A cancellation token that is canceled when the details should no longer be visible.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public ValueTask ShowDetails(CancellationToken ct);
}
