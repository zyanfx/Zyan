Zyan Communication Framework is used to establish bidirectional
network communication between two Android devices or between an
Android device and a PC running Windows, Linux or MacOS.
 
Consult the included sample application for the simplest possible
example of integrating Zyan library with your application.

For a real-world example application, check out the open-source
Zyan Drench game project at https://drench.codeplex.com.

## Server-side code example

```csharp
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
...

// Set up the duplex TCP protocol, use port 8080
var protocol = new TcpDuplexServerProtocolSetup(8080);

// Create component host named "ZyanDemo"
var host = new ZyanComponentHost("ZyanDemo", protocol);
 
// Register component type and interface
host.RegisterComponent<IHelloWorldService, HelloWordService>();
```

## Client-side code example

```csharp
using Zyan.Communication;
using Zyan.Communication.Protocols.Tcp;
...

// Connect to server
var connection = new ZyanConnection("tcpex://192.168.0.100:8080/ZyanDemo");
 
// Create a proxy for the remote component
var proxy = connection.CreateProxy<IHelloWorldService>();
 
// Invoke method
proxy.SayHello("HelloWorld");
 
// Subscribe to a remote event
proxy.MyEvent += (sender, e) => Console.WriteLine("Hi!");
```

## Project Resources

* [Project Homepage](http://zyan.com.de)
* [Nuget Package](https://www.nuget.org/packages/Zyan)
* [Ohloh Statistics Page](https://www.ohloh.net/p/zyan)

## Documentation and Support

* [Documentation](https://zyan.codeplex.com/documentation)
* [API Reference](http://zyan.sslk.ru/docs/v25)
* [Support forum in English](http://zyan.codeplex.com/discussions)
* [Support forum in German](http://www.mycsharp.de/wbb2/thread.php?threadid=89085)
* [Bug tracker](http://zyan.codeplex.com/workitem/list/basic)

## Sample Applications

* [Zyan Code Samples](https://zyan.codeplex.com/SourceControl/latest#examples/Zyan.Examples.Android/Zyan.Examples.Android.ConsoleServer/Program.cs)
* [Zyan Drench at CodeProject](http://www.codeproject.com/Articles/631666/Zyan-Drench-A-Game-for-Android-with-Wifi-Support), [CodePlex](https://drench.codeplex.com) and [GitHub](https://github.com/yallie/drench)
* [Zyan Drench at Google Play](https://play.google.com/store/apps/details?id=yallie.Zyan.Drench) and [Amazon](http://www.amazon.com/gp/product/B00E9GH0KY)
