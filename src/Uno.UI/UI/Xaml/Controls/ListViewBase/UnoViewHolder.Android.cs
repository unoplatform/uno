using System;
using System.Collections.Generic;
using System.Text;
using Android.Support.V7.Widget;

namespace Windows.UI.Xaml.Controls
{
	internal class UnoViewHolder : RecyclerView.ViewHolder
	{
		public UnoViewHolder(ContentControl itemView) : base(itemView) { }

		/// <summary>
		/// Has the ItemView been detached from the window?
		/// </summary>
		public bool IsDetached { get; set; }
	}
}
