/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetPointerEventsParams
{
	/* Pack=4 */
	public HtmlId : number;
	public Enabled : boolean;
	public static unmarshal(pData:number) : WindowManagerSetPointerEventsParams
	{
		const ret = new WindowManagerSetPointerEventsParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			ret.Enabled = Boolean(Module.getValue(pData + 4, "i32"));
		}
		return ret;
	}
}
