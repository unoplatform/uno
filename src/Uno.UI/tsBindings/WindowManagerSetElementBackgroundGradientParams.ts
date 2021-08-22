/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetElementBackgroundGradientParams
{
	/* Pack=4 */
	public HtmlId : string;
	public CssGradient : string;
	public static unmarshal(pData:number) : WindowManagerSetElementBackgroundGradientParams
	{
		const ret = new WindowManagerSetElementBackgroundGradientParams();
		
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
			const ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.CssGradient = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.CssGradient = null;
			}
		}
		return ret;
	}
}
