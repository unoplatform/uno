#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Windows.Apps.Test.Foundation;

public interface IFactory<out T>
{
	T Create(UIObject element);
}

public interface IFilter<in T>
{
	bool Matches(T element);
}

public abstract class UICollection : IEnumerable
{
	IEnumerator IEnumerable.GetEnumerator() => GetUntypedEnumerator();

	private protected abstract IEnumerator GetUntypedEnumerator();
}

/// <summary>
/// Snapshot of the automation tree at the time it was queried, like a MITA collection after ToStaticCollection().
/// </summary>
public class UICollection<I> : UICollection, IList<I>
	where I : UIObject
{
	private readonly List<I> _items;

	internal UICollection(IEnumerable<I> items) => _items = items.ToList();

	public UICollection(UICollection<I> previous) => _items = previous._items.ToList();

	public bool IsReadOnly => false;

	public int Count => _items.Count;

	public I this[int index]
	{
		get => _items[index];
		set => _items[index] = value;
	}

	public I this[string automationId] => Find(automationId);

	public UICollection<I> Duplicate() => new(this);

	public UICollection<I> ToStaticCollection() => new(this);

	public void Add(I item) => _items.Add(item);

	public void Clear() => _items.Clear();

	public bool Remove(I item) => _items.Remove(item);

	public void CopyTo(I[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

	public void RemoveAt(int index) => _items.RemoveAt(index);

	public void Insert(int index, I item) => _items.Insert(index, item);

	public int IndexOf(I item) => _items.IndexOf(item);

	public bool Contains(I item) => _items.Contains(item);

	public IEnumerator<I> GetEnumerator() => _items.GetEnumerator();

	private protected override IEnumerator GetUntypedEnumerator() => _items.GetEnumerator();

	public void AddFilter(UIProperty uiProperty, object value) => AddFilter(UICondition.Create(uiProperty, value));

	public void AddFilter(UICondition condition) => _items.RemoveAll(item => !condition.Matches(item));

	public void AddFilter(IFilter<UIObject> filter) => _items.RemoveAll(item => !filter.Matches(item));

	public bool Contains(string automationId) => TryFind(automationId, out _);

	public bool Contains(UIProperty uiProperty, object value) => TryFind(uiProperty, value, out _);

	public bool Contains(UICondition condition) => TryFind(condition, out _);

	public bool Contains(IFilter<UIObject> filter) => TryFind(filter, out _);

	public bool TryFind(string automationId, out I element) => TryFind(UICondition.CreateFromId(automationId), out element);

	public bool TryFind(UIProperty uiProperty, object value, out I element) => TryFind(UICondition.Create(uiProperty, value), out element);

	public bool TryFind(UICondition condition, out I element) => TryFind(condition.Matches, out element);

	public bool TryFind(IFilter<UIObject> filter, out I element) => TryFind(filter.Matches, out element);

	public I Find(string automationId) => Find(UICondition.CreateFromId(automationId));

	public I Find(UIProperty uiProperty, object value) => Find(UICondition.Create(uiProperty, value));

	public I Find(UICondition condition)
		=> TryFind(condition, out var element) ? element : throw new UIObjectNotFoundException($"Could not find an element matching {condition}.");

	public I Find(IFilter<UIObject> filter)
		=> TryFind(filter, out var element) ? element : throw new UIObjectNotFoundException($"Could not find an element matching {filter}.");

	public UICollection<I> FindMultiple(UIProperty uiProperty, object value) => FindMultiple(UICondition.Create(uiProperty, value));

	public UICollection<I> FindMultiple(UICondition condition) => new(_items.Where(condition.Matches));

	public UICollection<I> FindMultiple(IFilter<UIObject> filter) => new(_items.Where(filter.Matches));

	public override string ToString() => $"UICollection ({Count} items)";

	private bool TryFind(Func<UIObject, bool> predicate, out I element)
	{
		element = _items.FirstOrDefault(item => predicate(item))!;
		return element is not null;
	}
}

public class UIObjectNotFoundException : Exception
{
	public UIObjectNotFoundException()
	{
	}

	public UIObjectNotFoundException(string message) : base(message)
	{
	}

	public UIObjectNotFoundException(string message, Exception innerException) : base(message, innerException)
	{
	}
}

public class PatternNotFoundException : Exception
{
	public PatternNotFoundException(string message) : base(message)
	{
	}
}

public enum PointerButtons
{
	None,
	Primary,
	Secondary,
	PhysicalLeft,
	PhysicalRight,
	Middle,
	XButton1,
	XButton2,
}

[Flags]
public enum ModifierKeys
{
	None = 0,
	ShiftFlag = 1,
	ControlFlag = 2,
	AltFlag = 4,
	LeftShiftFlag = 8,
	RightShiftFlag = 16,
	LeftControlFlag = 32,
	RightControlFlag = 64,
	LeftAltFlag = 128,
	RightAltFlag = 256,
	LeftWindowsFlag = 512,
	RightWindowsFlag = 1024,
}

public enum Scope
{
	Element,
	Children,
	Descendants,
	Subtree,
}
