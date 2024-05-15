using System;
using Windows.System;

namespace Uno.Buffers
{
	/// <summary>
	/// Platform features provider for <see cref="ArrayPool{T}"/>
	/// </summary>
	/// <remarks>
	/// Used primarily to allow for deterministic testing of ArrayPool trimming features.
	/// </remarks>
	internal interface IArrayPoolPlatformProvider
	{
		/// <summary>
		/// Determines if memory manager can be used
		/// </summary>
		bool CanUseMemoryManager { get; }

		/// <summary>
		/// Determine current memory pressure
		/// </summary>
		AppMemoryUsageLevel AppMemoryUsageLevel { get; }

		/// <summary>
		/// Gets the current time
		/// </summary>
		TimeSpan Now { get; }

		/// <summary>
		/// Registers a callback to be called when GC triggerd
		/// </summary>
		/// <param name="callback">Function called with <paramref name="arrayPool"/> as the first parameter</param>
		/// <param name="arrayPool">instance to be provided when invoking <paramref name="callback"/></param>
		void RegisterTrimCallback(Func<object, bool> callback, object arrayPool);
	}
}
