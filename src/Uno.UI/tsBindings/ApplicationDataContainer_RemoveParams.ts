/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_RemoveParams
{
	/* Pack=4 */
	public Locality : string;
	public Key : string;
	public static unmarshal(pData:number) : ApplicationDataContainer_RemoveParams
	{
		const ret = new ApplicationDataContainer_RemoveParams();
		
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
			const ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.Key = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Key = null;
			}
		}
		return ret;
	}
}
