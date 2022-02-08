/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetSolidColorBorderParams
{
	/* Pack=4 */
	public HtmlId : number;
	public ColorHex : string;
	public Width : string;
	public static unmarshal(pData:number) : WindowManagerSetSolidColorBorderParams
	{
		const ret = new WindowManagerSetSolidColorBorderParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			const ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.ColorHex = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.ColorHex = null;
			}
		}
		
		{
			const ptr = Module.getValue(pData + 8, "*");
			if(ptr !== 0)
			{
				ret.Width = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Width = null;
			}
		}
		return ret;
	}
}
