/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetXUidParams
{
	/* Pack=4 */
	HtmlId : number;
	Uid : string;
	public static unmarshal(pData:number) : WindowManagerSetXUidParams
	{
		let ret = new WindowManagerSetXUidParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			var ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.Uid = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Uid = null;
			}
		}
		return ret;
	}
}
