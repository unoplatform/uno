#nullable enable

#if HAS_SKOTTIE

using System;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using SkiaSharp;
using SkiaSharp.SceneGraph;
using Uno.WinUI.Graphics2DSK;

#if HAS_UNO_WINUI
namespace CommunityToolkit.WinUI.Lottie
#else
namespace Microsoft.Toolkit.Uwp.UI.Lottie
#endif
{
	partial class LottieVisualSourceBase
	{
		private partial IAnimatedVisual2 CreatePendingAnimatedVisual(Compositor compositor)
			=> LottieAnimatedVisual.CreatePending(compositor);

		private partial IAnimatedVisual2? TryCreateAnimatedVisualFromJson(Compositor compositor, string animationJson, bool createAnimations, out object diagnostics)
			=> LottieAnimatedVisual.TryCreate(compositor, animationJson, out diagnostics);

		private sealed class LottieAnimatedVisual : IAnimatedVisual2
		{
			private readonly object _gate = new();
			private readonly InvalidationController _invalidationController = new();
			private readonly LottieCanvasElement _canvasElement;
			private readonly Visual _rootVisual;

			private SkiaSharp.Skottie.Animation? _animation;
			private bool _isDisposed;

			private LottieAnimatedVisual(Compositor compositor)
			{
				_canvasElement = new LottieCanvasElement(this);
				ElementCompositionPreview.SetElementVisualCompositor(_canvasElement, compositor);
				_rootVisual = ElementCompositionPreview.GetElementVisual(_canvasElement);
				_rootVisual.Properties.InsertScalar("Progress", 0.0f);
				_rootVisual.Properties.AddContext(_rootVisual, null);
				_invalidationController.Begin();
			}

			public Visual RootVisual => _rootVisual;

			public Vector2 Size { get; private set; }

			public TimeSpan Duration { get; private set; }

			public static LottieAnimatedVisual CreatePending(Compositor compositor)
				=> new(compositor);

			public static IAnimatedVisual2? TryCreate(Compositor compositor, string animationJson, out object diagnostics)
			{
				try
				{
					var animation = CreateAnimation(animationJson);
					if (animation is null)
					{
						diagnostics = new InvalidOperationException("Failed to load animation.");
						return null;
					}

					var visual = new LottieAnimatedVisual(compositor);
					visual.Initialize(animation);
					diagnostics = null!;
					return visual;
				}
				catch (Exception e)
				{
					diagnostics = e;
					return null;
				}
			}

			public void CreateAnimations()
			{
				// Skottie renders directly from the current Progress scalar each frame and does not
				// materialize separate composition animation objects that need to be created up front.
			}

			public void DestroyAnimations()
			{
				// See CreateAnimations(). There are no separate composition-side animations to tear down.
			}

			public void Dispose()
			{
				lock (_gate)
				{
					if (_isDisposed)
					{
						return;
					}

					_isDisposed = true;
					_rootVisual.Properties.RemoveContext(_rootVisual, null);
					_invalidationController.End();
					_animation?.Dispose();
					_animation = null;
				}
			}

			private static SkiaSharp.Skottie.Animation? CreateAnimation(string animationJson)
			{
				using var stream = new Utf8StringStream(animationJson);
				return SkiaSharp.Skottie.Animation.TryCreate(stream, out var animation)
					? animation
					: null;
			}

			private void Initialize(SkiaSharp.Skottie.Animation animation)
			{
				_animation = animation;
				Size = new Vector2(animation.Size.Width, animation.Size.Height);
				Duration = animation.Duration;
				_rootVisual.Size = Size;
				_canvasElement.Invalidate();
			}

			private void Render(SKCanvas canvas)
			{
				lock (_gate)
				{
					if (_isDisposed || _animation is null)
					{
						return;
					}

					canvas.Clear(SKColors.Transparent);

					var progress = GetProgress();
					var frameTime = TimeSpan.FromTicks((long)(Duration.Ticks * progress));
					_animation.SeekFrameTime(frameTime, _invalidationController);
					_animation.Render(canvas, new SKRect(0, 0, _animation.Size.Width, _animation.Size.Height));
					_invalidationController.Reset();
				}
			}

			private float GetProgress()
			{
				return _rootVisual.Properties.TryGetScalar("Progress", out var progress) == CompositionGetValueStatus.Succeeded
					? Math.Clamp(progress, 0.0f, 1.0f)
					: 0.0f;
			}

			private sealed class LottieCanvasElement(LottieAnimatedVisual owner) : SKCanvasElement
			{
				protected override void RenderOverride(SKCanvas canvas, Windows.Foundation.Size area)
					=> owner.Render(canvas);
			}

			private sealed class Utf8StringStream(string text) : Stream
			{
				private readonly Encoder _encoder = Encoding.UTF8.GetEncoder();
				private readonly byte[] _overflow = new byte[4];
				private int _charOffset;
				private int _overflowOffset;
				private int _overflowCount;

				public override bool CanRead => true;

				public override bool CanSeek => false;

				public override bool CanWrite => false;

				public override long Length { get; } = Encoding.UTF8.GetByteCount(text);

				public override long Position { get; set; }

				public override void Flush()
				{
				}

				public override int Read(byte[] buffer, int offset, int count)
					=> Read(buffer.AsSpan(offset, count));

				public override int Read(Span<byte> buffer)
				{
					if (buffer.Length == 0)
					{
						return 0;
					}

					var bytesWritten = 0;
					if (_overflowCount > 0)
					{
						var copied = Math.Min(buffer.Length, _overflowCount);
						_overflow.AsSpan(_overflowOffset, copied).CopyTo(buffer);
						_overflowOffset += copied;
						_overflowCount -= copied;
						Position += copied;
						bytesWritten += copied;

						if (_overflowCount == 0)
						{
							_overflowOffset = 0;
						}

						if (bytesWritten == buffer.Length)
						{
							return bytesWritten;
						}

						buffer = buffer[bytesWritten..];
					}

					if (_charOffset >= text.Length)
					{
						return bytesWritten;
					}

					_encoder.Convert(text.AsSpan(_charOffset), buffer, flush: true, out var charsUsed, out var bytesUsed, out _);
					_charOffset += charsUsed;
					Position += bytesUsed;
					bytesWritten += bytesUsed;

					if (bytesWritten == 0 && _charOffset < text.Length)
					{
						var charCount = char.IsHighSurrogate(text[_charOffset]) && _charOffset + 1 < text.Length ? 2 : 1;
						var runeBytes = Encoding.UTF8.GetBytes(text.AsSpan(_charOffset, charCount), _overflow);
						var copied = Math.Min(buffer.Length, runeBytes);
						_overflow.AsSpan(0, copied).CopyTo(buffer);
						_overflowOffset = copied;
						_overflowCount = runeBytes - copied;
						_charOffset += charCount;
						Position += copied;
						bytesWritten += copied;
					}

					return bytesWritten;
				}

				public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
				{
					cancellationToken.ThrowIfCancellationRequested();
					return ValueTask.FromResult(Read(buffer.Span));
				}

				public override long Seek(long offset, SeekOrigin origin)
					=> throw new NotSupportedException();

				public override void SetLength(long value)
					=> throw new NotSupportedException();

				public override void Write(byte[] buffer, int offset, int count)
					=> throw new NotSupportedException();
			}
		}
	}
}

#endif
