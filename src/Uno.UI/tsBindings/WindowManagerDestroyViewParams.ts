/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerDestroyViewParams
{
	/* Pack=4 */
	public HtmlId : string;
	public static unmarshal(pData:number) : WindowManagerDestroyViewParams
	{
		const ret = new WindowManagerDestroyViewParams();
		
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
		return ret;
	}
}
