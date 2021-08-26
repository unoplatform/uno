using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Windows.UI.Xaml.Media.Animation
{
	internal static class RenderingLoopAnimatorMetadataIdProvider
	{
		private static long _next;

		public static long Next() => Interlocked.Increment(ref _next);
	}
}
