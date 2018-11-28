/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRegisterEventOnViewParams
{
	/* Pack=4 */
	HtmlId : number;
	EventName : string;
	OnCapturePhase : boolean;
	EventFilterName : string;
	EventExtractorName : string;
	public static unmarshal(pData:number) : WindowManagerRegisterEventOnViewParams
	{
		let ret = new WindowManagerRegisterEventOnViewParams();
		ret.HtmlId = Number((Module.getValue(pData + 0, "*")));
		ret.EventName = String(Module.UTF8ToString(Module.getValue(pData + 4, "*")));
		ret.OnCapturePhase = Boolean((Module.getValue(pData + 8, "i32")));
		ret.EventFilterName = String(Module.UTF8ToString(Module.getValue(pData + 12, "*")));
		ret.EventExtractorName = String(Module.UTF8ToString(Module.getValue(pData + 16, "*")));
		return ret;
	}
}
