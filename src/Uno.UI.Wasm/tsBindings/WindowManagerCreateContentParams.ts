/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerCreateContentParams
{
	/* Pack=4 */
	HtmlId : number;
	TagName : string;
	Handle : number;
	UIElementRegistrationId : number;
	IsSvg : boolean;
	IsFocusable : boolean;
	public static unmarshal(pData:number) : WindowManagerCreateContentParams
	{
		let ret = new WindowManagerCreateContentParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			var ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.TagName = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.TagName = null;
			}
		}
		
		{
			ret.Handle = Number(Module.getValue(pData + 8, "*"));
		}
		
		{
			ret.UIElementRegistrationId = Number(Module.getValue(pData + 12, "i32"));
		}
		
		{
			ret.IsSvg = Boolean(Module.getValue(pData + 16, "i32"));
		}
		
		{
			ret.IsFocusable = Boolean(Module.getValue(pData + 20, "i32"));
		}
		return ret;
	}
}
