#pragma warning disable 0618 // For SetBackgroundDrawable

using Uno.UI;
using System;
using System.Collections.Generic;
using System.Text;

using Android.Widget;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class SpinnerEx : Spinner
	{
		public event EventHandler<int> IndexClicked;

		public SpinnerEx() : base(ContextHelper.Current)
		{
			SetBackgroundDrawable(null); // removes the spinner arrow
		}

		public override void SetSelection(int index)
		{
			base.SetSelection(index);

			IndexClicked?.Invoke(this, index);
		}
	}
}
