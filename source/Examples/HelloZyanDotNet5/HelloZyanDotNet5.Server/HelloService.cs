using HelloZyanDotNet5.Shared;

namespace HelloZyanDotNet5.Server
{
	public class HelloService : IHelloService
	{
		public string Greet(string name)
		{
			return $"Hello, {name}.";
		}
	}
}