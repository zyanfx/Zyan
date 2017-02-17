using System;

namespace ServerInterfaces
{
	[Serializable]
	public class SecondTestEventArgs : EventArgs
	{
		public DateTime Date { get; set; }
	}
}
