#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media
{
	public sealed partial class RenderingEventArgs
	{
		private readonly IReadOnlyList<(Window Window, object Data)>? _frameData;

		internal RenderingEventArgs(TimeSpan renderingTime, IReadOnlyList<(Window Window, object Data)>? frameData = null)
		{
			RenderingTime = renderingTime;
			_frameData = frameData;
		}

		public TimeSpan RenderingTime { get; }

		/// <summary>
		/// The frames recorded for this rendering pass, as pairs of a window and its opaque
		/// frame data, when available. Intended for internal and advanced scenarios.
		/// </summary>
		public IReadOnlyList<(Window Window, object Data)>? FrameData
		{
			get
			{
				FrameDataAccessed = true;
				return _frameData;
			}
		}

		internal bool FrameDataAccessed { get; private set; }
	}
}
