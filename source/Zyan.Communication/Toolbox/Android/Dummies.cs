using System;

namespace System.Diagnostics
{
	internal static class Trace
	{
		public static void Write(params object[] values)
		{ 
		}

		public static void WriteLine(params object[] values)
		{
		}
	}
}

namespace System.ServiceModel
{
	internal class ServiceContractAttribute : Attribute
	{
	}

	internal class OperationContractAttribute : Attribute
	{
	}
	
	internal class FaultContractAttribute : Attribute
	{
		public FaultContractAttribute(Type exceptionType)
		{
		}
	}
}

namespace Zyan.InterLinq.Communication.Wcf.NetDataContractSerializer
{
	internal class NetDataContractFormatAttribute : Attribute
	{
	}
}

namespace Zyan.Communication.Protocols.Http
{
}

namespace Zyan.Communication.Protocols.Ipc
{
}

