/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRegisterEventOnViewParams
{
	/* Pack=4 */
	HtmlId : number;
	EventName : string;
	OnCapturePhase : boolean;
	EventFilterId : number;
	EventExtractorId : number;
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
			ret.EventFilterId = Number(Module.getValue(pData + 12, "i32"));
		}
		
		{
			ret.EventExtractorId = Number(Module.getValue(pData + 16, "i32"));
		}
		return ret;
	}
}
