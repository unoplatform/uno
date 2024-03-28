// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SelectorItemAutomationPeer_Partial.cpp, tag winui3/release/1.4.2
namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes the items in a Selector to Microsoft UI Automation.
/// </summary>
public partial class SelectorItemAutomationPeer : ItemAutomationPeer, Provider.ISelectionItemProvider
{
	public SelectorItemAutomationPeer(object item, SelectorAutomationPeer parent) : base(item, parent)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		var spPatternProvider = base.GetPatternCore(patternInterface);

		if (spPatternProvider is not { })
		{
			return spPatternProvider;
		}

		if (patternInterface == PatternInterface.SelectionItem)
		{
			bool selectionPatternApplicable = true;

			if (Parent)
			{
				(Owner
			}
		}
	}

	/// <summary>
	/// Clears any existing selection and then selects the current element.
	/// </summary>
	public void Select()
	{

	}

	/// <summary>
	/// Adds the current element to the collection of selected items.
	/// </summary>
	public void AddToSelection()
	{

	}

	/// <summary>
	/// Removes the current element from the collection of selected items.
	/// </summary>
	public void RemoveFromSelection()
	{

	}

	/// <summary>
	/// Gets a value that indicates whether an item is selected.
	/// </summary>
	public bool IsSelected
	{
		get
		{

		}
	}

	/// <summary>
	/// Gets the UI Automation provider that implements ISelectionProvider and acts as container for the calling object.
	/// </summary>
	public Provider.IRawElementProviderSimple SelectionContainer
	{
		get
		{

		}
	}

	internal void ScrollIntoView()
	{

	}
}
