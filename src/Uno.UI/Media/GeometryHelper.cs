using Uno.Extensions;
using Uno.Media;
using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Media
{
	public static class GeometryHelper
	{
		private static Func<Action<StreamGeometryContext>, StreamGeometry> _build = CachedBuild;

		/// <summary>
		/// Build a StreamGeometry based on the specified <paramref name="contextAction"/>. If the <paramref name="contextAction"/> 
		/// is built from a C# lambda, then the StreamGeometry instance will be cached, allowing native paths to be reused.
		/// </summary>
		/// <param name="contextAction">A geometry action generation.</param>
		/// <returns></returns>
		public static StreamGeometry Build(Action<StreamGeometryContext> contextAction)
		{
			// The use of AsWeakMemoized will attach the StreamGeometry instance to 
			// "contextAction", therefore caching it for the lifetime of the app.

			return _build.AsWeakMemoized(contextAction)();
		}

		private static StreamGeometry CachedBuild(Action<StreamGeometryContext> contextAction)
		{
			var streamGeometry = new StreamGeometry();

			using (StreamGeometryContext context = streamGeometry.Open())
			{
				contextAction(context);
			}

			return streamGeometry;
		}
	}
}
