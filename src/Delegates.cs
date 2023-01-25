namespace Wasmtime;

/// <summary>
/// Action accepting a caller
/// </summary>
public delegate void CallerAction(Caller caller);

/// <summary>
/// Action accepting a caller and 1 parameter
/// </summary>
public delegate void CallerAction<in T>(Caller caller, T arg);

/// <summary>
/// Action accepting a caller and 2 parameters
/// </summary>
public delegate void CallerAction<in T1, in T2>(Caller caller, T1 arg1, T2 arg2);

/// <summary>
/// Action accepting a caller and 3 parameters
/// </summary>
public delegate void CallerAction<in T1, in T2, in T3>(Caller caller, T1 arg1, T2 arg2, T3 arg3);

/// <summary>
/// Action accepting a caller and 4 parameters
/// </summary>
public delegate void CallerAction<in T1, in T2, in T3, in T4>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4);

/// <summary>
/// Action accepting a caller and 5 parameters
/// </summary>
public delegate void CallerAction<in T1, in T2, in T3, in T4, in T5>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

/// <summary>
/// Action accepting a caller and 6 parameters
/// </summary>
public delegate void CallerAction<in T1, in T2, in T3, in T4, in T5, in T6>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);

/// <summary>
/// Action accepting a caller and 7 parameters
/// </summary>
public delegate void CallerAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);

/// <summary>
/// Action accepting a caller and 8 parameters
/// </summary>
public delegate void CallerAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);

/// <summary>
/// Action accepting a caller and 9 parameters
/// </summary>
public delegate void CallerAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);

/// <summary>
/// Action accepting a caller and 10 parameters
/// </summary>
public delegate void CallerAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);

/// <summary>
/// Action accepting a caller and 11 parameters
/// </summary>
public delegate void CallerAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);

/// <summary>
/// Action accepting a caller and 12 parameters
/// </summary>
public delegate void CallerAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);

/// <summary>
/// Func accepting a caller
/// </summary>
public delegate TResult CallerFunc<out TResult>(Caller caller);

/// <summary>
/// Func accepting a caller and 1 parameter
/// </summary>
public delegate TResult CallerFunc<in T, out TResult>(Caller caller, T arg);

/// <summary>
/// Func accepting a caller and 2 parameters
/// </summary>
public delegate TResult CallerFunc<in T1, in T2, out TResult>(Caller caller, T1 arg1, T2 arg2);

/// <summary>
/// Func accepting a caller and 3 parameters
/// </summary>
public delegate TResult CallerFunc<in T1, in T2, in T3, out TResult>(Caller caller, T1 arg1, T2 arg2, T3 arg3);

/// <summary>
/// Func accepting a caller and 4 parameters
/// </summary>
public delegate TResult CallerFunc<in T1, in T2, in T3, in T4, out TResult>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4);

/// <summary>
/// Func accepting a caller and 5 parameters
/// </summary>
public delegate TResult CallerFunc<in T1, in T2, in T3, in T4, in T5, out TResult>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

/// <summary>
/// Func accepting a caller and 6 parameters
/// </summary>
public delegate TResult CallerFunc<in T1, in T2, in T3, in T4, in T5, in T6, out TResult>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);

/// <summary>
/// Func accepting a caller and 7 parameters
/// </summary>
public delegate TResult CallerFunc<in T1, in T2, in T3, in T4, in T5, in T6, in T7, out TResult>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);

/// <summary>
/// Func accepting a caller and 8 parameters
/// </summary>
public delegate TResult CallerFunc<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, out TResult>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);

/// <summary>
/// Func accepting a caller and 9 parameters
/// </summary>
public delegate TResult CallerFunc<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, out TResult>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);

/// <summary>
/// Func accepting a caller and 10 parameters
/// </summary>
public delegate TResult CallerFunc<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, out TResult>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);

/// <summary>
/// Func accepting a caller and 11 parameters
/// </summary>
public delegate TResult CallerFunc<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, out TResult>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);

/// <summary>
/// Func accepting a caller and 12 parameters
/// </summary>
public delegate TResult CallerFunc<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, in T10, in T11, in T12, out TResult>(Caller caller, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);

