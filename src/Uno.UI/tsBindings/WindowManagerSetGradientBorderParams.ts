/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetGradientBorderParams
{
	/* Pack=4 */
	public HtmlId : number;
	public BorderImage : string;
	public Width : string;
	public static unmarshal(pData:number) : WindowManagerSetGradientBorderParams
	{
		const ret = new WindowManagerSetGradientBorderParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			const ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.BorderImage = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.BorderImage = null;
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
