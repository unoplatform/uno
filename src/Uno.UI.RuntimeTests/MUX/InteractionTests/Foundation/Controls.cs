#nullable enable

using System;
using System.Linq;
using Microsoft.Windows.Apps.Test.Automation;
using Microsoft.Windows.Apps.Test.Automation.Text;
using Microsoft.Windows.Apps.Test.Foundation.Patterns;
using Microsoft.Windows.Apps.Test.Foundation.Waiters;

namespace Microsoft.Windows.Apps.Test.Foundation.Controls;

internal sealed class DelegateFactory<T> : IFactory<T>
{
	private readonly Func<UIObject, T> _create;

	public DelegateFactory(Func<UIObject, T> create) => _create = create;

	public T Create(UIObject element) => _create(element);
}

public class Button : UIObject, IInvoke
{
	private readonly InvokeImplementation _invoke;

	public Button(UIObject uiObject) : base(uiObject) => _invoke = new(this);

	public static new IFactory<Button> Factory { get; } = new DelegateFactory<Button>(o => new Button(o));

	public void Invoke() => _invoke.Invoke();

	public UIEventWaiter GetInvokedWaiter() => _invoke.GetInvokedWaiter();
}

public class Hyperlink : UIObject, IInvoke
{
	private readonly InvokeImplementation _invoke;

	public Hyperlink(UIObject uiObject) : base(uiObject) => _invoke = new(this);

	public static new IFactory<Hyperlink> Factory { get; } = new DelegateFactory<Hyperlink>(o => new Hyperlink(o));

	public void Invoke() => _invoke.Invoke();

	public UIEventWaiter GetInvokedWaiter() => _invoke.GetInvokedWaiter();
}

public class ToggleButton : UIObject, IToggle
{
	private readonly ToggleImplementation _toggle;

	public ToggleButton(UIObject uiObject) : base(uiObject) => _toggle = new(this);

	public static new IFactory<ToggleButton> Factory { get; } = new DelegateFactory<ToggleButton>(o => new ToggleButton(o));

	public ToggleState ToggleState => _toggle.ToggleState;

	public void Toggle() => _toggle.Toggle();

	public UIEventWaiter GetToggledWaiter() => _toggle.GetToggledWaiter();

	public void Check() => ToggleTo(ToggleState.On);

	public void Uncheck() => ToggleTo(ToggleState.Off);

	private protected void ToggleTo(ToggleState state)
	{
		// Bounded: a three-state toggle needs at most two toggles to reach any state.
		for (var i = 0; i < 3 && ToggleState != state; i++)
		{
			Toggle();
		}
	}
}

public class CheckBox : ToggleButton
{
	public CheckBox(UIObject uiObject) : base(uiObject)
	{
	}

	public static new IFactory<CheckBox> Factory { get; } = new DelegateFactory<CheckBox>(o => new CheckBox(o));
}

public class ToggleSwitch : ToggleButton
{
	public ToggleSwitch(UIObject uiObject) : base(uiObject)
	{
	}

	public static new IFactory<ToggleSwitch> Factory { get; } = new DelegateFactory<ToggleSwitch>(o => new ToggleSwitch(o));

	public void TurnOn() => ToggleTo(ToggleState.On);

	public void TurnOff() => ToggleTo(ToggleState.Off);
}

public class RadioButton : UIObject, ISelectionItem<UIObject>
{
	private readonly SelectionItemImplementation<UIObject> _selectionItem;

	public RadioButton(UIObject uiObject) : base(uiObject) => _selectionItem = new(this, UIObject.Factory);

	public static new IFactory<RadioButton> Factory { get; } = new DelegateFactory<RadioButton>(o => new RadioButton(o));

	public bool IsSelected => _selectionItem.IsSelected;

	public UIObject SelectionContainer => _selectionItem.SelectionContainer;

	public void Select() => _selectionItem.Select();

	public void AddToSelection() => _selectionItem.AddToSelection();

	public void RemoveFromSelection() => _selectionItem.RemoveFromSelection();

	public UIEventWaiter GetSelectedWaiter() => _selectionItem.GetSelectedWaiter();

	public UIEventWaiter GetAddedToSelectionWaiter() => _selectionItem.GetAddedToSelectionWaiter();

	public UIEventWaiter GetRemovedFromSelectionWaiter() => _selectionItem.GetRemovedFromSelectionWaiter();
}

public class TextBlock : UIObject, IText
{
	private readonly TextImplementation _text;

	public TextBlock(UIObject uiObject) : base(uiObject) => _text = new(this);

	public static new IFactory<TextBlock> Factory { get; } = new DelegateFactory<TextBlock>(o => new TextBlock(o));

	public string DocumentText => DocumentRange.GetText(-1);

	public bool SupportsTextSelection => _text.SupportsTextSelection;

	public TextPatternRange DocumentRange => _text.DocumentRange;

	public TextPatternRange GetSelection() => _text.GetSelection();

	public TextPatternRange GetVisibleRange() => _text.GetVisibleRange();

