namespace More.Json
{
	//
	// Interface for classes that can serialize themselves to JSON
	//
	public interface IJsonValue
	{
		// Serialize to a JSON object (dictionary, array, string or number)
		object ToJsonValue();
	}
}
