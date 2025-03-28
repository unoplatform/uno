using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarViewBaseItem
	{
		/// <inheritdoc />
		internal override bool IsViewHit()
			=> true;

#if __CROSSRUNTIME__
		/// <inheritdoc />
		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
#if __WASM__
			// We prevent propagation to the parent, so we can set the right background based on the IsToday, IsSelected, etc.
			// base.OnBackgroundChanged(e);
#else
			base.OnBackgroundChanged(e);
#endif
		}
#endif
	}
}
