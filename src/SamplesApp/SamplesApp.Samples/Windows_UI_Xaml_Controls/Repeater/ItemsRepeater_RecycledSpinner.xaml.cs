#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using MuxProgressRing = Microsoft.UI.Xaml.Controls.ProgressRing;

namespace UITests.Windows_UI_Xaml_Controls.Repeater;

[Sample(
	"ItemsRepeater",
	IsManualTest = true,
	Description =
		"Repro for the recycled-spinner render-loop leak on Skia (#23446): an active ProgressRing inside an " +
		"ItemsRepeater item kept invalidating the canvas after its item was removed, because the recycled " +
		"element stays parented with its stale DataContext and is never reused (the remaining items use a " +
		"different template). Watch the frame probe before/after removal: with the pooled-visual suspension " +
		"fix it drops to ~1-2 frames/s within ~1 s; on builds without the fix the render loop stays at full " +
		"frame rate indefinitely. CPU shows the same contrast only on software-rasterized targets.")]
public sealed partial class ItemsRepeater_RecycledSpinner : Page
{
	private readonly ObservableCollection<RecycledSpinnerItem> _items = new();

	private UIElement? _recycledSpinnerElement;
	private object? _spinnerDataContextAtRemoval;

#if __SKIA__
	private CompositionTarget? _frameSource;
	private long _framesRendered;
	private long _lastFramesRendered;
#endif

	public ItemsRepeater_RecycledSpinner()
	{
		InitializeComponent();
		Repeater.ItemsSource = _items;
		ResetItems();
		InitializeFrameProbe();
	}

	private void InitializeFrameProbe()
	{
#if __SKIA__
		// Counts composed frames passively via the Uno-internal CompositionTarget.FrameRendered,
		// raised at the end of every frame render. Element-level paints can't be used as the
		// signal (per-visual picture caching replays unchanged visuals without repainting them),
		// and the public CompositionTarget.Rendering forces the render loop to keep producing
		// frames while subscribed.
		Loaded += (_, _) =>
		{
			if (_frameSource is null && XamlRoot?.VisualTree.ContentRoot.CompositionTarget is { } target)
			{
				_frameSource = target;
				target.FrameRendered += OnFrameRendered;
			}
		};
		Unloaded += (_, _) =>
		{
			if (_frameSource is { } target)
			{
				_frameSource = null;
				target.FrameRendered -= OnFrameRendered;
			}
		};

		var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
		timer.Tick += OnFrameTimerTick;
		timer.Start();
		Unloaded += (_, _) => timer.Stop();
#else
		FramesText.Text = "Frame probe not available on this target — observe frame production externally (e.g. PresentMon).";
#endif
	}

#if __SKIA__
	private void OnFrameRendered() => _framesRendered++;

	private void OnFrameTimerTick(object? sender, object e)
	{
		var rendered = _framesRendered;
		var delta = rendered - _lastFramesRendered;
		_lastFramesRendered = rendered;

		FramesText.Text = $"Frames rendered in the last second: {delta} " +
			(delta > 10 ? "— render loop is awake." : "— render loop is idle.");
	}
#endif

	private void ResetItems()
	{
		_items.Clear();
		_items.Add(new RecycledSpinnerItem("A plain text row", IsSpinning: false));
		_items.Add(new RecycledSpinnerItem("Working… (active spinner row)", IsSpinning: true));
		_items.Add(new RecycledSpinnerItem("Another plain text row", IsSpinning: false));

		_recycledSpinnerElement = null;
		_spinnerDataContextAtRemoval = null;
		MechanismText.Text = "Spinner row present and animating.";
		CpuText.Text = string.Empty;
		RemoveSpinnerButton.IsEnabled = true;
	}

	private void OnResetClick(object sender, RoutedEventArgs e) => ResetItems();

