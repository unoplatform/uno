#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.Windows.Apps.Test.Automation;
using Microsoft.Windows.Apps.Test.Automation.Text;
using Microsoft.Windows.Apps.Test.Foundation.Waiters;
using SupportedTextSelection = Microsoft.UI.Xaml.Automation.SupportedTextSelection;
using WindowInteractionState = Microsoft.UI.Xaml.Automation.WindowInteractionState;
using WindowVisualState = Microsoft.UI.Xaml.Automation.WindowVisualState;
using XamlExpandCollapseState = Microsoft.UI.Xaml.Automation.ExpandCollapseState;
using XamlScrollAmount = Microsoft.UI.Xaml.Automation.ScrollAmount;
using XamlToggleState = Microsoft.UI.Xaml.Automation.ToggleState;

namespace Microsoft.Windows.Apps.Test.Foundation.Patterns;

public interface IInvoke
{
	void Invoke();

	UIEventWaiter GetInvokedWaiter();
}

public interface IToggle
{
	ToggleState ToggleState { get; }

	void Toggle();

	UIEventWaiter GetToggledWaiter();
}

public interface IExpandCollapse
{
	ExpandCollapseState ExpandCollapseState { get; }

	void Expand();

	void Collapse();

	UIEventWaiter GetExpandedWaiter();

	UIEventWaiter GetCollapsedWaiter();
}

public interface IValue
{
	string Value { get; }

	bool IsReadOnly { get; }

	void SetValue(string value);
}

public interface IRangeValue
{
	double Minimum { get; }

	double Maximum { get; }

	double LargeChange { get; }

	double SmallChange { get; }

	double Value { get; }

	bool IsReadOnly { get; }

	void SetValue(double value);
}

public interface ISelectionItem<C>
{
	bool IsSelected { get; }

	C SelectionContainer { get; }

	void Select();

	void AddToSelection();

	void RemoveFromSelection();

	UIEventWaiter GetSelectedWaiter();

	UIEventWaiter GetAddedToSelectionWaiter();

	UIEventWaiter GetRemovedFromSelectionWaiter();
}

public interface ISelection<I>
	where I : UIObject
{
	UICollection<I> Selection { get; }

	bool CanSelectMultiple { get; }

	bool IsSelectionRequired { get; }
}

public interface IScroll
{
	bool HorizontallyScrollable { get; }

	bool VerticallyScrollable { get; }

	double HorizontalScrollPercent { get; }

	double VerticalScrollPercent { get; }

	double HorizontalViewSize { get; }

	double VerticalViewSize { get; }

	void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount);

	void ScrollHorizontal(ScrollAmount amount);

	void ScrollVertical(ScrollAmount amount);

	void SetScrollPercent(double horizontalPercent, double verticalPercent);
}

public interface IScrollItem
{
	void ScrollIntoView();
}

public interface IText
{
	bool SupportsTextSelection { get; }

	TextPatternRange DocumentRange { get; }

	TextPatternRange GetSelection();

	TextPatternRange GetVisibleRange();

	TextPatternRange RangeFromChild(UIObject childElement);
}

public class PatternImplementation<TProvider>
	where TProvider : class
{
	private readonly PatternInterface _patternInterface;

	public PatternImplementation(UIObject uiObject, PatternInterface patternInterface)
	{
		UIObject = uiObject;
		_patternInterface = patternInterface;
	}

	protected UIObject UIObject { get; }

	public bool IsAvailable => UIObject.GetPattern(_patternInterface) is TProvider;

	protected TProvider Provider
		=> UIObject.GetPattern(_patternInterface) as TProvider
			?? throw new PatternNotFoundException($"{UIObject} does not support the {_patternInterface} pattern.");
}

public class InvokeImplementation : PatternImplementation<IInvokeProvider>, IInvoke
{
	public InvokeImplementation(UIObject uiObject) : base(uiObject, PatternInterface.Invoke)
	{
	}

	public void Invoke() => Provider.Invoke();

	public UIEventWaiter GetInvokedWaiter() => new InvokedEventWaiter(UIObject);
}

