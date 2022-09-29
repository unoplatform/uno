#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Controls.Legacy
{
	/// <summary>
	/// A ListViewBaseLayout Template, similar to ItemsPanel for the "real" ListView.
	/// </summary>
	public class ListViewBaseLayoutTemplate
	{
		private Func<ListViewBaseLayout> _template;

		public ListViewBaseLayoutTemplate(Func<ListViewBaseLayout> template)
		{
			this._template = template;
		}

		public static implicit operator ListViewBaseLayoutTemplate(Func<ListViewBaseLayout> template)
		{
			return new ListViewBaseLayoutTemplate(template);
		}

		public ListViewBaseLayout LoadLayout()
		{
			return _template();
		}
	}
}
