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
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			var ptr = Module.getValue(pData + 4, "*");
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
			var ptr = Module.getValue(pData + 12, "*");
			if(ptr !== 0)
			{
				ret.EventFilterName = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.EventFilterName = null;
			}
		}
		
		{
			var ptr = Module.getValue(pData + 16, "*");
			if(ptr !== 0)
			{
				ret.EventExtractorName = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.EventExtractorName = null;
			}
		}
		return ret;
	}
}
