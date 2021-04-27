#nullable enable

//#define DRAW_DRAW_BOUNDS

#if __ANDROID__
using System.Numerics;
using System;
using System.Threading;
using Android.Graphics;
using Android.Views;
using Microsoft.Extensions.Logging;
using Uno;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Composition
{
	public partial class Visual : global::Windows.UI.Composition.CompositionObject
	{
		private readonly RenderNode _node = new RenderNode(string.Empty);

#if DRAW_DRAW_BOUNDS
		private const int _debugStrokeThickness = 8;
		private Paint? _debugBoundsFill;
		private Paint? _debugBoundsStroke;
		private static readonly Random _debugColorRandomizer = new Random(42);
		private static readonly byte[] _debugColor = new byte[3];

		partial void InitializePartial()
		{
			_debugColorRandomizer.NextBytes(_debugColor);
			_debugBoundsStroke = new Paint { Color = new Android.Graphics.Color(_debugColor[0], _debugColor[1], _debugColor[2], (byte)255), StrokeWidth = _debugStrokeThickness };
			_debugBoundsFill = new Paint { Color = new Android.Graphics.Color(_debugColor[0], _debugColor[1], _debugColor[2], (byte)64) };

			_debugBoundsStroke.SetStyle(Paint.Style.Stroke);
			_debugBoundsFill.SetStyle(Paint.Style.Fill);
		}
#endif

		//// Note: This is static to allow sub-class visual to use it without setting it to internal which would be confusing for external user.
		////		 If you need to render a Visual onto a Canvas, use Compositor.Render(visual, canvas).
		//private protected static void Render(Visual visual, Canvas canvas)
		//	=> visual.Render(canvas);

		///// <summary>
		///// Request to this Visual to render itself on the given canvas.
		///// </summary>
		///// <remarks>This method is invoked when we explicitly invoke the native Draw() method on a UIElement, or when drawing children.</remarks>
		//private void Render(Canvas canvas)
		//{
		//	// Note: We don't commit, either it has been done by the compositor / parent,
		//	//		 either it's from an external request (like taking a screen-shot), and drawing the current state is sufficient.

		//	Render();

		//	canvas.DrawRenderNode(_node);
		//}

		/// <summary>
		/// WARNING: This method is most probably NOT what you want!
		/// This is an **internal method for the composition engine** to draw either the root element, either a child element. <br />
		/// 
		/// To externally render a random element, use Compositor.Render(visual, canvas).
		/// </summary>
		/// <param name="canvas">The canvas on which this visual has to be draw.</param>
		/// <remarks>This method does not commit the changes.</remarks>
		internal void DrawOn(Canvas canvas)
		{
			Render();

			canvas.DrawRenderNode(_node);
		}


		/// <summary>
		/// WARNING: This method is most probably NOT what you want!
		/// This is an **internal method for the composition engine** to draw the root element. <br />
		/// 
		/// To externally render a random element, use Compositor.Render(visual, canvas).
		/// </summary>
		/// <param name="canvas">The canvas on which this visual has to be draw.</param>
		/// <remarks>This method does not commit the changes.</remarks>
		internal void DrawOn(RenderNode rootNode)
		{
			if (Edit(rootNode) is { } session)
			{
				try
				{
					DrawOn(session.Canvas);
				}
				catch (Exception)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"Failed to render visual {Comment}");
					}
				}
				finally
				{
					session.Dispose();
				}
			}
		}

		//internal void RenderTo(RenderNode rootNode)
		//{
		//	if (Edit(rootNode) is { } session)
		//	{
		//		try
		//		{
		//			rootNode.
		//		}
		//		catch (Exception)
		//		{
		//			if (this.Log().IsEnabled(LogLevel.Error))
		//			{
		//				this.Log().Error($"Failed to render visual {Comment}");
		//			}
		//		}
		//		finally
		//		{
		//			session.Dispose();
		//		}
		//	}
		//}

		//private protected void RenderChild(Visual visual, Canvas canvas)
		//{
		//	// Note: We don't commit, either it has been done by the compositor / parent,
		//	//		 either it's from an external request (like taking a screen-shot), and drawing the current state is sufficient.

		//	visual.Render();

		//	canvas.DrawRenderNode(visual._node);
		//}

		partial void RenderPartial()
		{
			//	Update();
			//}

			///// <summary>
			///// Requests to this Visual to apply its pending changes to its internal native RenderNode.
			///// On Android we have a 2 stages process in order to have a dedicated CompositorThread:
			/////		The updates are pushed to the native node only when a Frame is about to be rendered,
			/////		avoiding an update while the node is being rendered on the screen.
			///// </summary>
			//private void Update()
			//{
			//var (needsIndependentRender, needsDependentRender) = Kind switch
			//{
			//	VisualKind.UnknownNativeView => (true, true),
			//	VisualKind.NativeIndependent => (true, true),

			//	//(_, VisualDirtyState.Dependent) => (true, true),
			//	//(_, VisualDirtyState.Independent) => (true, false),
			//	_ => (IsDirty(VisualDirtyState.Independent), IsDirty(VisualDirtyState.Dependent))
			//};

			var (needsIndependentRender, needsDependentRender) = (true, true);

			if (needsIndependentRender)
			{
				RenderIndependent();
			}

			if (needsDependentRender)
			{
				RenderDependent();
			}

			//switch (Kind)
			//{
			//	case VisualKind.UnknownNativeView:
			//	case VisualKind.NativeIndependent:
			//		Invalidate(VisualDirtyState.Dependent);
			//		break;
			//}

			//ClearDirtyState();
		}

		private protected void RenderIndependent()
		{
			Reset(VisualDirtyState.Independent);

			RenderIndependent(_node);
		}

		/// <summary>
		/// Push the independent properties of this visual into the native RenderNode.
		/// </summary>
		/// <param name="node">The native node to sync.</param>
		/// <remarks>This might be invoked either on the UIThread, either on the compositor thread.</remarks>
		private protected virtual void RenderIndependent(RenderNode node)
		{
			var point = Offset;
			var size = Size;

			//node.SetPosition((int)point.X, (int)point.Y, (int)(point.X + size.X), (int)(point.Y + size.Y));
			node.SetPosition(new Windows.Foundation.Rect(point.X, point.Y, size.X, size.Y));
			//node.SetClipToBounds(false);
			//node.SetClipToOutline(false);
		}

		/// <summary>
		/// Request to this visual to draw its dependent content onto the native canvas.
		/// </summary>
		/// <remarks>This might be invoked either on the UIThread, either on the compositor thread.</remarks>
		private protected void RenderDependent()
		{
			if (Edit(_node) is { } session)
			{
				try
				{
					Reset(VisualDirtyState.Dependent);

					//session.Canvas.DrawColor(Android.Graphics.Color.Transparent, BlendMode.Clear!);
					RenderDependent(session.Canvas);
				}
				catch (Exception)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"Failed to render visual '{Comment}'");
					}
				}
				finally
				{
					session.Dispose();
				}
			}
		}

		/// <summary>
		/// Draw the dependent content of this visual onto a native canvas.
		/// </summary>
		/// <param name="canvas">The native canvas to draw on.</param>
		/// <remarks>This might be invoked either on the UIThread, either on the compositor thread.</remarks>
		private protected virtual void RenderDependent(Canvas canvas)
		{
			//canvas.DrawARGB(128, 255, 0, 0);
#if DRAW_DRAW_BOUNDS
			//canvas.DrawARGB(128, 255, 0, 0);
			//canvas.DrawRect(
			//	0,
			//	0,
			//	255,
			//	255,
			//	_debugBoundsFill!);

			//var bounds = new Rect(
			//	(int)_render._offset.X,
			//	(int)_render._offset.Y,
			//	(int)_render._size.X,
			//	(int)_render._size.Y);

			canvas.DrawRect(
				_render._offset.X,
				_render._offset.Y,
				_render._size.X,
				_render._size.Y,
				_debugBoundsFill!);

			var halfStrokeThickness = _debugStrokeThickness / 2.0;
			canvas.DrawRect(
				(float)Math.Min(_render._offset.X + halfStrokeThickness, _render._size.X),
				(float)Math.Min(_render._offset.Y + halfStrokeThickness, _render._size.Y),
				(float)Math.Max(_render._size.X - halfStrokeThickness, 0),
				(float)Math.Max(_render._size.Y - halfStrokeThickness, 0),
				_debugBoundsStroke!);

			canvas.DrawLine(
				_render._offset.X,
				_render._offset.Y,
				_render._offset.X + _render._size.X,
				_render._offset.Y + _render._size.Y,
				_debugBoundsStroke!);
			canvas.DrawLine(
				_render._offset.X,
				_render._offset.Y + _render._size.Y, 
				_render._offset.X + _render._size.X,
				_render._offset.Y,
				_debugBoundsStroke!);
#endif
		}

		internal static DrawingSession? Edit(RenderNode node)
		{
			try
			{
				return new DrawingSession(node, node.BeginRecording());
			}
			catch (Java.Lang.IllegalStateException)
			{
				// Cannot start recording (most probably recording is already running somehow).
				if (node.Log().IsEnabled(LogLevel.Error))
				{
					node.Log().Error("Failed to begin frame rendering.");
				}

				return default;
			}
		}

		internal readonly struct DrawingSession : IDisposable
		{
			private readonly RenderNode _node;

			public RecordingCanvas Canvas { get; }

			public DrawingSession(RenderNode node, RecordingCanvas canvas)
			{
				_node = node;
				Canvas = canvas;
			}

			/// <inheritdoc />
			public void Dispose()
				=> _node.EndRecording();
		}
	}
}
#endif