	private async void OnRemoveSpinnerClick(object sender, RoutedEventArgs e)
	{
		try
		{
			var spinnerIndex = -1;
			for (var i = 0; i < _items.Count; i++)
			{
				if (_items[i].IsSpinning)
				{
					spinnerIndex = i;
					break;
				}
			}

			if (spinnerIndex < 0)
			{
				MechanismText.Text = "No spinner item to remove — press Reset first.";
				return;
			}

			// Capture the realized element before removal so the recycled element can be inspected after.
			_recycledSpinnerElement = Repeater.TryGetElement(spinnerIndex);
			_spinnerDataContextAtRemoval = (_recycledSpinnerElement as FrameworkElement)?.DataContext;

			_items.RemoveAt(spinnerIndex);
			RemoveSpinnerButton.IsEnabled = false;

			// Let the repeater process the collection change and recycle the element.
			await Task.Delay(500);

			UpdateMechanismText();
		}
		catch (Exception ex)
		{
			MechanismText.Text = $"Removal failed: {ex.Message}";
		}
	}

	private void UpdateMechanismText()
	{
		if (_recycledSpinnerElement is not { } element)
		{
			MechanismText.Text = "Spinner element was not realized before removal.";
			return;
		}

		var isParented = VisualTreeHelper.GetParent(element) is not null;
		var dataContextUnchanged = ReferenceEquals(
			(element as FrameworkElement)?.DataContext,
			_spinnerDataContextAtRemoval);
		var ring = FindDescendant<MuxProgressRing>(element);

		MechanismText.Text =
			$"Recycled element after removal: stillParented={isParented}, " +
			$"dataContextUnchanged={dataContextUnchanged}, " +
			$"ringIsActive={(ring is null ? "ring not found" : ring.IsActive.ToString())} " +
			"(expected on Uno Platform/Skia: True / True / True — with the fix the pooled visual is suspended, so the render loop idles; " +
			"on builds without the fix the ring keeps invalidating the canvas with nothing visible; " +
			"on WinUI: True / False / True — WinUI clears the DataContext on recycle, yet the ring stays active there too, at no render cost).";
	}

	private async void OnMeasureCpuClick(object sender, RoutedEventArgs e)
	{
		try
		{
			CpuText.Text = "Measuring CPU for 2 s…";

			using var process = Process.GetCurrentProcess();
			var cpuBefore = process.TotalProcessorTime;
			var stopwatch = Stopwatch.StartNew();

			await Task.Delay(2000);

			process.Refresh();
			stopwatch.Stop();
			var coresUsed = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds / stopwatch.Elapsed.TotalMilliseconds;

			CpuText.Text =
				$"Process CPU over {stopwatch.Elapsed.TotalMilliseconds:F0} ms: {coresUsed:P0} of one core " +
				(coresUsed > 0.5 ? "— render loop is busy/pinned." : "— near idle.") +
				" CPU only tracks the render loop on software-rasterized targets; on GPU-accelerated desktops rely on the frame probe.";
		}
		catch (Exception ex)
		{
			// Process CPU accounting is unavailable on some targets (e.g. WebAssembly);
			// use the browser's performance tools / paint flashing there instead.
			CpuText.Text = $"CPU measurement unavailable on this target ({ex.GetType().Name}). " +
				"On WebAssembly use DevTools → Rendering → Paint flashing.";
		}
	}

	private static T? FindDescendant<T>(DependencyObject root) where T : class
	{
		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is T typed)
			{
				return typed;
			}

			if (FindDescendant<T>(child) is { } nested)
			{
				return nested;
			}
		}

		return null;
	}
}

public sealed record RecycledSpinnerItem(string Title, bool IsSpinning);

public sealed partial class RecycledSpinnerTemplateSelector : DataTemplateSelector
{
	public DataTemplate? SpinnerTemplate { get; set; }

	public DataTemplate? TextTemplate { get; set; }

	protected override DataTemplate SelectTemplateCore(object item)
		=> (item is RecycledSpinnerItem { IsSpinning: true } ? SpinnerTemplate : TextTemplate)!;

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		=> SelectTemplateCore(item);
}
