#nullable enable

using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Tests;

/// <summary>The runner's bottom sheet toggle: a button that screen readers see as expand/collapse.</summary>
public sealed partial class ShellSheetHandle : Button
{
	private bool _isExpanded;

	public bool IsExpanded
	{
		get => _isExpanded;
		set
		{
			if (_isExpanded == value)
			{
				return;
			}

			_isExpanded = value;
			if (FrameworkElementAutomationPeer.FromElement(this) is { } peer)
			{
				var (oldState, newState) = value
					? (ExpandCollapseState.Collapsed, ExpandCollapseState.Expanded)
					: (ExpandCollapseState.Expanded, ExpandCollapseState.Collapsed);
				peer.RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, oldState, newState);
			}
		}
	}

	/// <summary>Raised when automation asks to expand (true) or collapse (false).</summary>
	public event EventHandler<bool>? ExpandRequested;

	protected override AutomationPeer OnCreateAutomationPeer() => new ShellSheetHandleAutomationPeer(this);

	private sealed partial class ShellSheetHandleAutomationPeer : ButtonAutomationPeer, IExpandCollapseProvider
	{
		private readonly ShellSheetHandle _owner;

		public ShellSheetHandleAutomationPeer(ShellSheetHandle owner) : base(owner) => _owner = owner;

		public ExpandCollapseState ExpandCollapseState => _owner.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

		public void Expand() => _owner.ExpandRequested?.Invoke(_owner, true);

		public void Collapse() => _owner.ExpandRequested?.Invoke(_owner, false);

		protected override object GetPatternCore(PatternInterface patternInterface)
			=> patternInterface == PatternInterface.ExpandCollapse ? this : base.GetPatternCore(patternInterface);
	}
}
