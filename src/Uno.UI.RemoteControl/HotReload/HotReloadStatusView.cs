using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.HotReload;

internal sealed partial class HotReloadStatusView : Control
{
	private (long id, string state) _currentResult = (-1, "None");

	public HotReloadStatusView()
	{
		DefaultStyleKey = typeof(HotReloadStatusView);
	}

	/// <inheritdoc />
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
	}

	public void Update(HotReloadStatusMessage? status)
	{
		ToolTipService.SetToolTip(this, GetStatusSummary(status));

		if (status is null)
		{
			return;
		}

		VisualStateManager.GoToState(this, GetStatusVisualState(status), true);
		if (GetResultVisualState(status) is { } resultState)
		{
			VisualStateManager.GoToState(this, resultState, true);
		}
	}

	public static string GetStatusSummary(HotReloadStatusMessage? status)
		=> status?.State switch
		{
			HotReloadState.Disabled => "Hot-reload is disabled.",
			HotReloadState.Initializing => "Hot-reload is initializing.",
			HotReloadState.Idle => "Hot-reload server is ready and listening for file changes.",
			HotReloadState.Processing => "Hot-reload server is processing file changes",
			_ => "Unable to determine the state of the hot-reload server."
		};

	private static string GetStatusVisualState(HotReloadStatusMessage status)
		=> status.State switch
		{
			HotReloadState.Disabled => "Disabled",
			HotReloadState.Initializing => "Initializing",
			HotReloadState.Idle => "Idle",
			HotReloadState.Processing => "Processing",
			_ => "Unknown"
		};

	private string? GetResultVisualState(HotReloadStatusMessage status)
	{
		var op = status.Operations.MaxBy(op => op.Id);
		if (op is null)
		{
			return null; // No state change
		}

		var updated = (op.Id, GetStateName(op));
		if (_currentResult == updated)
		{
			return null; // No state change
		}

		_currentResult = updated;
		return _currentResult.state;

		static string GetStateName(HotReloadOperationInfo op)
			=> op.Result switch
			{
				null => "None",
				HotReloadResult.NoChanges => "Success",
				HotReloadResult.Success => "Success",
				_ => "Failed"
			};
	}
}
