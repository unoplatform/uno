#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Effects;
using Windows.UI;
using System.Linq;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Dispatching;
using Windows.ApplicationModel.Core;
using Windows.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class Compositor : global::System.IDisposable
	{
		private static Lazy<Compositor> _sharedCompositorLazy = new(() => new());

		private static Lazy<CompositionEasingFunction> _defaultEasingFunction = new(() => new CubicBezierEasingFunction(GetSharedCompositor(), new(0.41f, 0.52f), new(0.0f, 0.94f)));

		static Compositor()
		{
			Initialize();
		}

		static partial void Initialize();

		public Compositor()
		{
		}

		// https://github.com/dotnet/runtime/blob/c52fd37cc835a13bcfa9a64fdfe7520809a75345/src/libraries/System.Private.CoreLib/src/System/Diagnostics/Stopwatch.cs#L27
		private static readonly double s_tickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

		// Callsites usually use this with TimeSpan ticks. We need to multiply by s_tickFrequency to get it right.
		// NOTE: s_tickFrequency is likely 1 on Windows, but not on Linux.
		// See https://github.com/dotnet/runtime/blob/c52fd37cc835a13bcfa9a64fdfe7520809a75345/src/libraries/System.Private.CoreLib/src/System/Diagnostics/Stopwatch.cs#L157
		public long TimestampInTicks => unchecked((long)(Stopwatch.GetTimestamp() * s_tickFrequency));

		internal static Compositor GetSharedCompositor() => _sharedCompositorLazy.Value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static CompositionEasingFunction GetDefaultEasingFunction() => _defaultEasingFunction.Value;

		public ContainerVisual CreateContainerVisual()
			=> new ContainerVisual(this);

		public SpriteVisual CreateSpriteVisual()
			=> new SpriteVisual(this);

		public CompositionColorBrush CreateColorBrush()
			=> new CompositionColorBrush(this);

		public CompositionColorBrush CreateColorBrush(Color color)
			=> new CompositionColorBrush(this)
			{
				Color = color
			};

		public ScalarKeyFrameAnimation CreateScalarKeyFrameAnimation()
			=> new ScalarKeyFrameAnimation(this);

		public CompositionScopedBatch CreateScopedBatch(CompositionBatchTypes batchType)
			=> new CompositionScopedBatch(this, batchType);

		public ShapeVisual CreateShapeVisual()
			=> new ShapeVisual(this);

#if __SKIA__
		internal BorderVisual CreateBorderVisual()
			=> new BorderVisual(this);
#endif

		public CompositionSpriteShape CreateSpriteShape()
			=> new CompositionSpriteShape(this);

		public CompositionSpriteShape CreateSpriteShape(CompositionGeometry geometry)
			=> new CompositionSpriteShape(this, geometry);

		public CompositionContainerShape CreateContainerShape()
			=> new CompositionContainerShape(this);

		public CompositionPathGeometry CreatePathGeometry()
			=> new CompositionPathGeometry(this);

		public CompositionPathGeometry CreatePathGeometry(CompositionPath path)
			=> new CompositionPathGeometry(this, path);

		public CompositionEllipseGeometry CreateEllipseGeometry()
			=> new CompositionEllipseGeometry(this);

		public CompositionLineGeometry CreateLineGeometry()
			=> new CompositionLineGeometry(this);

		public CompositionRectangleGeometry CreateRectangleGeometry()
			=> new CompositionRectangleGeometry(this);

		public CompositionRoundedRectangleGeometry CreateRoundedRectangleGeometry()
			=> new CompositionRoundedRectangleGeometry(this);

		public CompositionSurfaceBrush CreateSurfaceBrush()
			=> new CompositionSurfaceBrush(this);

		public CompositionSurfaceBrush CreateSurfaceBrush(ICompositionSurface surface)
			=> new CompositionSurfaceBrush(this, surface);

		public CompositionGeometricClip CreateGeometricClip()
			=> new CompositionGeometricClip(this);

		public CompositionGeometricClip CreateGeometricClip(CompositionGeometry geometry)
			=> new CompositionGeometricClip(this) { Geometry = geometry };

		public CompositionPropertySet CreatePropertySet()
			=> new CompositionPropertySet(this);

		public InsetClip CreateInsetClip()
			=> new InsetClip(this);

		public InsetClip CreateInsetClip(float leftInset, float topInset, float rightInset, float bottomInset)
			=> new InsetClip(this)
			{
				LeftInset = leftInset,
				TopInset = topInset,
				RightInset = rightInset,
				BottomInset = bottomInset
			};

		public RectangleClip CreateRectangleClip()
			=> new RectangleClip(this);

		public RectangleClip CreateRectangleClip(float left, float top, float right, float bottom)
			=> new RectangleClip(this)
			{
				Left = left,
				Top = top,
				Right = right,
				Bottom = bottom
			};

		public RectangleClip CreateRectangleClip(
			float left,
			float top,
			float right,
			float bottom,
			Vector2 topLeftRadius,
			Vector2 topRightRadius,
			Vector2 bottomRightRadius,
			Vector2 bottomLeftRadius)
			=> new RectangleClip(this)
			{
				Left = left,
				Top = top,
				Right = right,
				Bottom = bottom,
				TopLeftRadius = topLeftRadius,
				TopRightRadius = topRightRadius,
				BottomRightRadius = bottomRightRadius,
				BottomLeftRadius = bottomLeftRadius
			};

		public CompositionLinearGradientBrush CreateLinearGradientBrush()
			=> new CompositionLinearGradientBrush(this);

		public CompositionRadialGradientBrush CreateRadialGradientBrush()
			=> new CompositionRadialGradientBrush(this);

		public CompositionColorGradientStop CreateColorGradientStop()
			=> new CompositionColorGradientStop(this);

		public CompositionColorGradientStop CreateColorGradientStop(float offset, Color color)
			=> new CompositionColorGradientStop(this)
			{
				Offset = offset,
				Color = color
			};

		public CompositionViewBox CreateViewBox()
			=> new CompositionViewBox(this);

		public RedirectVisual CreateRedirectVisual()
			=> new RedirectVisual(this);

		public RedirectVisual CreateRedirectVisual(Visual source)
			=> new RedirectVisual(this) { Source = source };

		public CompositionVisualSurface CreateVisualSurface()
			=> new CompositionVisualSurface(this);

		public CompositionMaskBrush CreateMaskBrush()
			=> new CompositionMaskBrush(this);

		public CompositionNineGridBrush CreateNineGridBrush()
			=> new CompositionNineGridBrush(this);

		public ExpressionAnimation CreateExpressionAnimation(string expression)
			=> new ExpressionAnimation(this) { Expression = expression };

		public ExpressionAnimation CreateExpressionAnimation()
			=> new ExpressionAnimation(this);

		public BooleanKeyFrameAnimation CreateBooleanKeyFrameAnimation()
			=> new BooleanKeyFrameAnimation(this);

		public Vector2KeyFrameAnimation CreateVector2KeyFrameAnimation()
			=> new Vector2KeyFrameAnimation(this);

		public Vector3KeyFrameAnimation CreateVector3KeyFrameAnimation()
			=> new Vector3KeyFrameAnimation(this);

		public Vector4KeyFrameAnimation CreateVector4KeyFrameAnimation()
			=> new Vector4KeyFrameAnimation(this);

		// Uno currently does not buffer composition commits, so RequestCommitAsync completes immediately.
		// This still satisfies callers that schedule cleanup work after a commit fence (e.g. AnimatedVisualPlayer).
		public IAsyncAction RequestCommitAsync()
			=> Task.CompletedTask.AsAsyncAction();

		// Tracks the active CompositionScopedBatch stack. CompositionScopedBatch hooks animations
		// started while a batch is active so its Completed event fires when those animations
		// actually stop (rather than synchronously when End() is called). The stack supports
		// nested batches, though only the innermost batch tracks new animations.
		private readonly Stack<CompositionScopedBatch> _scopedBatchStack = new();

		internal void RegisterScopedBatch(CompositionScopedBatch batch) => _scopedBatchStack.Push(batch);

		internal void UnregisterScopedBatch(CompositionScopedBatch batch)
		{
			if (_scopedBatchStack.Count > 0 && _scopedBatchStack.Peek() == batch)
			{
				_scopedBatchStack.Pop();
				return;
			}

			// Defensive: if batches were ended out of order, remove this one wherever it is.
			var preserved = new Stack<CompositionScopedBatch>();
			while (_scopedBatchStack.Count > 0)
			{
				var top = _scopedBatchStack.Pop();
				if (top == batch)
				{
					break;
				}
				preserved.Push(top);
			}
			while (preserved.Count > 0)
			{
				_scopedBatchStack.Push(preserved.Pop());
			}
		}

		internal void InvalidateRender(Visual visual) => InvalidateRenderPartial(visual);
		public CompositionBackdropBrush CreateBackdropBrush()
			=> new CompositionBackdropBrush(this);

		public CompositionEffectFactory CreateEffectFactory(IGraphicsEffect graphicsEffect)
			=> new CompositionEffectFactory(this, graphicsEffect);

		public CompositionEffectFactory CreateEffectFactory(IGraphicsEffect graphicsEffect, IEnumerable<string> animatableProperties)
			=> new CompositionEffectFactory(this, graphicsEffect, animatableProperties);

		public CubicBezierEasingFunction CreateCubicBezierEasingFunction(Vector2 controlPoint1, Vector2 controlPoint2)
			=> new(this, controlPoint1, controlPoint2);

		public LinearEasingFunction CreateLinearEasingFunction()
			=> new(this);

		public StepEasingFunction CreateStepEasingFunction()
			=> new(this);

		public StepEasingFunction CreateStepEasingFunction(int stepCount)
			=> new(this, stepCount);

		partial void InvalidateRenderPartial(Visual visual);

		private Dictionary<CompositionAnimation, ICompositionTarget> _runningAnimations = new();
		private Dictionary<ICompositionTarget, int> _runningTargets = new();
		private LinkedList<ColorBrushTransitionState> _backgroundTransitions = new();
	#if PRINT_FRAME_TIMES
		private int _frameNumber;
	#endif

		static partial void Initialize()
		{
			UnoSkiaApi.Initialize();
		}

		/// <summary>
		/// Whether the scene is rasterized on the CPU rather than by a GPU-backed surface.
		/// Set by the active render backend once its renderer is selected; null until then.
		/// Consulted while recording (e.g. by effect brushes to generate filters the target
		/// surface can rasterize) and temporarily overridden by RenderTargetBitmap.
		/// </summary>
		internal bool? IsSoftwareRenderer { get; set; }

		internal static bool SkipVisualTreePainting { get; set; }

		internal bool IsAnimating => _runningAnimations.Count > 0;

		internal void RegisterAnimation(CompositionAnimation animation, CompositionObject host)
		{
			// Feed the animation into the innermost active scoped batch so its Completed event waits
			// for the animation to actually stop instead of firing synchronously when batch.End() is
			// called.
			if (animation is KeyFrameAnimation keyFrameAnimation && _scopedBatchStack.Count > 0)
			{
				_scopedBatchStack.Peek().TrackAnimation(keyFrameAnimation);
			}

			if (!animation.IsTrackedByCompositor)
			{
				return;
			}

			// Resolve the CompositionTarget that needs invalidation. For Visuals it's the visual's
			// own target; for a CompositionPropertySet it's the owning Visual's target so animations
			// on `someVisual.Properties.Foo` still get ticked. A property set created standalone via
			// Compositor.CreatePropertySet (e.g. AnimatedIcon's progress property set) must therefore
			// have its Owner set to a Visual — AnimatedIcon does this before starting its animations.
			// Without an owning Visual there is no target and the animation never ticks.
			ICompositionTarget? target = host switch
			{
				Visual visual => visual.CompositionTarget,
				CompositionPropertySet { Owner: Visual ownerVisual } => ownerVisual.CompositionTarget,
				_ => null,
			};

			if (target is null)
			{
				return;
			}

			_runningAnimations.Add(animation, target);

			if (_runningTargets.TryGetValue(target, out int count))
			{
				_runningTargets[target] = count + 1;
			}
			else
			{
				_runningTargets[target] = 1;
				target.RequestNewFrame();
			}

			if (this.Log().IsTraceEnabled())
			{
				this.Log().Trace($"Register running targets {target.GetHashCode():X8}={count} Animations={_runningAnimations.Count}");
			}
		}

		internal void UnregisterAnimation(CompositionAnimation animation, CompositionObject visual)
		{
			if (animation.IsTrackedByCompositor)
			{
				if (_runningAnimations.TryGetValue(animation, out var target))
				{
					_runningAnimations.Remove(animation);

					if (_runningTargets.TryGetValue(target, out int count))
					{
						if (this.Log().IsTraceEnabled())
						{
							this.Log().Trace($"Unregister running targets {target.GetHashCode():X8}={count - 1} Animations={_runningAnimations.Count}");
						}

						if (count == 1)
						{
							_runningTargets.Remove(target);
						}
						else
						{
							_runningTargets[target] = count - 1;
						}
					}
				}
				else
				{
					if (this.Log().IsDebugEnabled())
					{
						this.Log().Debug($"Cannot unregister unknown animation");
					}
				}
			}
		}

		internal void DeactivateBackgroundTransition(BorderVisual visual)
		{
			for (var current = _backgroundTransitions.First; current != null; current = current.Next)
			{
				var transition = current.Value;
				var transitionVisual = transition.Visual;

				if (transitionVisual == visual)
				{
					current.Value = transition with { IsActive = false };
					break;
				}
			}
		}

		internal void RegisterBackgroundTransition(BorderVisual visual, Color fromColor, Color toColor, TimeSpan duration)
		{
			var start = TimestampInTicks;
			var end = start + duration.Ticks;

			for (var current = _backgroundTransitions.First; current != null; current = current.Next)
			{
				var transition = current.Value;
				var transitionVisual = transition.Visual;

				if (transition.Visual == visual)
				{
					// when the background changes when already in a transition, the new transition
					// picks up from where the preexisting transition stopped UNLESS the preexisting
					// transition was inactive (i.e. an animation started during the transition.
					// In that case, just reactivate the preexisting transition.

					if (!transition.IsActive)
					{
						current.Value = transition with { IsActive = true };
						return;
					}

					fromColor = transition.CurrentColor;
					_backgroundTransitions.Remove(current);
					break;
				}
			}

			_backgroundTransitions.AddLast(new ColorBrushTransitionState(visual, fromColor, toColor, start, end, true));
		}

		internal bool TryGetEffectiveBackgroundColor(CompositionSpriteShape shape, out Color color)
		{
			foreach (var transition in _backgroundTransitions)
			{
				if (transition.Visual.IsMyBackgroundShape(shape))
				{
					if (transition.IsActive)
					{
						color = transition.CurrentColor;
						return true;
					}
					else
					{
						break;
					}
				}
			}

			color = default;
			return false;
		}

		internal void RenderRootVisual(SKCanvas canvas, ContainerVisual rootVisual, SKPath? damage = null)
		{
			if (rootVisual is null)
			{
				throw new ArgumentNullException(nameof(rootVisual));
			}

			foreach (var animation in _runningAnimations.Keys.ToArray())
			{
				try
				{
					animation.RaiseAnimationFrame();
				}
				catch (Exception e)
				{
					// A single animation's expression must never wedge the render loop. Its failure is
					// deterministic, so stop it rather than throwing every frame and stalling rendering.
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error("Stopping animation after an unhandled evaluation error.", e);
					}
					animation.Stop();
				}
			}

	#if PRINT_FRAME_TIMES
			var start = Stopwatch.GetTimestamp();
	#endif
			// Skip only the paint walk: animations above still tick and transitions/frame
			// re-requests below still run, so the scene stays live without producing pixels.
			if (!SkipVisualTreePainting)
			{
				rootVisual.RenderRootVisual(canvas, null, damage);
			}
	#if PRINT_FRAME_TIMES
			var span = Stopwatch.GetElapsedTime(start);
			Console.WriteLine($"Rendered frame {_frameNumber++} in {span.TotalMilliseconds}ms");
	#endif

			var transitionsCount = _backgroundTransitions.Count;
			for (var current = _backgroundTransitions.First; current != null; current = current.Next)
			{
				var transition = current.Value;
				var transitionVisual = transition.Visual;

				transitionVisual.InvalidatePaint();

				if (TimestampInTicks >= transition.EndTimestamp)
				{
					_backgroundTransitions.Remove(current);
				}
			}

			if (_runningAnimations.Count > 0 || transitionsCount > 0)
			{
				rootVisual.CompositionTarget?.RequestNewFrame();
			}
		}

		partial void InvalidateRenderPartial(Visual visual)
		{
			visual.SetMatrixDirty(); // TODO: only invalidate matrix when specific properties are changed
			visual.InvalidatePaint(); // TODO: only repaint when "dependent" properties are changed
			visual.CompositionTarget?.RequestNewFrame();
		}
	}
}
