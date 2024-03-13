using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Core
{
	public sealed partial class CoreDispatcher
	{
		/// <summary>
		/// Provide a action that will delegate the dispatch of CoreDispatcher work
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Action<Action> DispatchOverride
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		/// <summary>
		/// Provide a action that will delegate the dispatch of CoreDispatcher work
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Func<bool> HasThreadAccessOverride
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}
	}
}
