/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
namespace Microsoft.UI.Xaml
{
	export class NativePointerEventArgs
	{
		/* Pack=4 */
		public HtmlId : number;
		public Event : number;
		public pointerId : number;
		public x : number;
		public y : number;
		public ctrl : boolean;
		public shift : boolean;
		public buttons : number;
		public buttonUpdate : number;
		public deviceType : number;
		public srcHandle : number;
		public timestamp : number;
		public pressure : number;
		public wheelDeltaX : number;
		public wheelDeltaY : number;
		public hasRelatedTarget : boolean;
		public marshal(pData:number)
		{
			Module.setValue(pData + 0, this.HtmlId, "*");
			Module.setValue(pData + 4, this.Event, "i8");
			Module.setValue(pData + 8, this.pointerId, "double");
			Module.setValue(pData + 16, this.x, "double");
			Module.setValue(pData + 24, this.y, "double");
			Module.setValue(pData + 32, this.ctrl, "i32");
			Module.setValue(pData + 36, this.shift, "i32");
			Module.setValue(pData + 40, this.buttons, "i32");
			Module.setValue(pData + 44, this.buttonUpdate, "i32");
			Module.setValue(pData + 48, this.deviceType, "i32");
			Module.setValue(pData + 52, this.srcHandle, "i32");
			Module.setValue(pData + 56, this.timestamp, "double");
			Module.setValue(pData + 64, this.pressure, "double");
			Module.setValue(pData + 72, this.wheelDeltaX, "double");
			Module.setValue(pData + 80, this.wheelDeltaY, "double");
			Module.setValue(pData + 88, this.hasRelatedTarget, "i32");
		}
	}
}