public class ToggleImplementation : PatternImplementation<IToggleProvider>, IToggle
{
	public ToggleImplementation(UIObject uiObject) : base(uiObject, PatternInterface.Toggle)
	{
	}

	public ToggleState ToggleState => Provider.ToggleState switch
	{
		XamlToggleState.On => ToggleState.On,
		XamlToggleState.Indeterminate => ToggleState.Indeterminate,
		_ => ToggleState.Off,
	};

	public void Toggle() => Provider.Toggle();

	public UIEventWaiter GetToggledWaiter() => new PropertyChangedEventWaiter(UIObject, Scope.Element, UIProperty.Get("Toggle.ToggleState"));
}

public class ExpandCollapseImplementation : PatternImplementation<IExpandCollapseProvider>, IExpandCollapse
{
	public ExpandCollapseImplementation(UIObject uiObject) : base(uiObject, PatternInterface.ExpandCollapse)
	{
	}

	public ExpandCollapseState ExpandCollapseState => Provider.ExpandCollapseState switch
	{
		XamlExpandCollapseState.Expanded => ExpandCollapseState.Expanded,
		XamlExpandCollapseState.PartiallyExpanded => ExpandCollapseState.PartiallyExpanded,
		XamlExpandCollapseState.LeafNode => ExpandCollapseState.LeafNode,
		_ => ExpandCollapseState.Collapsed,
	};

	public void Expand() => Provider.Expand();

	public void Collapse() => Provider.Collapse();

	public UIEventWaiter GetExpandedWaiter() => new ConditionWaiter(UIObject, () => ExpandCollapseState == ExpandCollapseState.Expanded, "Expanded");

	public UIEventWaiter GetCollapsedWaiter() => new ConditionWaiter(UIObject, () => ExpandCollapseState == ExpandCollapseState.Collapsed, "Collapsed");
}

public class ValueImplementation : PatternImplementation<IValueProvider>, IValue
{
	public ValueImplementation(UIObject uiObject) : base(uiObject, PatternInterface.Value)
	{
	}

	public string Value => Provider.Value ?? string.Empty;

	public bool IsReadOnly => Provider.IsReadOnly;

	public void SetValue(string value) => Provider.SetValue(value);
}

public class RangeValueImplementation : PatternImplementation<IRangeValueProvider>, IRangeValue
{
	public RangeValueImplementation(UIObject uiObject) : base(uiObject, PatternInterface.RangeValue)
	{
	}

	public double Minimum => Provider.Minimum;

	public double Maximum => Provider.Maximum;

	public double LargeChange => Provider.LargeChange;

	public double SmallChange => Provider.SmallChange;

	public double Value => Provider.Value;

	public bool IsReadOnly => Provider.IsReadOnly;

	public void SetValue(double value) => Provider.SetValue(value);
}

public class SelectionItemImplementation<C> : PatternImplementation<ISelectionItemProvider>, ISelectionItem<C>
	where C : UIObject
{
	private readonly IFactory<C> _factory;

	public SelectionItemImplementation(UIObject uiObject, IFactory<C> factory) : base(uiObject, PatternInterface.SelectionItem)
		=> _factory = factory;

	public bool IsSelected => Provider.IsSelected;

	public C SelectionContainer
		=> UIObject.FromProvider(Provider.SelectionContainer) is { } container
			? _factory.Create(container)
			: throw new UIObjectNotFoundException($"{UIObject} has no selection container.");

	public void Select() => Provider.Select();

	public void AddToSelection() => Provider.AddToSelection();

	public void RemoveFromSelection() => Provider.RemoveFromSelection();

	public UIEventWaiter GetSelectedWaiter() => new ConditionWaiter(UIObject, () => IsSelected, "Selected");

	public UIEventWaiter GetAddedToSelectionWaiter() => GetSelectedWaiter();

	public UIEventWaiter GetRemovedFromSelectionWaiter() => new ConditionWaiter(UIObject, () => !IsSelected, "RemovedFromSelection");
}

