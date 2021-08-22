/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetPointerEventsParams
{
	/* Pack=4 */
	public HtmlId : string;
	public Enabled : boolean;
	public static unmarshal(pData:number) : WindowManagerSetPointerEventsParams
	{
		const ret = new WindowManagerSetPointerEventsParams();
		
		{
			const ptr = Module.getValue(pData + 0, "*");
			if(ptr !== 0)
			{
				ret.HtmlId = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.HtmlId = null;
			}
		}
		
		{
			ret.Enabled = Boolean(Module.getValue(pData + 4, "i32"));
		}
		return ret;
	}
}
