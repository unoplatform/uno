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

#if __CROSSRUNTIME__
		/// <inheritdoc />
		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnBackgroundChanged(e);
		}
#endif
	}
}
