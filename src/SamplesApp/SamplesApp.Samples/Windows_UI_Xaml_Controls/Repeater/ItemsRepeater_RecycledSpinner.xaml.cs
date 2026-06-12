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
		"Repro for the recycled-spinner render-loop leak on Skia: an active ProgressRing inside an " +
		"ItemsRepeater item keeps invalidating the canvas after its item is removed, because the recycled " +
		"element stays parented with its stale DataContext and is never reused (the remaining items use a " +
		"different template). Measure CPU before/after removal; after removal it should be near-idle but " +
		"stays at ~100% of one core.")]
public sealed partial class ItemsRepeater_RecycledSpinner : Page
{
	private readonly ObservableCollection<RecycledSpinnerItem> _items = new();

	private UIElement? _recycledSpinnerElement;
	private object? _spinnerDataContextAtRemoval;

	public ItemsRepeater_RecycledSpinner()
	{
		InitializeComponent();
		Repeater.ItemsSource = _items;
		ResetItems();
	}

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
			"(expected by this bug: True / True / True — nothing visible, yet the ring keeps invalidating the canvas).";
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
				(coresUsed > 0.5 ? "— render loop is busy/pinned." : "— near idle.");
		}
		catch (Exception ex)
		{
			// Process CPU accounting is unavailable on some targets (e.g. WebAssembly);
			// use the browser's performance tools / paint flashing there instead.
			CpuText.Text = $"CPU measurement unavailable on this target ({ex.GetType().Name}). " +
				"On WebAssembly use DevTools → Rendering → Paint flashing.";
		}
	}

	private static T? FindDescendant<T>(DependencyObject root) where T : class, DependencyObject
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
