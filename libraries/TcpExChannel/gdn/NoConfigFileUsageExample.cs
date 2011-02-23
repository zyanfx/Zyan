// For those of us who detest (or are simply to daft to understand) config files. 
// The moral of the following story, is that both the marshaled class, and the class remoting it, must be marshelable...  
// Now feel free to critisize me for the bad programming practices you see in the following code.
// -- TheLoneCabbage

using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using TcpEx;

namespace TestServer 
{ 
   class TestServer 
   { 
      [STAThread] 
      static void Main(string[] args) 
      { 
         ChannelServices.RegisterChannel(new TcpExChannel(TypeFilterLevel.Full, 1948), false); 
         TestClass.TestClass tc = new TestClass.TestClass(); 
         RemotingServices.Marshal(tc,"TLC8675309"); 
 
         Console.WriteLine("Press Enter to close server."); 
         Console.ReadLine(); 
      } 
   } 
} 
 
namespace TestClass 
{ 
   [Serializable] 
   public class TestClass: MarshalByRefObject 
   { 
      private int cnt=0; 
      public delegate void dPoke(int x); 
      public event dPoke OnPoked; 
 
      public TestClass() 
      {} 
 
      public string SayHello() 
      { 
         cnt++; 
         Console.WriteLine("I'm saying 'Hello'. Are you happy? This is the " 
         +cnt.ToString()+" time!"); 
 
         return "Hello, putz!"; 
      } 
 
      public void PokeBack(int x) 
      { 
         Console.WriteLine("Poking Client!");

         if (OnPoked!=null) 
            if (OnPoked.GetInvocationList().Length < 0) 
            { 
               OnPoked(x); 
               Console.WriteLine("I poked the client."); 
               return; 
            } 
         Console.WriteLine("No Events Registered."); 
 
      } 
 
      public override object InitializeLifetimeService() 
      {
      	return null; //return base.InitializeLifetimeService ();
      } 
 
   } 
} 
 
namespace TestClient 
{ 
   [Serializable] 
   class TestClient: MarshalByRefObject 
   { 
      public void pk(int x) 
      { 
        Console.WriteLine("I've been poked ("+x.ToString()+")!"); 
      }
 
      public TestClient(int timeout) 
      { 
         Console.WriteLine("Press Enter to start client."); 
         Console.ReadLine(); 
    
 
         ChannelServices.RegisterChannel(new TcpEx.TcpExChannel(TypeFilterLevel.Full, true), false); 
 
         TestClass.TestClass tc=(TestClass.TestClass)Activator.GetObject(typeof(TestClass.TestClass), @"tcpex://localhost:1948/TLC8675309"); 
          
         tc.OnPoked += new TestClass.TestClass.dPoke(pk); 
          
         Console.WriteLine(tc.SayHello()); 
 
         tc.PokeBack(42);
      } 
   }
}    
