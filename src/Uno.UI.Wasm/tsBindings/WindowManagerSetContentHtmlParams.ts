/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetContentHtmlParams
{
	/* Pack=4 */
	public HtmlId : number;
	public Html : string;
	public static unmarshal(pData:number) : WindowManagerSetContentHtmlParams
	{
		const ret = new WindowManagerSetContentHtmlParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			const ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.Html = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Html = null;
			}
		}
		return ret;
	}
}
