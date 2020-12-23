using System;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataRequest
	{
		public DataPackage Data { get; set; }

		public DateTimeOffset Deadline { get; }

		public void FailWithDisplayText(string value)
		{
			throw new NotImplementedException();
		}

		public DataRequestDeferral GetDeferral()
		{
			throw new NotImplementedException();
		}
	}
}
