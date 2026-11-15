#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Windows.Apps.Test.Foundation.Patterns;

namespace Microsoft.Windows.Apps.Test.Foundation;

public sealed class UIProperty
{
	private static readonly Dictionary<string, UIProperty> _properties = new(StringComparer.Ordinal);

	private readonly Func<UIObject, object?> _getter;

	static UIProperty()
	{
		Register("AutomationId", o => o.AutomationId);
		Register("Name", o => o.Name);
		Register("ClassName", o => o.ClassName);
		Register("ControlType", o => o.ControlType);
		Register("HasKeyboardFocus", o => o.HasKeyboardFocus);
		Register("IsKeyboardFocusable", o => o.IsKeyboardFocusable);
		Register("IsEnabled", o => o.IsEnabled);
		Register("IsOffscreen", o => o.IsOffscreen);
		Register("BoundingRectangle", o => o.BoundingRectangle);
		Register("HelpText", o => o.HelpText);
		Register("ItemStatus", o => o.ItemStatus);
		Register("LocalizedControlType", o => o.LocalizedControlType);
		Register("Toggle.ToggleState", o => new ToggleImplementation(o).ToggleState);
		Register("ExpandCollapse.ExpandCollapseState", o => new ExpandCollapseImplementation(o).ExpandCollapseState);
		Register("Value.Value", o => new ValueImplementation(o).Value);
		Register("Value.IsReadOnly", o => new ValueImplementation(o).IsReadOnly);
		Register("RangeValue.Value", o => new RangeValueImplementation(o).Value);
		Register("SelectionItem.IsSelected", o => new SelectionItemImplementation<UIObject>(o, UIObject.Factory).IsSelected);
		Register("Scroll.HorizontalScrollPercent", o => new ScrollImplementation(o).HorizontalScrollPercent);
		Register("Scroll.VerticalScrollPercent", o => new ScrollImplementation(o).VerticalScrollPercent);
	}

	private UIProperty(string name, Func<UIObject, object?> getter)
	{
		Name = name;
		_getter = getter;
	}

	public static IEnumerable<UIProperty> AllProperties => _properties.Values;

	public string Name { get; }

	public static UIProperty Get(string name)
		=> _properties.TryGetValue(name, out var property)
			? property
			: throw new NotSupportedException($"UIProperty '{name}' is not supported by the in-process MITA shim.");

	public static bool Exists(string name) => _properties.ContainsKey(name);

	internal object? GetValue(UIObject uiObject) => _getter(uiObject);

	public override string ToString() => Name;

	private static void Register(string name, Func<UIObject, object?> getter) => _properties[name] = new UIProperty(name, getter);
}

public sealed class UICondition
{
	private readonly Func<UIObject, bool> _predicate;
	private readonly string _description;

	private UICondition(Func<UIObject, bool> predicate, string description)
	{
		_predicate = predicate;
		_description = description;
	}

	public static UICondition True { get; } = new(_ => true, "True");

	public static UICondition False { get; } = new(_ => false, "False");

	public static UICondition RawTree => True;

	public static UICondition ControlTree { get; } = new(o => o.IsControlElement, "ControlTree");

	public static UICondition ContentTree { get; } = new(o => o.IsContentElement, "ContentTree");

	public static UICondition Create(UIProperty property, object value)
		=> new(o => Equals(property.GetValue(o), value), $"{property.Name} = '{value}'");

	public static UICondition CreateFromId(string automationId)
		=> new(o => o.AutomationId == automationId, $"AutomationId = '{automationId}'");

	public static UICondition CreateFromName(string name)
		=> new(o => o.Name == name, $"Name = '{name}'");

	public static UICondition CreateFromClassName(string className)
		=> new(o => o.ClassName == className, $"ClassName = '{className}'");

	public UICondition AndWith(UICondition newCondition)
		=> new(o => _predicate(o) && newCondition._predicate(o), $"({this} AND {newCondition})");

	public UICondition OrWith(UICondition newCondition)
		=> new(o => _predicate(o) || newCondition._predicate(o), $"({this} OR {newCondition})");

	public UICondition Negate() => new(o => !_predicate(o), $"NOT {this}");

	internal bool Matches(UIObject uiObject) => _predicate(uiObject);

	public override string ToString() => _description;
}
