/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetVisibilityParams
{
	/* Pack=4 */
	public HtmlId : string;
	public Visible : boolean;
	public static unmarshal(pData:number) : WindowManagerSetVisibilityParams
	{
		const ret = new WindowManagerSetVisibilityParams();
		
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
			ret.Visible = Boolean(Module.getValue(pData + 4, "i32"));
		}
		return ret;
	}
}
