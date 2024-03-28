#pragma warning disable 108 // new keyword hiding
using System;

namespace Windows.UI.Xaml
{
	public partial class UnhandledExceptionEventArgs
	{
		public UnhandledExceptionEventArgs(Exception e, bool fatal)
		{
			Exception = e;
			Fatal = fatal;
			Message = e.Message;
		}

		public bool Handled { get; set; }

		public global::System.Exception Exception
		{
			get;
		}

		public string Message
		{
			get;
		}

		internal bool Fatal { get; }
	}
}
