using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.UI;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml.Media
{
	public partial class CompositionTarget
	{
		private static Action _renderingActiveChanged;
		private static bool _isRenderingActive;
		private static Timer _renderTimer;

		public static event EventHandler<object> Rendering
		{
			add
			{
				NativeDispatcher.CheckThreadAccess();

				var currentlyRaisingEvents = NativeDispatcher.Main.IsRendering;
				NativeDispatcher.Main.Rendering += value;
				NativeDispatcher.Main.RenderingEventArgsGenerator ??= (d => new RenderingEventArgs(d));

				IsRenderingActive = NativeDispatcher.Main.IsRendering;

				if (!currentlyRaisingEvents)
				{
					NativeDispatcher.Main.WakeUp();
				}
			}
			remove
			{
				NativeDispatcher.CheckThreadAccess();

				NativeDispatcher.Main.Rendering -= value;

				IsRenderingActive = NativeDispatcher.Main.IsRendering;
			}
		}

		internal static event Action RenderingActiveChanged
		{
			add => _renderingActiveChanged += value;
			remove => _renderingActiveChanged -= value;
		}

		/// <summary>
		/// Use a generic frame timer instead of the native one, generally 
		/// in the context of desktop targets.
		/// </summary>
		internal static bool UseGenericTimer { get; set; }

		/// <summary>
		/// Determines if the CompositionTarget rendering is active.
		/// </summary>
		internal static bool IsRenderingActive
		{
			get => _isRenderingActive;
			set
			{
				if (value != _isRenderingActive)
				{
					_isRenderingActive = value;
					_renderingActiveChanged?.Invoke();

					TryUpdateGenericTimer();
				}
			}
		}

		private static void TryUpdateGenericTimer()
		{
			if (UseGenericTimer)
			{
				if (_isRenderingActive)
				{
					_renderTimer ??= new Timer(_ => NativeDispatcher.Main.DispatchRendering());
					_renderTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1 / FeatureConfiguration.CompositionTarget.FrameRate));
				}
				else
				{
					_renderTimer?.Change(Timeout.Infinite, Timeout.Infinite);
					_renderTimer?.Dispose();
					_renderTimer = null;
				}
			}
		}
	}
}
