#nullable enable
using System;
using System.Linq;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class OperationCompletedEventArgs
	{
		internal OperationCompletedEventArgs(string? acceptedFormatId, DataPackageOperation operation)
		{
			AcceptedFormatId = acceptedFormatId;
			Operation = operation;
		}

		public string? AcceptedFormatId { get; }

		public DataPackageOperation Operation { get; }
	}
}
