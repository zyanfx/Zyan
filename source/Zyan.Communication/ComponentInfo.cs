using System;

namespace Zyan.Communication
{
	/// <summary>
	/// Schreibt eine veröffentlichte Komponente.
	/// </summary>
	[Serializable]
	public class ComponentInfo
	{
		/// <summary>
		/// Gibt den Schnittstellennamen zurück, oder legt ihn fest.
		/// </summary>
		public string InterfaceName
		{
			get;
			set;
		}

		/// <summary>
		/// Gibt den eindeutigen Namen der Komponente zurück, oder legt ihn fest.
		/// </summary>
		public string UniqueName
		{
			get;
			set;
		}

		/// <summary>
		/// Gibt den Aktivierungstyp zurück, oder legt ihn fest.
		/// </summary>
		public ActivationType ActivationType
		{
			get;
			set;
		}
	}
}
