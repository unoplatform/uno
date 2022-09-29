#nullable disable

using System;

namespace Uno.Extensions
{
	public class HtmlCustomEventArgs : EventArgs
	{
		public string Detail { get; }

		public HtmlCustomEventArgs(string detail)
		{
			Detail = detail;
		}
	}
}