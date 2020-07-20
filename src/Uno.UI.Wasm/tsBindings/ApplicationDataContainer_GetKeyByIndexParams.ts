/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_GetKeyByIndexParams
{
	/* Pack=4 */
	public Locality : string;
	public Index : number;
	public static unmarshal(pData:number) : ApplicationDataContainer_GetKeyByIndexParams
	{
		const ret = new ApplicationDataContainer_GetKeyByIndexParams();
		
		{
			const ptr = Module.getValue(pData + 0, "*");
			if(ptr !== 0)
			{
				ret.Locality = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Locality = null;
			}
		}
		
		{
			ret.Index = Number(Module.getValue(pData + 4, "i32"));
		}
		return ret;
	}
}
