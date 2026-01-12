using System;
using Uno;
using Uno.UI.DataBinding;


#if HAS_UNO_WINUI
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
#else
using DispatcherQueueTimer = Windows.System.DispatcherQueueTimer;
#endif

namespace Microsoft.UI.Xaml;

/// <summary>
/// Provides a timer that is integrated into the Dispatcher queue, which is processed at a specified interval of time and at a specified priority.
/// </summary>
public partial class DispatcherTimer : IDispatcherTimer
{
	private readonly DispatcherQueueTimer _timer;

	/// <summary>
	/// Initializes a new instance of the DispatcherTimer class. 
	/// </summary>
	public DispatcherTimer()
	{
		_timer = new DispatcherQueueTimer(simulateDispatcherTimer: true);
		_timer.Tick += OnTick;
	}

	/// <summary>
	/// Occurs when the timer interval has elapsed.
	/// </summary>
	public event EventHandler<object> Tick;

	/// <summary>
	/// Gets or sets the amount of time between timer ticks.
	/// </summary>
	public TimeSpan Interval
	{
		get => _timer.Interval;
		set => _timer.Interval = value;
	}

	/// <summary>
	/// Gets a value that indicates whether the timer is running.
	/// </summary>
	public bool IsEnabled => _timer.IsRunning;

	/// <summary>
	/// Starts the DispatcherTimer.
	/// </summary>
	public void Start() => _timer.Start();

	/// <summary>
	/// Stops the DispatcherTimer.
	/// </summary>
	public void Stop() => _timer.Stop();

	private void OnTick(DispatcherQueueTimer sender, object args) => Tick?.Invoke(this, null);

	public DependencyObject TargetObject { get; set; }
}
