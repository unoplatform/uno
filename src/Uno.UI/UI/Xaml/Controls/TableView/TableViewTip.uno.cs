// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewTip.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// TODO Uno: Telemetry and TiP are Microsoft-only, intentionally no-op. The fault tagging below stays real: exceptions
// keep their app-fault kind through ShapingHelpers. C++ reads the current exception; here the catch passes it in.
internal static partial class TableViewTip
{
	private const int E_FAIL = unchecked((int)0x80004005);

	// An exception's HRESULT and, when app code threw it, which app code (AppFaultKind::None otherwise).
	internal struct Fault
	{
		public Fault()
		{
		}

		public int error = E_FAIL;
		public AppFaultKind appFault = AppFaultKind.None;
	}

	// In a catch: the current exception as a Fault. App code's exceptions carry their kind in their type
	// (RethrowAppFault, or shaping for the app's collection), so whoever catches one can attribute it.
	internal static Fault CurrentFault(Exception error) => new()
	{
		error = error.HResult,
		appFault = (AppFaultKind)ShapingHelpers.CurrentCallerErrorKind(error),
	};

	// In a catch around a call into app code: rethrows the exception as the app's, with the same HRESULT and error
	// info (out of memory stays an OutOfMemoryException). An exception already tagged keeps its kind.
	// Call it as: catch (Exception ex) { TableViewTip.RethrowAppFault(ex, kind); throw; }
	[DoesNotReturn]
	internal static void RethrowAppFault(Exception error, AppFaultKind kind) =>
		ShapingHelpers.RethrowAsCallerError(error, (uint)kind);

	// An app fault leaving a callback that a projected object made (SelectionModel calls TableView back through the
	// ABI). The ABI keeps the exception's error object but drops its C++ type, so the call into that object tags the
	// exception again when its error object is this one; the HRESULT alone never matches.
	// TODO Uno: SelectionModel is managed, so the exception arrives unchanged and is matched by reference.
	internal struct AppFaultHop
	{
		public Exception? error;
		public AppFaultKind kind;
	}

	// In a catch, before the exception leaves the callback: the hop when app code threw it, else empty.
	internal static AppFaultHop CurrentAppFaultHop(Exception error)
	{
		var kind = (AppFaultKind)ShapingHelpers.CurrentCallerErrorKind(error);
		if (kind == AppFaultKind.None)
		{
			return default;
		}

		return new AppFaultHop { error = error, kind = kind };
	}

	// In a catch where the projected call returned: rethrows the exception as the app's again when it is the hop's.
	internal static void RestoreAppFault(AppFaultHop hop, Exception error)
	{
		if (hop.kind != AppFaultKind.None && hop.error is not null && ReferenceEquals(error, hop.error))
		{
			RethrowAppFault(error, hop.kind);
		}
	}

	// TODO Uno: A class because TableViewSource's ShapingTip holds it in a readonly field and mutates it in place.
	internal sealed class ShapingObservation
	{
		public bool completed;
		public bool published;
		public bool unproven;
		public AppFaultKind appFaultKind;
		public int? error;
		public ContainedFaults contained;

		// TableView's own work threw or never ran; app faults are tagged, so they are not failures.
		public bool Failed() => appFaultKind == AppFaultKind.None && (error.HasValue || !completed);

		public IgnoreReason? Ignored()
		{
			if (appFaultKind != AppFaultKind.None)
			{
				return IgnoreReason.ApplicationFault;
			}
			if (!published || unproven)
			{
				return IgnoreReason.DeferredOrUnproven;
			}
			return null;
		}
	}

	// One in-flight tracked operation, and its TiP test when the operation has one.
	// TODO Uno: Never written while TiP is a no-op, and the C++ reference to the telemetry session is dropped.
	internal class OperationState
	{
		public ulong operationGeneration;
		public ulong operationSessionGeneration;
		public Operation operation;
		public bool operationTipPending;
		public bool operationPending;
		public bool operationHandled;
	}

	// The control's tests: initialization, plus the operation test its control and source operations share.
	internal sealed class State : OperationState
	{
		public bool initializationPending;
		public bool initializationTipPending;
		// One verdict per template application; ResetInitialization clears it.
		public bool initializationDone;
	}

	internal static bool IsOperationStarted(OperationState state) => state.operationPending;
	internal static bool IsOperationHandled(OperationState state, Operation operation, ulong generation) => false;
	// Starts Initialize unless it is running or this template application already has its verdict.
	internal static void StartInitialization(State state, MetricsInputs metrics) { }
	internal static void ResetInitialization(State state) { }
	internal static void CompleteInitialization(State state) { }
	internal static void FailInitialization(State state, Stage stage, int error) { }
	internal static void IgnoreInitialization(State state, IgnoreReason reason, int? error = null, AppFaultKind kind = AppFaultKind.None) { }
	// metrics is read only by an operation with a TiP test (OperationTestIdentity); withTest false runs none.
	internal static ulong BeginOperation(OperationState state, Operation operation, MetricsInputs metrics, bool withTest = true) => 0;
	internal static void CompleteOperation(OperationState state, ulong generation) { }
	internal static void IgnoreOperation(OperationState state, ulong generation, IgnoreReason reason, int? error = null, AppFaultKind kind = AppFaultKind.None) { }
	internal static void FailOperation(OperationState state, ulong generation, Stage stage, int? error) { }
}
