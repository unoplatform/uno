using System;
using System.Collections.Generic;
using System.Text;
using Android.Views;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Controls.Legacy
{
	internal partial class ListViewHeader : ContentControl
	{
		public ListViewHeader()
		{

		}

		/// <remarks>
		/// Ensure that the ContentControl will create its chidren even
		/// if it has no parent view. This is critical for the recycling panels,
		/// where the content is databound before being assigned to its
		/// parent and displayed.
		/// </remarks>
		protected override bool CanCreateTemplateWithoutParent { get; } = true;
	}
}

