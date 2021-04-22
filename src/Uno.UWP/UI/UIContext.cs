#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Composition;

namespace Windows.UI
{
	public partial class UIContext
	{
		[ThreadStatic]
		private static UIContext? _current;

		/// <summary>
		/// Gets the context for the current UI Thread.
		/// </summary>
		internal static UIContext GetForCurrentThread()
			=> _current ??= new UIContext();

		private UIContext()
		{
		}

		internal Compositor Compositor { get; } = new Compositor();
	}
}
