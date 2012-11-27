using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zyan.Communication.Toolbox
{
	/// <summary>
	/// Encapsulates a method that has five parameters and does not return a value.
	/// </summary>
	public delegate void Action<in T1, in T2, in T3, in T4, in T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

	/// <summary>
	/// Encapsulates a method that has six parameters and does not return a value.
	/// </summary>
	public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);

	/// <summary>
	/// Encapsulates a method that has seven parameters and does not return a value.
	/// </summary>
	public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);

	/// <summary>
	/// Encapsulates a method that has eight parameters and does not return a value.
	/// </summary>
	public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);

	/// <summary>
	/// Encapsulates a method that has nine parameters and does not return a value.
	/// </summary>
	public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);

	/// <summary>
	/// Encapsulates a method that has ten parameters and does not return a value.
	/// </summary>
	public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);

	/// <summary>
	/// Encapsulates a method that has five parameters and returns a value of the type specified by the TResult parameter.
	/// </summary>
	public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

	/// <summary>
	/// Encapsulates a method that has six parameters and returns a value of the type specified by the TResult parameter.
	/// </summary>
	public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);

	/// <summary>
	/// Encapsulates a method that has seven parameters and returns a value of the type specified by the TResult parameter.
	/// </summary>
	public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);

	/// <summary>
	/// Encapsulates a method that has eight parameters and returns a value of the type specified by the TResult parameter.
	/// </summary>
	public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);

	/// <summary>
	/// Encapsulates a method that has nine parameters and returns a value of the type specified by the TResult parameter.
	/// </summary>
	public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);

	/// <summary>
	/// Encapsulates a method that has ten parameters and returns a value of the type specified by the TResult parameter.
	/// </summary>
	public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
}
