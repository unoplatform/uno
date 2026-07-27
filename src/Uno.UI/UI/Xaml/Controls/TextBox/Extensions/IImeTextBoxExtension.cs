#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls.Extensions;

internal interface IImeTextBoxExtension
{
	/// <summary>
	/// Called when the host control gains focus. The platform should prepare
	/// IME context for the given host (a <see cref="TextBox"/> or RichEditBox).
	/// </summary>
	void StartImeSession(IImeSessionHost host, ImeSessionActivation activation);

	/// <summary>
	/// Called when the TextBox loses focus. The platform should clean up
	/// IME context. Any active composition should be committed or cancelled.
	/// </summary>
	void EndImeSession();

	/// <summary>
	/// Applies properties that changed while the host owns the active session.
	/// </summary>
	void UpdateImeSession(IImeSessionHost host, ImeSessionUpdate update);

	/// <summary>
	/// Gets the active conversion or prediction candidates in platform order.
	/// </summary>
	Task<IReadOnlyList<string>> GetLinguisticAlternativesAsync(string compositionText, CancellationToken cancellationToken);

	/// <summary>
	/// Gets whether an IME composition is currently active.
	/// </summary>
	bool IsComposing { get; }

	/// <summary>
	/// Raised when the user begins an IME composition.
	/// </summary>
	event EventHandler? CompositionStarted;

	/// <summary>
	/// Raised when the IME composition string changes.
	/// </summary>
	event EventHandler<ImeCompositionEventArgs>? CompositionUpdated;

	/// <summary>
	/// Raised when the user commits text from the IME.
	/// </summary>
	event EventHandler<ImeCompositionEventArgs>? CompositionCompleted;

	/// <summary>
	/// Raised when the IME commits a prefix and continues composing the remaining span.
	/// </summary>
	event EventHandler<ImePartialCompositionEventArgs>? CompositionPartiallyCommitted;

	/// <summary>
	/// Raised when the IME cancels the current transient preedit.
	/// </summary>
	event EventHandler<ImeCompositionEventArgs>? CompositionCanceled;

	/// <summary>
	/// Raised when the IME composition session ends (after commit or cancel).
	/// </summary>
	event EventHandler? CompositionEnded;

	/// <summary>
	/// Raised when the platform reports candidate-window bounds in active-host coordinates.
	/// </summary>
	event EventHandler<ImeCandidateWindowBoundsChangedEventArgs>? CandidateWindowBoundsChanged;
}
