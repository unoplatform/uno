/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRegisterEventOnViewParams
{
	/* Pack=4 */
	public HtmlId : number;
	public EventName : string;
	public OnCapturePhase : boolean;
	public EventExtractorId : number;
	public static unmarshal(pData:number) : WindowManagerRegisterEventOnViewParams
	{
		const ret = new WindowManagerRegisterEventOnViewParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			const ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.EventName = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.EventName = null;
			}
		}
		
		{
			ret.OnCapturePhase = Boolean(Module.getValue(pData + 8, "i32"));
		}
		
		{
			ret.EventExtractorId = Number(Module.getValue(pData + 12, "i32"));
		}
		return ret;
	}
}