	public TextPatternRange RangeFromChild(UIObject childElement) => _text.RangeFromChild(childElement);
}

public class Edit : UIObject, IText, IValue
{
	private readonly TextImplementation _text;
	private readonly ValueImplementation _value;

	public Edit(UIObject uiObject) : base(uiObject)
	{
		_text = new(this);
		_value = new(this);
	}

	public static new IFactory<Edit> Factory { get; } = new DelegateFactory<Edit>(o => new Edit(o));

	public string Value => _value.Value;

	public bool IsReadOnly => _value.IsReadOnly;

	public void SetValue(string value) => _value.SetValue(value);

	public bool SupportsTextSelection => _text.SupportsTextSelection;

	public TextPatternRange DocumentRange => _text.DocumentRange;

	public TextPatternRange GetSelection() => _text.GetSelection();

	public TextPatternRange GetVisibleRange() => _text.GetVisibleRange();

	public TextPatternRange RangeFromChild(UIObject childElement) => _text.RangeFromChild(childElement);
}

public class RangeValueSlider : UIObject, IRangeValue
{
	private readonly RangeValueImplementation _rangeValue;

	public RangeValueSlider(UIObject uiObject) : base(uiObject) => _rangeValue = new(this);

	public static new IFactory<RangeValueSlider> Factory { get; } = new DelegateFactory<RangeValueSlider>(o => new RangeValueSlider(o));

	public double Minimum => _rangeValue.Minimum;

	public double Maximum => _rangeValue.Maximum;

	public double LargeChange => _rangeValue.LargeChange;

	public double SmallChange => _rangeValue.SmallChange;

	public double Value => _rangeValue.Value;

	public bool IsReadOnly => _rangeValue.IsReadOnly;

	public void SetValue(double value) => _rangeValue.SetValue(value);
}

public class ListItem<C> : UIObject, ISelectionItem<C>, IScrollItem
	where C : UIObject
{
	private readonly SelectionItemImplementation<C> _selectionItem;
	private readonly ScrollItemImplementation _scrollItem;

	protected ListItem(UIObject uiObject, IFactory<C> containerFactory) : base(uiObject)
	{
		_selectionItem = new(this, containerFactory);
		_scrollItem = new(this);
	}

	public bool IsSelected => _selectionItem.IsSelected;

	public C SelectionContainer => _selectionItem.SelectionContainer;

	public void Select() => _selectionItem.Select();

	public void AddToSelection() => _selectionItem.AddToSelection();

	public void RemoveFromSelection() => _selectionItem.RemoveFromSelection();

	public UIEventWaiter GetSelectedWaiter() => _selectionItem.GetSelectedWaiter();

	public UIEventWaiter GetAddedToSelectionWaiter() => _selectionItem.GetAddedToSelectionWaiter();

	public UIEventWaiter GetRemovedFromSelectionWaiter() => _selectionItem.GetRemovedFromSelectionWaiter();

	public void ScrollIntoView() => _scrollItem.ScrollIntoView();
}

public class ItemsContainer<I> : UIObject, ISelection<I>
	where I : UIObject
{
	private readonly SelectionImplementation<I> _selection;
	private readonly IFactory<I> _itemFactory;

	protected ItemsContainer(UIObject uiObject, IFactory<I> itemFactory) : base(uiObject)
	{
		_itemFactory = itemFactory;
		_selection = new(this, itemFactory);
	}

	public UICollection<I> AllItems => new(Children.Where(IsItem).Select(_itemFactory.Create));

	public UICollection<I> Items => AllItems;

	public UICollection<I> Selection => _selection.Selection;

	public bool CanSelectMultiple => _selection.CanSelectMultiple;

	public bool IsSelectionRequired => _selection.IsSelectionRequired;

	private protected virtual bool IsItem(UIObject child) => child.ControlType == ControlType.ListItem || child.ControlType == ControlType.DataItem;
}

public class ComboBoxItem : ListItem<ComboBox>
{
	public ComboBoxItem(UIObject uiObject) : base(uiObject, ComboBox.Factory)
	{
	}

	public static new IFactory<ComboBoxItem> Factory { get; } = new DelegateFactory<ComboBoxItem>(o => new ComboBoxItem(o));
}

public class ComboBox : ItemsContainer<ComboBoxItem>, IExpandCollapse, IValue
{
	private readonly ExpandCollapseImplementation _expandCollapse;
	private readonly ValueImplementation _value;

	public ComboBox(UIObject uiObject) : base(uiObject, ComboBoxItem.Factory)
	{
		_expandCollapse = new(this);
		_value = new(this);
	}

	public static new IFactory<ComboBox> Factory { get; } = new DelegateFactory<ComboBox>(o => new ComboBox(o));

	public ExpandCollapseState ExpandCollapseState => _expandCollapse.ExpandCollapseState;

	public void Expand() => _expandCollapse.Expand();

	public void Collapse() => _expandCollapse.Collapse();

