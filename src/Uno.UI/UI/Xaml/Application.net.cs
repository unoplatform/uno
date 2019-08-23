using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	public partial class Application
	{
		public Application()
		{
			Current = this;
		}

		/// <summary>
		/// Ensure that application exists, for unit tests. 
		/// </summary>
		internal static void EnsureApplication()
		{
			if (Current == null)
			{
				new Application();
			}
		}
	}
}
