using System.Runtime.InteropServices;

namespace Windows.Foundation
{
	public delegate void DeferralCompletedHandler();

	public sealed partial class Deferral : IClosable
	{
		private readonly DeferralCompletedHandler _handler;

		/// <summary>
		/// Initializes a new Deferral object and specifies a DeferralCompletedHandler
		/// to be called upon completion of the deferral.
		/// </summary>
		/// <param name="handler">A DeferralCompletedHandler to be called upon completion of the deferral.</param>
		public Deferral([In] DeferralCompletedHandler handler)
		{
			_handler = handler;
		}

		/// <summary>
		/// If the DeferralCompletedHandler has not yet been invoked,
		/// this will call it and drop the reference to the delegate.
		/// </summary>
		public void Complete() => _handler?.Invoke();

		public void Close() { }
	}
}
