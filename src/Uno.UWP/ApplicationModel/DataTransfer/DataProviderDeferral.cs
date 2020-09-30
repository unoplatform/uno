#nullable enable

using System;
using System.Linq;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataProviderDeferral
	{
		private readonly Action _complete;

		internal DataProviderDeferral(Action complete)
		{
			_complete = complete;
		}

		public void Complete() => _complete();
	}
}
