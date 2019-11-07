using System;
using System.Collections.Generic;
using System.Text;
using AndroidX.RecyclerView.Widget;

namespace Windows.UI.Xaml.Controls
{
	internal class UnoViewHolder : RecyclerView.ViewHolder
	{
		public UnoViewHolder(ContentControl itemView) : base(itemView) { }

		/// <summary>
		/// Has the ItemView been detached from the window? This is a public mirror of the internal ViewHolder.isTmpDetached() method.
		/// </summary>
		public bool IsDetached { get; set; }

#if DEBUG
		// This is as dirty as it looks. We only use it as a sanity check and only in debug.
		public bool IsDetachedPrivate => ToString().Contains("tmpDetached");
#endif
	}
}
