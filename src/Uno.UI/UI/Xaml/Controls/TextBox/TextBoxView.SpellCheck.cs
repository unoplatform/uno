#nullable enable

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI;

namespace Microsoft.UI.Xaml.Controls;

internal partial class TextBoxView
{
	// Spell-check squiggles sit below the line box, below the descenders. The scroll presenter clips the text to its
	// lines, so the squiggles are drawn in a layer on the content element instead: above the text, clipped to the
	// padding box, which leaves the text's own clipping as it is.
	private SpellCheckSquiggleVisual? _squiggleVisual;
	private ContentControl? _squiggleHost;
	private IParsedText? _squiggleText;

	internal void AttachSpellCheckSquiggles(ContentControl host)
	{
		if (ReferenceEquals(_squiggleHost, host))
		{
			return;
		}

		DetachSpellCheckSquiggles();

		_squiggleVisual ??= new SpellCheckSquiggleVisual(host.Visual.Compositor, this);
		_squiggleHost = host;

		ElementCompositionPreview.SetElementChildVisual(host, _squiggleVisual);
		host.SizeChanged += OnSquiggleHostSizeChanged;
		host.Loaded += OnSquiggleHostLoaded;
		if (host is ScrollViewer scrollViewer)
		{
			scrollViewer.ViewChanging += OnSquiggleHostViewChanging;
			scrollViewer.ViewChanged += OnSquiggleHostViewChanged;
		}
		DisplayBlock.DrawingFinished += OnSquiggleSourceDrawingFinished;

		UpdateSquiggleVisualSize();
	}

	private void DetachSpellCheckSquiggles()
	{
		if (_squiggleHost is { } host)
		{
			ElementCompositionPreview.SetElementChildVisual(host, null);
			host.SizeChanged -= OnSquiggleHostSizeChanged;
			host.Loaded -= OnSquiggleHostLoaded;
			if (host is ScrollViewer scrollViewer)
			{
				scrollViewer.ViewChanging -= OnSquiggleHostViewChanging;
				scrollViewer.ViewChanged -= OnSquiggleHostViewChanged;
			}

			DisplayBlock.DrawingFinished -= OnSquiggleSourceDrawingFinished;
		}

		_squiggleHost = null;
		_squiggleText = null;
	}

	// The template's own visuals are added after the layer when the host is first templated; re-adding the layer
	// puts it back on top of them.
	private void OnSquiggleHostLoaded(object sender, RoutedEventArgs e)
	{
		if (_squiggleHost is { } host && _squiggleVisual is { } visual)
		{
			ElementCompositionPreview.SetElementChildVisual(host, visual);
		}
	}

	private void OnSquiggleHostSizeChanged(object sender, SizeChangedEventArgs e) => UpdateSquiggleVisualSize();

	private void OnSquiggleHostViewChanging(object? sender, ScrollViewerViewChangingEventArgs e) => _squiggleVisual?.InvalidatePaint();

	private void OnSquiggleHostViewChanged(object? sender, ScrollViewerViewChangedEventArgs e) => _squiggleVisual?.InvalidatePaint();

	private void OnSquiggleSourceDrawingFinished(object? sender, EventArgs e)
	{
		// A parsed text's squiggles are fixed by its text and layout, so only a new parse changes them.
		if (!ReferenceEquals(DisplayBlock.ParsedText, _squiggleText))
		{
			_squiggleText = DisplayBlock.ParsedText;
			_squiggleVisual?.InvalidatePaint();
		}
	}

	private void UpdateSquiggleVisualSize()
	{
		if (_squiggleHost is { } host && _squiggleVisual is { } visual)
		{
			visual.Size = new Vector2((float)host.ActualWidth, (float)host.ActualHeight);
			visual.InvalidatePaint();
		}
	}

	private sealed class SpellCheckSquiggleVisual : ContainerVisual
	{
		private readonly WeakReference<TextBoxView> _owner;

		public SpellCheckSquiggleVisual(Compositor compositor, TextBoxView owner) : base(compositor)
		{
			_owner = new WeakReference<TextBoxView>(owner);
			Clip = compositor.CreateInsetClip(0, 0, 0, 0);
		}

		internal override void Paint(in PaintingSession session)
		{
			if (!_owner.TryGetTarget(out var view)
				|| view._squiggleText is not { } text
				|| view._squiggleHost is not { } host)
			{
				return;
			}

			var source = view.DisplayBlock;
			// The text is drawn inside the source's padding; scrolling and flow direction are in the transform.
			var padding = source.Padding;
			var toHost = Matrix3x2.CreateTranslation((float)padding.Left, (float)padding.Top) * UIElement.GetTransform(from: source, to: host);
			var red = Colors.Red;
			var color = Color.FromArgb((byte)(red.A * Math.Clamp(session.Opacity, 0f, 1f)), red.R, red.G, red.B);

			var canvas = session.Session;
			var saveCount = canvas.Save();
			canvas.Concat(new Matrix4x4(toHost));
			text.DrawSpellCheckSquiggles(canvas, color);
			canvas.RestoreToCount(saveCount);
		}

		internal override bool CanPaint() => true;
	}
}
