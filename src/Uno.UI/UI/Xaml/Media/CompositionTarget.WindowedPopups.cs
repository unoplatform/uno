#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Composition.Drawing;
using Uno.UI.Xaml.Controls;
using Windows.Graphics;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	// Null until a windowed popup opens, so a window without one pays a single null check per frame.
	private List<WindowedPopupSurface>? _windowedPopupSurfaces;

	internal void AddWindowedPopupSurface(WindowedPopupSurface surface)
	{
		(_windowedPopupSurfaces ??= new()).Add(surface);
		((Uno.UI.Composition.ICompositionTarget)this).RequestNewFrame();
	}

	internal void RemoveWindowedPopupSurface(WindowedPopupSurface surface)
	{
		_windowedPopupSurfaces?.Remove(surface);
		if (_windowedPopupSurfaces is { Count: 0 })
		{
			_windowedPopupSurfaces = null;
		}
	}

	private void RecordWindowedPopupSurfaces(List<WindowedPopupSurface> surfaces)
	{
		// A popup's position depends on the layout that just settled, and repositioning can close a popup.
		for (var i = surfaces.Count - 1; i >= 0 && i < surfaces.Count; i--)
		{
			surfaces[i].Record(Renderer);
		}
	}

	/// <summary>
	/// The frame state of a windowed popup's window, the counterpart of the frame slot <see cref="Render"/> and
	/// <see cref="Draw"/> share for the main window. A popup's window repaints whole: its frames aren't damage-tracked.
	/// </summary>
	internal sealed class WindowedPopupSurface(Popup popup) : IWindowedPopupContent
	{
		private readonly Lock _frameGate = new();

		// The site lives in the host's window list, and must not keep a closed popup alive.
		private readonly WeakReference<Popup> _popup = new(popup);
		private (FrameHold frame, RectInt32 bounds, float scale)? _frame;

		internal IWindowedPopupSite? Site { get; set; }

		internal void Record(IDrawingFactory factory)
		{
			if (!_popup.TryGetTarget(out var popup) || Site is not { } site)
			{
				return;
			}

			popup.Reposition();

			if (!popup.TryGetWindowedFrame(out var bounds, out var scale, out var visuals))
			{
				return;
			}

			var recording = factory.CreateRecording();
			foreach (var visual in visuals)
			{
				visual.RenderOwnSurface(recording);
			}

			var hold = new FrameHold(recording.Finish());
			(FrameHold frame, RectInt32 bounds, float scale)? previous;
			lock (_frameGate)
			{
				previous = _frame;
				_frame = (hold, bounds, scale);
			}

			previous?.frame.OnPipelineReleased();
			site.InvalidateRender();
		}

		internal void DropFrame()
		{
			(FrameHold frame, RectInt32 bounds, float scale)? previous;
			lock (_frameGate)
			{
				previous = _frame;
				_frame = null;
			}

			previous?.frame.OnPipelineReleased();
		}

		bool IWindowedPopupContent.DrawFrame(ISwapChain swapChain, IDrawingFactory renderer)
		{
			(FrameHold frame, RectInt32 bounds, float scale)? borrowed;
			lock (_frameGate)
			{
				borrowed = _frame;
				_frame = null;
			}

			try
			{
				if (borrowed is not { } frame)
				{
					return false;
				}

				var target = swapChain.AcquireRenderTarget(frame.bounds.Width, frame.bounds.Height);

				// The window takes a piece out of the main window's coordinate space: the popup's visuals are already placed
				// in it by their total matrix. MUX Reference Popup.cpp, the undo transform of PositionAndSizeWindowForWindowedPopup.
				var scale = Matrix4x4.CreateScale(frame.scale, frame.scale, 1f);
				var offset = Matrix4x4.CreateTranslation(-frame.bounds.X, -frame.bounds.Y, 0f);

				using (var present = BeginPresent(renderer, target, default))
				{
					present.Clear(global::Microsoft.UI.Colors.Transparent);
					present.Save();
					present.Concat(scale * offset);
					frame.frame.Record.Replay(present);
					present.Restore();
				}

				return true;
			}
			finally
			{
				swapChain.Present();

				// The frame stays, so a surface that couldn't show it (a just-resized swapchain) or that was only just
				// shown can present it again.
				if (borrowed is { } returned)
				{
					var dispose = false;
					lock (_frameGate)
					{
						if (_frame is null)
						{
							_frame = returned;
						}
						else
						{
							dispose = true;
						}
					}

					if (dispose)
					{
						returned.frame.OnPipelineReleased();
					}
				}
			}
		}

		void IWindowedPopupContent.OnOwnerBoundsChanged()
		{
			if (_popup.TryGetTarget(out var popup))
			{
				popup.Reposition();
			}
		}

		void IWindowedPopupContent.OnSiteClosed()
		{
			if (_popup.TryGetTarget(out var popup))
			{
				popup.OnWindowedPopupSiteClosed();
			}
		}

		UIElement? IWindowedPopupContent.AutomationRoot => _popup.TryGetTarget(out var popup) ? popup.Child : null;
	}
}