	public UIEventWaiter GetExpandedWaiter() => _expandCollapse.GetExpandedWaiter();

	public UIEventWaiter GetCollapsedWaiter() => _expandCollapse.GetCollapsedWaiter();

	public string Value => _value.IsAvailable ? _value.Value : Selection.FirstOrDefault()?.Name ?? string.Empty;

	public bool IsReadOnly => !_value.IsAvailable || _value.IsReadOnly;

	public void SetValue(string value) => _value.SetValue(value);
}

public class ListViewItem : ListItem<ListView>
{
	public ListViewItem(UIObject uiObject) : base(uiObject, ListView.Factory)
	{
	}

	public static new IFactory<ListViewItem> Factory { get; } = new DelegateFactory<ListViewItem>(o => new ListViewItem(o));
}

public class ListView : ItemsContainer<ListViewItem>
{
	public ListView(UIObject uiObject) : base(uiObject, ListViewItem.Factory)
	{
	}

	public static new IFactory<ListView> Factory { get; } = new DelegateFactory<ListView>(o => new ListView(o));
}

public class SplitButton : UIObject, IInvoke, IExpandCollapse
{
	private readonly InvokeImplementation _invoke;
	private readonly ExpandCollapseImplementation _expandCollapse;

	public SplitButton(UIObject uiObject) : base(uiObject)
	{
		_invoke = new(this);
		_expandCollapse = new(this);
	}

	public static new IFactory<SplitButton> Factory { get; } = new DelegateFactory<SplitButton>(o => new SplitButton(o));

	public void Invoke() => _invoke.Invoke();

	public UIEventWaiter GetInvokedWaiter() => _invoke.GetInvokedWaiter();

	public ExpandCollapseState ExpandCollapseState => _expandCollapse.ExpandCollapseState;

	public void Expand() => _expandCollapse.Expand();

	public void Collapse() => _expandCollapse.Collapse();

	public UIEventWaiter GetExpandedWaiter() => _expandCollapse.GetExpandedWaiter();

	public UIEventWaiter GetCollapsedWaiter() => _expandCollapse.GetCollapsedWaiter();
}

public class MenuItem : UIObject, IInvoke, IExpandCollapse
{
	private readonly InvokeImplementation _invoke;
	private readonly ExpandCollapseImplementation _expandCollapse;

	public MenuItem(UIObject uiObject) : base(uiObject)
	{
		_invoke = new(this);
		_expandCollapse = new(this);
	}

	public static new IFactory<MenuItem> Factory { get; } = new DelegateFactory<MenuItem>(o => new MenuItem(o));

	public void Invoke() => _invoke.Invoke();

	public UIEventWaiter GetInvokedWaiter() => _invoke.GetInvokedWaiter();

	public ExpandCollapseState ExpandCollapseState => _expandCollapse.ExpandCollapseState;

	public void Expand() => _expandCollapse.Expand();

	public void Collapse() => _expandCollapse.Collapse();

	public UIEventWaiter GetExpandedWaiter() => _expandCollapse.GetExpandedWaiter();

	public UIEventWaiter GetCollapsedWaiter() => _expandCollapse.GetCollapsedWaiter();
}

public class TreeItem : UIObject, IExpandCollapse, ISelectionItem<UIObject>, IScrollItem
{
	private readonly ExpandCollapseImplementation _expandCollapse;
	private readonly SelectionItemImplementation<UIObject> _selectionItem;
	private readonly ScrollItemImplementation _scrollItem;

	public TreeItem(UIObject uiObject) : base(uiObject)
	{
		_expandCollapse = new(this);
		_selectionItem = new(this, UIObject.Factory);
		_scrollItem = new(this);
	}

	public static new IFactory<TreeItem> Factory { get; } = new DelegateFactory<TreeItem>(o => new TreeItem(o));

	public ExpandCollapseState ExpandCollapseState => _expandCollapse.ExpandCollapseState;

	public void Expand() => _expandCollapse.Expand();

	public void Collapse() => _expandCollapse.Collapse();

	public UIEventWaiter GetExpandedWaiter() => _expandCollapse.GetExpandedWaiter();

	public UIEventWaiter GetCollapsedWaiter() => _expandCollapse.GetCollapsedWaiter();

	public bool IsSelected => _selectionItem.IsSelected;

	public UIObject SelectionContainer => _selectionItem.SelectionContainer;

	public void Select() => _selectionItem.Select();

	public void AddToSelection() => _selectionItem.AddToSelection();

	public void RemoveFromSelection() => _selectionItem.RemoveFromSelection();

	public UIEventWaiter GetSelectedWaiter() => _selectionItem.GetSelectedWaiter();

	public UIEventWaiter GetAddedToSelectionWaiter() => _selectionItem.GetAddedToSelectionWaiter();

	public UIEventWaiter GetRemovedFromSelectionWaiter() => _selectionItem.GetRemovedFromSelectionWaiter();

	public void ScrollIntoView() => _scrollItem.ScrollIntoView();
}
