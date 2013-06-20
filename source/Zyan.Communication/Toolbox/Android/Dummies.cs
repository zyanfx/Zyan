using System;

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

