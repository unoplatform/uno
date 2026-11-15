#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.Windows.Apps.Test.Foundation.Waiters;

/// <summary>
/// Async counterpart of MITA's Waiter. The test runs on the UI thread, so waiting must yield to it:
/// <c>waiter.Wait()</c> becomes <c>await waiter.Wait()</c>.
/// </summary>
public class Waiter : IDisposable
{
	private const int PollIntervalMs = 10;

	public static TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(10);

	public Exception? InnerException { get; set; }

	public Task Wait() => Wait(DefaultTimeout);

	public Task Wait(int timeout) => Wait(TimeSpan.FromMilliseconds(timeout));

	public async Task Wait(TimeSpan timeout)
	{
		if (!await TryWait(timeout))
		{
			throw new WaiterTimedOutException($"{this} timed out after {timeout.TotalMilliseconds}ms.", InnerException);
		}
	}

	public Task<bool> TryWait() => TryWait(DefaultTimeout);

	public Task<bool> TryWait(int timeout) => TryWait(TimeSpan.FromMilliseconds(timeout));

	public virtual async Task<bool> TryWait(TimeSpan timeout)
	{
		var stopwatch = Stopwatch.StartNew();
		while (true)
		{
			if (IsSignaled())
			{
				return true;
			}

			if (stopwatch.Elapsed >= timeout)
			{
				return false;
			}

			await Task.Delay(PollIntervalMs);
		}
	}

	public virtual void Reset()
	{
	}

	private protected virtual bool IsSignaled() => false;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}
}

public class CompositableWaiter : Waiter
{
	public static async Task<CompositableWaiter?> WaitAny(TimeSpan timeout, params CompositableWaiter[] waiters)
	{
		var stopwatch = Stopwatch.StartNew();
		do
		{
			if (waiters.FirstOrDefault(w => w.IsSignaled()) is { } signaled)
			{
				return signaled;
			}

			await Task.Delay(10);
		}
		while (stopwatch.Elapsed < timeout);

		return null;
	}
}

public class TimeWaiter : Waiter
{
	private readonly TimeSpan _duration;

	public TimeWaiter(TimeSpan duration) => _duration = duration;

	public override async Task<bool> TryWait(TimeSpan timeout)
	{
		await Task.Delay(_duration < timeout ? _duration : timeout);
		return true;
	}
}

public class WaiterEventArgs : EventArgs
{
	public WaiterEventArgs(UIObject? sender, EventArgs? eventArgs = null)
	{
		Sender = sender;
		EventArgs = eventArgs ?? EventArgs.Empty;
	}

	public UIObject? Sender { get; }

	public EventArgs EventArgs { get; }
}

/// <summary>
/// Detects an "event" by polling, since there is no out-of-process UIA event stream.
/// </summary>
public interface IEventSource
{
	void Start();

	bool TryGetEvent(out WaiterEventArgs args);
}

public class UIEventWaiter : CompositableWaiter, IDisposable
{
	private readonly List<Predicate<WaiterEventArgs>> _filters = new();
	private readonly IEventSource _source;
	private bool _signaled;

	public UIEventWaiter(IEventSource source)
	{
		_source = source;
		_source.Start();
	}

	public UIObject? Source => Arguments?.Sender;

	public WaiterEventArgs? Arguments { get; private set; }

	public IFactory<UIObject> Factory { get; set; } = UIObject.Factory;

	public event EventHandler<WaiterEventArgs>? Notify;

	public void AddFilter(Predicate<WaiterEventArgs> callback) => _filters.Add(callback);

	public void RemoveFilter(Predicate<WaiterEventArgs> callback) => _filters.Remove(callback);

	public override void Reset()
	{
		_signaled = false;
		Arguments = null;
		_source.Start();
	}

	protected void Start() => _source.Start();

	protected override void Dispose(bool disposing)
	{
		if (disposing && _source is IDisposable disposable)
		{
			disposable.Dispose();
		}

		base.Dispose(disposing);
	}

	private protected override bool IsSignaled()
	{
		while (!_signaled && _source.TryGetEvent(out var args))
		{
			if (_filters.All(filter => filter(args)))
			{
				_signaled = true;
				Arguments = args;
				Notify?.Invoke(this, args);
			}
		}

		return _signaled;
	}
}

public class PropertyChangedEventSource : IEventSource
{
	private readonly UIObject _root;
	private readonly Scope _scope;
	private readonly UIProperty[] _properties;
	private Dictionary<(UIObject, UIProperty), object?> _snapshot = new();

	public PropertyChangedEventSource(UIObject root, Scope scope, params UIProperty[] properties)
	{
		_root = root;
		_scope = scope;
		_properties = properties;
	}

	public void Start() => _snapshot = TakeSnapshot();

	public bool TryGetEvent(out WaiterEventArgs args)
	{
		var current = TakeSnapshot();
		foreach (var entry in current)
		{
			if (!_snapshot.TryGetValue(entry.Key, out var previous) || !Equals(previous, entry.Value))
			{
				_snapshot = current;
				args = new WaiterEventArgs(entry.Key.Item1);
				return true;
			}
		}

		_snapshot = current;
		args = null!;
		return false;
	}

	private Dictionary<(UIObject, UIProperty), object?> TakeSnapshot()
	{
		var snapshot = new Dictionary<(UIObject, UIProperty), object?>();
		foreach (var element in GetElements())
		{
			foreach (var property in _properties)
			{
				snapshot[(element, property)] = TryGetValue(element, property);
			}
		}

		return snapshot;
	}

