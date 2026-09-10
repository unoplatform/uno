// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// WinSDK Reference windows.ui.shell.tasks.idl, Windows SDK 10.0.26100.7705, commit 1bfb76d

#pragma warning disable CS8305

using Windows.Foundation.Metadata;

namespace Windows.UI.Shell.Tasks;

/// <summary>
/// Defines constants that specify the state of the app task.
/// </summary>
[ContractVersion(typeof(AppTaskContract), 65536U)]
[Experimental]
public enum AppTaskState
{
	/// <summary>
	/// The task is actively executing.
	/// </summary>
	Running = 0,

	/// <summary>
	/// The task has finished execution successfully.
	/// </summary>
	Completed = 1,

	/// <summary>
	/// The task needs user input to continue.
	/// </summary>
	NeedsAttention = 2,

	/// <summary>
	/// The task execution is suspended but can be resumed without user intervention.
	/// </summary>
	Paused = 3,

	/// <summary>
	/// The task completed with an error state.
	/// </summary>
	Error = 4,
}
