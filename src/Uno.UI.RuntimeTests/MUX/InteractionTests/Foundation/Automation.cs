#nullable enable

using System;
using XamlAutomationControlType = Microsoft.UI.Xaml.Automation.Peers.AutomationControlType;

namespace Microsoft.Windows.Apps.Test.Automation;

public enum ToggleState
{
	Off,
	On,
	Indeterminate,
}

public enum ExpandCollapseState
{
	Collapsed,
	Expanded,
	LeafNode,
	PartiallyExpanded,
}

public enum ScrollAmount
{
	LargeDecrement,
	SmallDecrement,
	NoAmount,
	LargeIncrement,
	SmallIncrement,
}

public enum RowOrColumnMajor
{
	RowMajor,
	ColumnMajor,
	Indeterminate,
}

public enum OrientationType
{
	None,
	Horizontal,
	Vertical,
}

public sealed class ControlType : IEquatable<ControlType>
{
	private ControlType(XamlAutomationControlType value) => Value = value;

	internal XamlAutomationControlType Value { get; }

	public string ProgrammaticName => "ControlType." + Value;

	public static readonly ControlType Button = new(XamlAutomationControlType.Button);
	public static readonly ControlType Calendar = new(XamlAutomationControlType.Calendar);
	public static readonly ControlType CheckBox = new(XamlAutomationControlType.CheckBox);
	public static readonly ControlType ComboBox = new(XamlAutomationControlType.ComboBox);
	public static readonly ControlType Edit = new(XamlAutomationControlType.Edit);
	public static readonly ControlType Hyperlink = new(XamlAutomationControlType.Hyperlink);
	public static readonly ControlType Image = new(XamlAutomationControlType.Image);
	public static readonly ControlType ListItem = new(XamlAutomationControlType.ListItem);
	public static readonly ControlType List = new(XamlAutomationControlType.List);
	public static readonly ControlType Menu = new(XamlAutomationControlType.Menu);
	public static readonly ControlType MenuBar = new(XamlAutomationControlType.MenuBar);
	public static readonly ControlType MenuItem = new(XamlAutomationControlType.MenuItem);
	public static readonly ControlType ProgressBar = new(XamlAutomationControlType.ProgressBar);
	public static readonly ControlType RadioButton = new(XamlAutomationControlType.RadioButton);
	public static readonly ControlType ScrollBar = new(XamlAutomationControlType.ScrollBar);
	public static readonly ControlType Slider = new(XamlAutomationControlType.Slider);
	public static readonly ControlType Spinner = new(XamlAutomationControlType.Spinner);
	public static readonly ControlType StatusBar = new(XamlAutomationControlType.StatusBar);
	public static readonly ControlType Tab = new(XamlAutomationControlType.Tab);
	public static readonly ControlType TabItem = new(XamlAutomationControlType.TabItem);
	public static readonly ControlType Text = new(XamlAutomationControlType.Text);
	public static readonly ControlType ToolBar = new(XamlAutomationControlType.ToolBar);
	public static readonly ControlType ToolTip = new(XamlAutomationControlType.ToolTip);
	public static readonly ControlType Tree = new(XamlAutomationControlType.Tree);
	public static readonly ControlType TreeItem = new(XamlAutomationControlType.TreeItem);
	public static readonly ControlType Custom = new(XamlAutomationControlType.Custom);
	public static readonly ControlType Group = new(XamlAutomationControlType.Group);
	public static readonly ControlType Thumb = new(XamlAutomationControlType.Thumb);
	public static readonly ControlType DataGrid = new(XamlAutomationControlType.DataGrid);
	public static readonly ControlType DataItem = new(XamlAutomationControlType.DataItem);
	public static readonly ControlType Document = new(XamlAutomationControlType.Document);
	public static readonly ControlType SplitButton = new(XamlAutomationControlType.SplitButton);
	public static readonly ControlType Window = new(XamlAutomationControlType.Window);
	public static readonly ControlType Pane = new(XamlAutomationControlType.Pane);
	public static readonly ControlType Header = new(XamlAutomationControlType.Header);
	public static readonly ControlType HeaderItem = new(XamlAutomationControlType.HeaderItem);
	public static readonly ControlType Table = new(XamlAutomationControlType.Table);
	public static readonly ControlType TitleBar = new(XamlAutomationControlType.TitleBar);
	public static readonly ControlType Separator = new(XamlAutomationControlType.Separator);
	public static readonly ControlType SemanticZoom = new(XamlAutomationControlType.SemanticZoom);
	public static readonly ControlType AppBar = new(XamlAutomationControlType.AppBar);

	internal static ControlType From(XamlAutomationControlType value) => new(value);

	public bool Equals(ControlType? other) => other is not null && other.Value == Value;

	public override bool Equals(object? obj) => obj is ControlType other && Equals(other);

	public override int GetHashCode() => Value.GetHashCode();

	public override string ToString() => ProgrammaticName;

	public static bool operator ==(ControlType? left, ControlType? right) => left is null ? right is null : left.Equals(right);

	public static bool operator !=(ControlType? left, ControlType? right) => !(left == right);
}

public class ElementNotAvailableException : Exception
{
	public ElementNotAvailableException()
	{
	}

	public ElementNotAvailableException(string message) : base(message)
	{
	}
}