	private IEnumerable<UIObject> GetElements() => _scope switch
	{
		Scope.Element => new[] { _root },
		Scope.Children => _root.Children,
		Scope.Descendants => _root.Descendants,
		_ => new[] { _root }.Concat(_root.Descendants),
	};

	private static object? TryGetValue(UIObject element, UIProperty property)
	{
		try
		{
			return property.GetValue(element);
		}
		catch (PatternNotFoundException)
		{
			return null;
		}
	}
}

public class PropertyChangedEventWaiter : UIEventWaiter
{
	public PropertyChangedEventWaiter(UIObject root, params UIProperty[] uiProperties)
		: this(root, Scope.Element, uiProperties)
	{
	}

	public PropertyChangedEventWaiter(UIObject root, Scope scope, params UIProperty[] uiProperties)
		: base(new PropertyChangedEventSource(root, scope, uiProperties))
	{
	}
}

public class FocusAcquiredWaiter : UIEventWaiter
{
	private readonly string _description;

	public FocusAcquiredWaiter()
		: this(UICondition.True, "any element")
	{
	}

	public FocusAcquiredWaiter(string automationId)
		: this(UICondition.CreateFromId(automationId), $"AutomationId '{automationId}'")
	{
	}

	public FocusAcquiredWaiter(UIProperty uiProperty, string value)
		: this(UICondition.Create(uiProperty, value), $"{uiProperty} '{value}'")
	{
	}

	public FocusAcquiredWaiter(UICondition condition)
		: this(condition, condition.ToString())
	{
	}

	private FocusAcquiredWaiter(UICondition condition, string description)
		: base(new FocusEventSource(condition))
		=> _description = description;

	public override string ToString() => $"FocusAcquiredWaiter for {_description}";

	private sealed class FocusEventSource : IEventSource
	{
		private readonly UICondition _condition;
		private UIObject? _initial;

		public FocusEventSource(UICondition condition) => _condition = condition;

		public void Start() => _initial = UIObject.Focused;

		public bool TryGetEvent(out WaiterEventArgs args)
		{
			var focused = UIObject.Focused;
			if (focused is not null && !focused.Equals(_initial) && _condition.Matches(focused))
			{
				args = new WaiterEventArgs(focused);
				return true;
			}

			args = null!;
			return false;
		}
	}
}

/// <summary>
/// Signals while <paramref name="condition"/> holds, e.g. the ExpandCollapse "expanded" event.
/// </summary>
internal sealed class ConditionWaiter : UIEventWaiter
{
	private readonly string _description;

	public ConditionWaiter(UIObject source, Func<bool> condition, string description)
		: base(new ConditionEventSource(source, condition))
		=> _description = description;

	public override string ToString() => $"{_description} waiter on {Source}";

	private sealed class ConditionEventSource : IEventSource
	{
		private readonly UIObject _source;
		private readonly Func<bool> _condition;

		public ConditionEventSource(UIObject source, Func<bool> condition)
		{
			_source = source;
			_condition = condition;
		}

		public void Start()
		{
		}

		public bool TryGetEvent(out WaiterEventArgs args)
		{
			args = new WaiterEventArgs(_source);
			return _condition();
		}
	}
}

/// <summary>
/// Signals on the owner's Click; other invokable elements invoke synchronously in-process,
/// so they signal right away.
/// </summary>
internal sealed class InvokedEventWaiter : UIEventWaiter
{
	public InvokedEventWaiter(UIObject source)
		: base(new InvokedEventSource(source))
	{
	}

	private sealed class InvokedEventSource : IEventSource, IDisposable
	{
		private readonly UIObject _source;
		private readonly Action? _unsubscribe;
		private bool _invoked;

		public InvokedEventSource(UIObject source)
		{
			_source = source;
			switch (source.Owner)
			{
				case ButtonBase button:
					button.Click += OnClick;
					_unsubscribe = () => button.Click -= OnClick;
					break;
				case MenuFlyoutItem item:
					item.Click += OnClick;
					_unsubscribe = () => item.Click -= OnClick;
					break;
				case SplitButton splitButton:
					splitButton.Click += OnSplitButtonClick;
					_unsubscribe = () => splitButton.Click -= OnSplitButtonClick;
					break;
			}
		}

		public void Start() => _invoked = false;

		public bool TryGetEvent(out WaiterEventArgs args)
		{
			args = new WaiterEventArgs(_source);
			var invoked = _invoked || _unsubscribe is null;
			_invoked = false;
			return invoked;
		}

		public void Dispose() => _unsubscribe?.Invoke();

		private void OnClick(object sender, RoutedEventArgs e) => _invoked = true;

		private void OnSplitButtonClick(SplitButton sender, SplitButtonClickEventArgs args) => _invoked = true;
	}
}

public class WaiterException : Exception
{
	public WaiterException()
	{
	}

	public WaiterException(string message) : base(message)
	{
	}

	public WaiterException(string message, Exception? innerException) : base(message, innerException)
	{
	}
}

public class WaiterTimedOutException : WaiterException
{
	public WaiterTimedOutException()
	{
	}

	public WaiterTimedOutException(string message) : base(message)
	{
	}

	public WaiterTimedOutException(string message, Exception? innerException) : base(message, innerException)
	{
	}
}
