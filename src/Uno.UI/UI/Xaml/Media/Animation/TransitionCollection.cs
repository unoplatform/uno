using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	/// <summary>
	/// TransitionCollection : Based on WinRT TransitionCollection
	/// (https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.media.animation.transitioncollection.aspx)
	/// </summary>
	public partial class TransitionCollection : List<Transition>, IList<Transition>, IEnumerable<Transition>
	{
#if HAS_UNO_WINUI
		public new IEnumerator<Transition> GetEnumerator() => base.GetEnumerator();
#endif
	}
}
