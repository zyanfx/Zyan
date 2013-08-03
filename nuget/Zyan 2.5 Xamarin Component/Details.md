Zyan is an easy to use distributed application framework
for .NET, Mono and Xamarin.Android (at least Indie edition is required).

Zyan provides clean intuitive API for hosting and accessing remote components.
It supports Windows, Linux, MacOS and Android platforms in any possible
combinations (i.e. Windows server + Android client or vice versa).

Notable features:

* Transparent realtime traffic compression and encryption, even on mobile devices.
* Bidirectional TCP communication through client-side NAT and firewalls.
* LINQ queries to the remote components, built-in LINQ expression serialization.
* Extensibility: allows custom protocols, session management, authentication, and more.
* Transparency: doesn't require any attributes or base classes for your components.
* Extensively covered with unit test and integration tests.
* Enterprise-ready (check out the *Who uses Zyan Framework* section of the website).
* Comprehensive documentation and free tech support in English, German and Russian.

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
