using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition",
		Name = "CompositionBrushContextLeak",
		Description = "Shared SolidColorBrush: dead CompositionObject context entries must not accumulate when consumers are removed and collected (Skia).",
		IsManualTest = true,
		IgnoreInSnapshotTests = true)]
	public sealed partial class CompositionBrushContextLeak : UserControl
	{
		private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

		private readonly SolidColorBrush _sharedBrush = new SolidColorBrush(Colors.SteelBlue);
		private int _iterations;

		public CompositionBrushContextLeak()
		{
			this.InitializeComponent();
			Loaded += (_, _) => ReportStatus();
		}

		private async void OnRun(object sender, RoutedEventArgs e)
		{
			RunButton.IsEnabled = false;
			try
			{
				for (var i = 0; i < 200; i++)
				{
					AddConsumers();
					await Task.Delay(30); // Let them render, so the composition brush is assigned to their visuals.
					Host.Children.Clear();
					_iterations++;
				}

				await Task.Delay(100);
				Collect();

				// One more consumer gives AddContext a chance to sweep what was just collected.
				AddConsumers();
				await Task.Delay(30);
				Host.Children.Clear();
				Collect();

				ReportStatus();
			}
			catch (Exception ex)
			{
				// async void click handler: an escaping exception would take down the app.
				StatusText.Text = $"Run failed: {ex}";
			}
			finally
			{
				RunButton.IsEnabled = true;
			}
		}

		private void OnChangeColor(object sender, RoutedEventArgs e)
		{
			// Control: a change of the brush walks (and compacts) its context entries.
			_sharedBrush.Color = _sharedBrush.Color == Colors.SteelBlue ? Colors.IndianRed : Colors.SteelBlue;
			ReportStatus();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void AddConsumers()
		{
			Host.Children.Add(new Border { Width = 40, Height = 40, Background = _sharedBrush });
			Host.Children.Add(new Rectangle { Width = 40, Height = 40, Margin = new Thickness(50, 0, 0, 0), Fill = _sharedBrush, HorizontalAlignment = HorizontalAlignment.Left });
		}

		private static void Collect()
		{
			for (var i = 0; i < 3; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}

		private void ReportStatus()
		{
			var (total, dead) = CountContextEntries(_sharedBrush);
			StatusText.Text = total < 0
				? $"iterations={_iterations}: no composition brush (not Skia, or not rendered yet)"
				: $"iterations={_iterations}: context entries total={total} dead={dead} alive={total - dead}";
		}

		// Reads Brush._compositionBrush and CompositionObject._contextEntries by reflection (Skia only).
		private static (int total, int dead) CountContextEntries(Brush brush)
		{
			var compositionBrush = typeof(Brush).GetField("_compositionBrush", NonPublicInstance)?.GetValue(brush);
			if (compositionBrush is null)
			{
				return (-1, -1);
			}

			var compositionObjectType = compositionBrush.GetType();
			while (compositionObjectType is not null && compositionObjectType.Name != "CompositionObject")
			{
				compositionObjectType = compositionObjectType.BaseType;
			}

			if (compositionObjectType?.GetField("_contextEntries", NonPublicInstance)?.GetValue(compositionBrush) is not IList entries)
			{
				return (0, 0);
			}

			var dead = 0;
			foreach (var entry in entries)
			{
				var weakRef = entry.GetType().GetProperty("Context")?.GetValue(entry);
				var tryGetTarget = weakRef?.GetType().GetMethod("TryGetTarget");
				var args = new object[] { null };
				if (tryGetTarget is null || !(bool)tryGetTarget.Invoke(weakRef, args))
				{
					dead++;
				}
			}

			return (entries.Count, dead);
		}
	}
}
