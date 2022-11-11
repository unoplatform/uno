// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Uno.Disposables;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class CommandBarFlyout
{
	/// <summary>
	/// Gets or sets a value that indicates whether or not the CommandBarFlyout
	/// should always stay in its Expanded state and block the user from entering
	/// the Collapsed state. Defaults to false.
	/// </summary>
	public bool AlwaysExpanded
	{
		get => m_alwaysExpanded;
		set => m_alwaysExpanded = value;
	}

	/// <summary>
	/// Gets the collection of primary command elements for the CommandBarFlyout.
	/// </summary>
	public IObservableVector<ICommandBarElement> PrimaryCommands { get; }

	/// <summary>
	/// Gets the collection of secondary command elements for the CommandBarFlyout.
	/// </summary>
	public IObservableVector<ICommandBarElement> SecondaryCommands { get; }

	private const int s_commandBarElementDependencyPropertiesCount = 3;
	private const int s_commandBarElementDependencyPropertiesCountRS3 = 2;

	static winrt::DependencyProperty s_appBarButtonDependencyProperties[s_commandBarElementDependencyPropertiesCount];
	static winrt::DependencyProperty s_appBarToggleButtonDependencyProperties[s_commandBarElementDependencyPropertiesCount];

	private bool m_alwaysExpanded;

	private IObservableVector<ICommandBarElement>? m_primaryCommands = null;
	private IObservableVector<ICommandBarElement>? m_secondaryCommands = null;

	// winrt::IObservableVector<winrt::ICommandBarElement>::VectorChanged_revoker throws a weird build error when used,
	// so we're using the event token pattern for manual detachment instead.
	private readonly SerialDisposable m_primaryCommandsVectorChangedToken = new();
	private readonly SerialDisposable m_secondaryCommandsVectorChangedToken = new();

	private readonly SerialDisposable m_commandBarOpenedRevoker = new();
	private readonly SerialDisposable m_commandBarOpeningRevoker = new();
	private readonly SerialDisposable m_commandBarClosedRevoker = new();
	private readonly SerialDisposable m_commandBarClosingRevoker = new();

	std::map<int, winrt::ButtonBase::Click_revoker> m_secondaryButtonClickRevokerByIndexMap;
	std::map<int, winrt::ToggleButton::Checked_revoker> m_secondaryToggleButtonCheckedRevokerByIndexMap;
	std::map<int, winrt::ToggleButton::Unchecked_revoker> m_secondaryToggleButtonUncheckedRevokerByIndexMap;
	std::map<int, PropertyChanged_revoker[s_commandBarElementDependencyPropertiesCount]> m_propertyChangedRevokersByIndexMap;

	private FlyoutPresenter m_presenter { this };

	private bool m_isClosingAfterCloseAnimation = false;
}
