#pragma warning disable 0618 // For SetBackgroundDrawable

using Uno.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class SpinnerEx : Android.Widget.Spinner
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
