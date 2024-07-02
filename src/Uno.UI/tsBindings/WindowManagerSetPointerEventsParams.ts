/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetPointerEventsParams
{
	/* Pack=4 */
	public HtmlId : number;
	public Value : string;
	public static unmarshal(pData:number) : WindowManagerSetPointerEventsParams
	{
		const ret = new WindowManagerSetPointerEventsParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			const ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.Value = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Value = null;
			}
		}
		return ret;
	}
}
