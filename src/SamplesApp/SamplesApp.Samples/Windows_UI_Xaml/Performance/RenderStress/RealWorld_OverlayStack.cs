#nullable enable

using System;
using System.Collections.Generic;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Performance.RenderStress
{
	/// <summary>
	/// Overlay-stack archetype: a busy page under a stack of FULL-VIEWPORT translucent panes — the shape an app
	/// takes when a modal sits over a flyout over a scrim over content.
	/// <para>
	/// This is the deliberate DRAW-phase worst case, and it scales the only thing that actually scales fill rate:
	/// overdraw per pixel. A wall of small cards cannot do it — off-screen items are culled, so adding items adds
	/// record/layout and no draw at all. Here every layer covers the whole viewport and carries content behind a
	/// sub-1 Opacity, so each one forces a screen-sized offscreen (SaveLayer) that is drawn into and then
	/// composited: N layers means N full-screen allocations plus N full-screen blends per frame, on top of a
	/// multi-stop gradient evaluated per pixel per layer.
	/// </para>
	/// <c>count</c> is the number of stacked panes, so draw should scale close to linearly with it while record
	/// and layout stay flat.
	/// </summary>
	[Sample("Performance", Name = "RealWorld_OverlayStack", Description = "Real-UI perf: a busy page under N full-viewport translucent overlay panes (modal-over-flyout-over-scrim). Each pane is a screen-sized isolation layer + gradient, so this scales OVERDRAW — the draw-phase worst case. Compare fps/frame-time across Skia and the WebGPU/ProGPU builds.")]
	public sealed class RealWorld_OverlayStack : PerfBenchBase
	{
		private readonly List<LinearGradientBrush> _paneBrushes = new();
		private ScrollViewer _sv = null!;
		private double _offset;
		private int _dir = 1;

		protected override string ScenarioName => "OverlayStack";

		/// <summary>Number of stacked full-viewport panes. Each one is a screen-sized offscreen + blend.</summary>
		protected override int DefaultCount => 14;

		protected override UIElement BuildStage(int count)
		{
			var rng = new Rng(61);
			_paneBrushes.Clear();

			var root = new Grid();

			// --- Base content: something real underneath, kept cheap so it does not pollute record. ---
			var list = new StackPanel { Padding = new Thickness(16), Spacing = 6 };
			for (var i = 0; i < 120; i++)
			{
				var row = new Border
				{
					Height = 34,
					CornerRadius = new CornerRadius(6),
					Background = new SolidColorBrush(Color.FromArgb((byte)(0x18 + (i % 3) * 8), 0xFF, 0xFF, 0xFF)),
					Padding = new Thickness(10, 6, 10, 6),
					Child = new TextBlock
					{
						Text = $"RFI-{1800 + i} · Awaiting response · Ball in court: Architect · Due 9/{1 + (i % 28):D2}",
						FontSize = 12,
						Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xD8, 0xDE, 0xF0)),
					},
				};
				list.Children.Add(row);
			}

			_sv = new ScrollViewer
			{
				Content = list,
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				HorizontalScrollMode = ScrollMode.Disabled,
			};
			root.Children.Add(_sv);

			// --- The overlay stack: every pane is viewport-sized, translucent, and has content. ---
			for (var i = 0; i < count; i++)
			{
				// A multi-stop diagonal gradient across the full viewport: per-pixel shader work per layer.
				var brush = new LinearGradientBrush { StartPoint = new(0, 0), EndPoint = new(1, 1) };
				brush.GradientStops.Add(new GradientStop { Offset = 0, Color = Color.FromArgb(0x30, 0x10, 0x18, 0x30) });
				brush.GradientStops.Add(new GradientStop { Offset = 0.35, Color = Color.FromArgb(0x40, rng.Byte(), 0x40, 0xC0) });
				brush.GradientStops.Add(new GradientStop { Offset = 0.7, Color = Color.FromArgb(0x30, 0x20, rng.Byte(), 0x90) });
				brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Color.FromArgb(0x50, 0x08, 0x0C, 0x1A) });
				_paneBrushes.Add(brush);

				// Opacity < 1 on an element WITH content is what forces the isolation layer; without a child the
				// compositor could fold the alpha into the fill and skip the offscreen entirely.
				var pane = new Border
				{
					Opacity = 0.55,
					Background = brush,
					Child = new Grid
					{
						Children =
						{
							// A centred "dialog" so each pane also carries real content into its layer.
							new Border
							{
								Width = 520,
								Height = 300,
								CornerRadius = new CornerRadius(14),
								HorizontalAlignment = HorizontalAlignment.Center,
								VerticalAlignment = VerticalAlignment.Center,
								Background = new SolidColorBrush(Color.FromArgb(0x90, 0x1A, 0x1E, 0x2C)),
								BorderThickness = new Thickness(1),
								BorderBrush = new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF)),
								Padding = new Thickness(18),
								Child = new StackPanel
								{
									Spacing = 8,
									Children =
									{
										new TextBlock
										{
											Text = $"Confirm transmittal ({i + 1})",
											FontSize = 16,
											Foreground = new SolidColorBrush(Windows.UI.Colors.White),
										},
										new TextBlock
										{
											Text = "Recipients will be notified and the document set will be locked for revision.",
											FontSize = 12,
											TextWrapping = TextWrapping.Wrap,
											Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xC0, 0xC8, 0xE0)),
										},
									},
								},
							},
						},
					},
				};

				root.Children.Add(pane);
			}

			_offset = 0;
			_dir = 1;
			return root;
		}

		protected override void Tick(long frame)
		{
			// Scroll the base list so the viewport is dirty every frame (all panes must therefore be
			// re-composited), and sweep each pane's gradient so the per-pixel shader work is genuinely redone
			// rather than served from a cached layer. Neither touches layout or invalidates a shadow silhouette.
			AdvanceScroll(_sv, ref _offset, ref _dir, 6.0);

			var t = frame * 0.01;
			for (var i = 0; i < _paneBrushes.Count; i++)
			{
				var phase = t + i * 0.09;
				var dx = 0.5 + 0.5 * Math.Sin(phase);
				var dy = 0.5 + 0.5 * Math.Cos(phase * 0.8);
				_paneBrushes[i].StartPoint = new Windows.Foundation.Point(dx * 0.25, dy * 0.25);
				_paneBrushes[i].EndPoint = new Windows.Foundation.Point(1 - dx * 0.25, 1 - dy * 0.25);
			}
		}
	}
}
