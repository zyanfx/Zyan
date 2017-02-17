using System;

namespace ServerInterfaces
{
	[Serializable]
	public class FirstTestEventArgs : EventArgs
	{
		public DateTime Date { get; set; }
	}
}