public class SelectionImplementation<I> : PatternImplementation<ISelectionProvider>, ISelection<I>
	where I : UIObject
{
	private readonly IFactory<I> _factory;

	public SelectionImplementation(UIObject uiObject, IFactory<I> factory) : base(uiObject, PatternInterface.Selection)
		=> _factory = factory;

	public UICollection<I> Selection
		=> new((Provider.GetSelection() ?? Array.Empty<IRawElementProviderSimple>())
			.Select(provider => UIObject.FromProvider(provider))
			.Where(item => item is not null)
			.Select(item => _factory.Create(item!)));

	public bool CanSelectMultiple => Provider.CanSelectMultiple;

	public bool IsSelectionRequired => Provider.IsSelectionRequired;
}

public class ScrollImplementation : PatternImplementation<IScrollProvider>, IScroll
{
	public ScrollImplementation(UIObject uiObject) : base(uiObject, PatternInterface.Scroll)
	{
	}

	public bool HorizontallyScrollable => Provider.HorizontallyScrollable;

	public bool VerticallyScrollable => Provider.VerticallyScrollable;

	public double HorizontalScrollPercent => Provider.HorizontalScrollPercent;

	public double VerticalScrollPercent => Provider.VerticalScrollPercent;

	public double HorizontalViewSize => Provider.HorizontalViewSize;

	public double VerticalViewSize => Provider.VerticalViewSize;

	public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount) => Provider.Scroll(ToXaml(horizontalAmount), ToXaml(verticalAmount));

	public void ScrollHorizontal(ScrollAmount amount) => Scroll(amount, ScrollAmount.NoAmount);

	public void ScrollVertical(ScrollAmount amount) => Scroll(ScrollAmount.NoAmount, amount);

	public void SetScrollPercent(double horizontalPercent, double verticalPercent) => Provider.SetScrollPercent(horizontalPercent, verticalPercent);

	private static XamlScrollAmount ToXaml(ScrollAmount amount) => amount switch
	{
		ScrollAmount.LargeDecrement => XamlScrollAmount.LargeDecrement,
		ScrollAmount.SmallDecrement => XamlScrollAmount.SmallDecrement,
		ScrollAmount.LargeIncrement => XamlScrollAmount.LargeIncrement,
		ScrollAmount.SmallIncrement => XamlScrollAmount.SmallIncrement,
		_ => XamlScrollAmount.NoAmount,
	};
}

public class ScrollItemImplementation : PatternImplementation<IScrollItemProvider>, IScrollItem
{
	public ScrollItemImplementation(UIObject uiObject) : base(uiObject, PatternInterface.ScrollItem)
	{
	}

	public void ScrollIntoView() => Provider.ScrollIntoView();
}

public class TextImplementation : PatternImplementation<ITextProvider>, IText
{
	public TextImplementation(UIObject uiObject) : base(uiObject, PatternInterface.Text)
	{
	}

	public bool SupportsTextSelection => Provider.SupportedTextSelection != SupportedTextSelection.None;

	public TextPatternRange DocumentRange => new(Provider.DocumentRange);

	public TextPatternRange GetSelection() => new(Provider.GetSelection()?.FirstOrDefault());

	public TextPatternRange GetVisibleRange() => new(Provider.GetVisibleRanges()?.FirstOrDefault());

	public TextPatternRange RangeFromChild(UIObject childElement)
		=> throw new NotSupportedException("TextPattern.RangeFromChild is not supported by the in-process MITA shim.");
}

public class WindowImplementation : PatternImplementation<IWindowProvider>
{
	public WindowImplementation(UIObject uiObject) : base(uiObject, PatternInterface.Window)
	{
	}

	public bool CanMaximize => Provider.Maximizable;

	public bool CanMinimize => Provider.Minimizable;

	public bool IsModal => Provider.IsModal;

	public bool IsTopmost => Provider.IsTopmost;

	public WindowVisualState WindowVisualState => Provider.VisualState;

	public WindowInteractionState WindowInteractionState => Provider.InteractionState;

	public void Close() => Provider.Close();

	public void SetWindowVisualState(WindowVisualState state) => Provider.SetVisualState(state);
}
