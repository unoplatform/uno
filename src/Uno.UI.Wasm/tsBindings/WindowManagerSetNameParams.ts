/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetNameParams
{
	/* Pack=4 */
	HtmlId : number;
	Name : string;
	public static unmarshal(pData:number) : WindowManagerSetNameParams
	{
		let ret = new WindowManagerSetNameParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			var ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.Name = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Name = null;
			}
		}
		return ret;
	}
}
