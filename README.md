# Zyan Communication Framework

### Easy to use distributed application framework for .NET and Mono.

* [Project homepage: zyan.com.de](http://zyan.com.de)
* [NuGet package: Zyan](http://nuget.org/packages/Zyan)
* [Download binaries](https://zyan.codeplex.com/releases/)
* [Browse source code](http://zyan.codeplex.com/SourceControl/BrowseLatest)

## Why use Zyan Communication Framework?

### Easy to use

Zyan is very easy to learn and use. It provides clean intuitive API (see examples below) for hosting and accessing remote components.

### Supports desktop and mobile

Zyan runs on Windows, Linux, MacOS and Android platforms supporting [Microsoft .NET 3.5+](http://www.microsoft.com/net), [Mono 2.10+](http://mono-project.com) or [Mono for Android 4.7+](http://xamarin.com/monoforandroid) frameworks.

### Compact and secure

Supports transparent realtime traffic compression and encryption, even on mobile devices. Doesn't require digital certificates.

### Unobtrusive

All you need is plain .NET classes and interfaces: no ServiceContract/OperationContract attributes, no MarshalByRefObjects are necessary.

### Multiprotocol

Supports TCP, HTTP and Named pipes natively on most platforms. Extensibility model allows plugging any custom transport protocols.

### Full duplex TCP

Supports bidirectional TCP communication through client-side NAT and firewalls.

### LINQ-enabled

Supports LINQ queries to the remote components. Allows passing serialized LINQ expressions over network and generating anonymous classes on demand.

### Extensible

Plug in custom transport protocols, session manager, authentication provider, and more. Build loosely coupled client-server systems using Zyan and MEF.

### Supports events

Distributed events are as easy as button_Click in Windows Forms applications. Distributed Event-Based Components (EBC) are supported out-of-the-box.

### Supports duck typing

Zyan is able to host component that matches an interface, but doesn't implement it.

### Unit tested

Zyan code is extensively covered with unit tests and integrational tests. Integrational tests are executed on Windows and Linux using Mono.

### Enterprise ready

Zyan Framework is used in commercial enterprise applications. Check out the *Who uses Zyan Communication Framework* section of the website.

### Well-documented

Comprehensive documentation covers all aspects of Zyan API. Check out source code repository for several example application.

### Actively supported

Our community provides free technical support in English, German and Russian. Most of your questions are answered within hours!

### Absolutely free

Zyan is distributed under the terms of MIT license. It can be used in any applications, including closed-source and commercial.

## What does the application code look like?

### Server-side

``` C#
// Create component host named "ZyanDemo" and bind to TCP port 8080
var host = new ZyanComponentHost("ZyanDemo", 8080);
 
// Register component type and interface
host.RegisterComponent<IHelloWorldService, HelloWordService>();
```

### Client-side

``` C#
// Connect to server
var connection = new ZyanConnection("tcp://localhost:8080/ZyanDemo");
 
// Create HelloWorldService proxy
var proxy = connection.CreateProxy<IHelloWorldService>();
 
// Invoke a method
proxy.SayHello("HelloWorld");

// Subscribe to a remote event
proxy.MyEvent += (sender, e) => Console.WriteLine("Hi!");
```

## Why is it better than WCF?

* You don't have to decorate your interfaces with ServiceContract and OperationContract attributes: every method is a part of the contract.
* You can use overloaded methods, which isn't possible with WCF. Service contracts produce WSDL, which doesn't support method overloading (all operations must be uniquely named).
* You can call methods with generic parameters, which is also impossible with WCF. What's worse, these constraints cannot be validated at compile-time, so malformed WCF contracts throw exceptions at runtime.
* There is no need to create separate callback contract for the duplex communication -- just pass in a delegate as an argument to the remote method, or subscribe to a remote event using familiar syntax.
* You are not forced to specify component activation mode using attributes at compile time. ZyanComponentHost can be configured to activate any component in a single-call (default) or a singleton mode.

## Why is it better than .NET Remoting?

* You don't have to derive your component from MarshalByRefObject. Any .NET class can be used as a hosted component.
* With the duck typing feature, you can bind a component to an interface it doesn't implement (provided that all interface methods are implemented). This way you can make any third-party class remotely callable.
* You can set up custom serialization handlers for data types that aren't serializable. This way you can make any third-party class serializable.
* You can query remote components using LINQ, even with projections to anonymous classes. LINQ expressions can be passed as method arguments and returned from remote methods. This feature relies on custom serialization handlers mentioned above.
* There is no need for special treatment to use events, everything works just out-of-the box. Subscription to the remote event looks exactly the same as local subscription.

## Why is it better than other RPC frameworks?

* Zyan is more than just an RPC framework. It is in fact a skeleton for a full-featured application server with a built-in authentication, session management, policy injection features, etc.
* Authentication system is fully customizable. You can use integrated Windows authentication (single sign on) or plug in your own authentication provider.
* Session management is also customizable. Fast and slim in-process session manager can be used for self-hosted single-process application servers. SqlServer-backed session manager is suitable for multi-process clusters. You can easily create your own session manager by inheriting from the base class and overriding a few virtual methods.
* Zyan supports transparent traffic compression (using Deflate or LZF algorithm) and encryption out-of-the-box. Encryption feature doesn't require certificates and is not platform dependent.

## What are other benefits of Zyan?

* Managed Extensibility Framework integration (only available for .NET 4.x and Mono 2.10) allows building loosely coupled client-server applications. You can set up your application server declaratively using MEF attributes and publish it with a Zyan host.
* Client application can use MEF container to access remote services. Both client and server can be completely unaware of the communication layer being used.
* Zyan supports wiring distributed Event-Based Components. EBC is an architectural model which reduces dependencies between components. Components in EBC application communicate with each other by means of messages instead of invoking methods.
* Other notable features of Zyan include call interception, deterministic resource cleanup, duplex TCP communication, server-side event filters, session-bound events, OneWay methods support, centralized exception handling, and many more.

## Links to Resources

* [Project homepage: zyan.com.de](http://zyan.com.de)
* [NuGet package: Zyan](http://nuget.org/packages/Zyan)
* [Download binaries](https://zyan.codeplex.com/releases/)
* [Browse source code](http://zyan.codeplex.com/SourceControl/BrowseLatest)
* [Project statistics at Ohloh](https://www.ohloh.net/p/zyan)
* [Support forum in English](http://zyan.codeplex.com/discussions)
* [Support forum in German](http://www.mycsharp.de/wbb2/thread.php?threadid=89085)
