using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Windows.Foundation;

/// <summary>
/// Stores a DeferralCompletedHandler to be invoked upon completion of the deferral and manipulates the state of the deferral.
/// </summary>
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

	/// <summary>
	/// Completes the deferral (calls <see cref="Complete" />).
	/// </summary>
	public void Dispose() => Complete();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void Close() => Complete();
}
