/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetVisibilityParams
{
	/* Pack=4 */
	public HtmlId : number;
	public Visible : boolean;
	public static unmarshal(pData:number) : WindowManagerSetVisibilityParams
	{
		const ret = new WindowManagerSetVisibilityParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			ret.Visible = Boolean(Module.getValue(pData + 4, "i32"));
		}
		return ret;
	}
}
