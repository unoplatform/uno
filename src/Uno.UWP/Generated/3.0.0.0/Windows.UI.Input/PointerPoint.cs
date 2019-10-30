#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PointerPoint 
	{
		#if false
		[global::Uno.NotImplemented]
		public  uint FrameId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint PointerPoint.FrameId is not implemented in Uno.");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  bool IsInContact
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PointerPoint.IsInContact is not implemented in Uno.");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Input.PointerDevice PointerDevice
		{
			get
			{
				throw new global::System.NotImplementedException("The member PointerDevice PointerPoint.PointerDevice is not implemented in Uno.");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  uint PointerId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint PointerPoint.PointerId is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property Position
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Input.PointerPointProperties Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member PointerPointProperties PointerPoint.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Point RawPosition
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point PointerPoint.RawPosition is not implemented in Uno.");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public  ulong Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong PointerPoint.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.PointerPoint.PointerDevice.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.Position.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.RawPosition.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.PointerId.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.FrameId.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.Timestamp.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.IsInContact.get
		// Forced skipping of method Windows.UI.Input.PointerPoint.Properties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Input.PointerPoint GetCurrentPoint( uint pointerId)
		{
			throw new global::System.NotImplementedException("The member PointerPoint PointerPoint.GetCurrentPoint(uint pointerId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::System.Collections.Generic.IList<global::Windows.UI.Input.PointerPoint> GetIntermediatePoints( uint pointerId)
		{
			throw new global::System.NotImplementedException("The member IList<PointerPoint> PointerPoint.GetIntermediatePoints(uint pointerId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Input.PointerPoint GetCurrentPoint( uint pointerId,  global::Windows.UI.Input.IPointerPointTransform transform)
		{
			throw new global::System.NotImplementedException("The member PointerPoint PointerPoint.GetCurrentPoint(uint pointerId, IPointerPointTransform transform) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::System.Collections.Generic.IList<global::Windows.UI.Input.PointerPoint> GetIntermediatePoints( uint pointerId,  global::Windows.UI.Input.IPointerPointTransform transform)
		{
			throw new global::System.NotImplementedException("The member IList<PointerPoint> PointerPoint.GetIntermediatePoints(uint pointerId, IPointerPointTransform transform) is not implemented in Uno.");
		}
		#endif
	}
}
