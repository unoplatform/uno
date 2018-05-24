using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Core
{
    public sealed partial class CoreDispatcher
    {
		public static bool HasThreadAccessOverride { get; set; } = true;
		 
		private bool GetHasThreadAccess() => HasThreadAccessOverride;

		public static CoreDispatcher Main { get; } = new CoreDispatcher();
	}
}