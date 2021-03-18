/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_ClearParams
{
	/* Pack=4 */
	public Locality : string;
	public static unmarshal(pData:number) : ApplicationDataContainer_ClearParams
	{
		const ret = new ApplicationDataContainer_ClearParams();
		
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
		return ret;
	}
}
