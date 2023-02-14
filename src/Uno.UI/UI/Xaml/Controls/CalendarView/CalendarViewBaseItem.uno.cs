using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	partial class CalendarViewBaseItem
	{
		/// <inheritdoc />
		internal override bool IsViewHit()
			=> true;

#if __WASM__
		/// <inheritdoc />
		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			// We prevent propagation to the parent, so we can set the right background based on the IsToday, IsSelected, etc.
			// base.OnBackgroundChanged(e);
		}
#endif
	}
}
