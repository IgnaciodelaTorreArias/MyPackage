using System.Reflection;
using System.Runtime.InteropServices;

using Google.Protobuf;

using Msg = MyPackage.Messages;


namespace MyPackage;

public class ResultUnavailableException : Exception
{
    public ResultUnavailableException() : base("Partial success") { }
}
public static partial class ForeignFunctions
{

    [DllImport("my_rust_protos", CallingConvention = CallingConvention.Cdecl)]
    static extern void free_buffer(nint ptr, nuint len);

    /// <summary>
    /// Filter errors from dynamic library.
    /// </summary>
    /// <param name="status"></param>
    /// <param name="buf"></param>
    /// <param name="ptr"></param>
    /// <param name="len"></param>
    /// <returns></returns>
    /// <exception cref="ResultUnavailableException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    static int ThrowErrors(int status)
    {
        if (status == 0)
            return 0;
        switch (status)
        {
            case -3:
                throw new ArgumentException($"Error occurred, no details recovered");
            case -5:
                throw new ObjectDisposedException("Object disposed, did you try to shallow copy an instance?");
        }
        // output unavailable
        throw new Exception($"Unknown error occurred, code: {status}");
    }

    /// <summary>
    /// Filter errors from dynamic library.
    /// </summary>
    /// <param name="status"></param>
    /// <param name="buf"></param>
    /// <param name="ptr"></param>
    /// <param name="len"></param>
    /// <returns></returns>
    /// <exception cref="ResultUnavailableException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    static int ThrowResultErrors(int status, Span<byte> buf, nint ptr, nuint len)
    {
        if (status == 0)
            return 0;
        if (status == -1)
            throw new ResultUnavailableException();
        string details = "";
        if ((status % 2) == 0) // output available
        {
            Msg.Error err;
            if (len > 0)
            {
                err = Msg.Error.Parser.ParseFrom(buf);
                free_buffer(ptr, len);
            }
            else
                err = new Msg.Error();
            details = err.Details;
        }
        switch (status)
        {
            case -2:
                throw new ArgumentException($"Error occurred, details: {details}");
            case -3:
                throw new ArgumentException($"Error occurred, no details recovered");
            case -5:
                throw new ObjectDisposedException("Object disposed, did you try to shallow copy an instance?");
        }
        // output unavailable
        throw new Exception($"Unknown error occurred, code: {status}");
    }

    // ========== **Standalone Functions** ==========

    public delegate int _FuncDelegate();
    public static void Func(_FuncDelegate func)
    {
        ThrowErrors(func());
    }

    public delegate int _FuncArgsDelegate(nint ptr, nuint len);
    public static void FuncArgs<I>(_FuncArgsDelegate func, I request)
        where I : IMessage<I>
    {
        byte[] buf = request.ToByteArray();
        unsafe
        {
            fixed (byte* ptr = buf)
            {
                ThrowErrors(func(
                    (nint)ptr,
                    (nuint)buf.Length
                ));
            }
        }
    }

    public delegate int _FuncArgsResultDelegate(nint ptr, nuint len, out nint outPtr, out nuint outLen);
    public static R FuncArgsResult<R, I>(_FuncArgsResultDelegate func, I request, MessageParser<R> Parser)
        where I : IMessage<I>
        where R : IMessage<R>
    {
        byte[] buf = request.ToByteArray();
        unsafe
        {
            fixed (byte* ptr = buf)
            {
                int result = func(
                    (nint)ptr,
                    (nuint)buf.Length,
                    out nint outPtr,
                    out nuint outLen
                );
                Span<byte> span = new((void*)outPtr, (int)outLen);
                ThrowResultErrors(result, span, outPtr, outLen);
                var res = Parser.ParseFrom(span);
                free_buffer(outPtr, outLen);
                return res;
            }
        }
    }

    public delegate int _FuncResultDelegate(out nint outPtr, out nuint outLen);
    public static R FuncResult<R>(_FuncResultDelegate func, MessageParser<R> Parser)
        where R : IMessage<R>
    {
        unsafe
        {
            int result = func(
                out nint outPtr,
                out nuint outLen
            );
            Span<byte> span = new((void*)outPtr, (int)outLen);
            ThrowResultErrors(result, span, outPtr, outLen);
            var res = Parser.ParseFrom(span);
            free_buffer(outPtr, outLen);
            return res;
        }
    }

    // ========== Create new instances for **Method Functions** ==========

    public delegate int _CreateNewDelegate(out nint instance_ptr);
    public static nint CreateNew(_CreateNewDelegate func)
    {
        int result = func(
            out nint instance_ptr
        );
        ThrowErrors(result);
        return instance_ptr;
    }

    public delegate int _CreateNewArgsDelegate(out nint instance_ptr, nint ptr, nuint len);
    public static nint CreateNewArgs<I>(_CreateNewArgsDelegate func, I request)
        where I : IMessage<I>
    {
        byte[] buf = request.ToByteArray();
        unsafe
        {
            fixed (byte* ptr = buf)
            {
                int result = func(
                    out nint instance_ptr,
                    (nint)ptr,
                    (nuint)buf.Length
                );
                ThrowErrors(result);
                return instance_ptr;
            }
        }
    }

    public delegate int _CreateNewArgsResultDelegate(out nint instance_ptr, nint ptr, nuint len, out nint outPtr, out nuint outLen);
    public static nint CreateNewArgsResult<I>(_CreateNewArgsResultDelegate func, I request)
        where I : IMessage<I>
    {
        byte[] buf = request.ToByteArray();
        unsafe
        {
            fixed (byte* ptr = buf)
            {
                int result = func(
                    out nint instance_ptr,
                    (nint)ptr,
                    (nuint)buf.Length,
                    out nint outPtr,
                    out nuint outLen
                );
                Span<byte> span = new((void*)outPtr, (int)outLen);
                ThrowResultErrors(result, span, outPtr, outLen);
                // We don't free the outPtr here, because it's only used to pass error details, managed by ThrowResultErrors
                return instance_ptr;
            }
        }
    }

    public delegate int _CreateNewResultDelegate(out nint instance_ptr, out nint outPtr, out nuint outLen);
    public static nint CreateNewResult(_CreateNewResultDelegate func)
    {
        unsafe
        {
            int result = func(
                out nint instance_ptr,
                out nint outPtr,
                out nuint outLen
            );
            Span<byte> span = new((void*)outPtr, (int)outLen);
            ThrowResultErrors(result, span, outPtr, outLen);
            // We don't free the outPtr here, because it's only used to pass error details, managed by ThrowResultErrors
            return instance_ptr;
        }
    }

    // ========== **Method Functions** ==========

    public delegate int _MethodFuncDelegate(nint instance_ptr);
    public static void Method(_MethodFuncDelegate func, nint instance_ptr)
    {
        ThrowErrors(func(instance_ptr));
    }

    public delegate int _MethodFuncArgsDelegate(nint instance_ptr, nint ptr, nuint len);
    public static void MethodArgs<I>(_MethodFuncArgsDelegate func, nint instance_ptr, I request)
        where I : IMessage<I>
    {
        byte[] buf = request.ToByteArray();
        unsafe
        {
            fixed (byte* ptr = buf)
            {
                ThrowErrors(func(
                    instance_ptr,
                    (nint)ptr,
                    (nuint)buf.Length
                ));
            }
        }
    }

    public delegate int _MethodFuncArgsResultDelegate(nint instance_ptr, nint ptr, nuint len, out nint outPtr, out nuint outLen);
    public static R MethodArgsResult<R, I>(_MethodFuncArgsResultDelegate func, nint instance_ptr, I request, MessageParser<R> Parser)
        where I : IMessage<I>
        where R : IMessage<R>
    {
        byte[] buf = request.ToByteArray();
        unsafe
        {
            fixed (byte* ptr = buf)
            {
                int result = func(
                    instance_ptr,
                    (nint)ptr,
                    (nuint)buf.Length,
                    out nint outPtr,
                    out nuint outLen
                );
                Span<byte> span = new((void*)outPtr, (int)outLen);
                ThrowResultErrors(result, span, outPtr, outLen);
                var res = Parser.ParseFrom(span);
                free_buffer(outPtr, outLen);
                return res;
            }
        }
    }

    public delegate int _MethodFuncResultDelegate(nint instance_ptr, out nint outPtr, out nuint outLen);
    public static R MethodResult<R>(_MethodFuncResultDelegate func, nint instance_ptr, MessageParser<R> Parser)
        where R : IMessage<R>
    {
        unsafe
        {
            int result = func(
                instance_ptr,
                out nint outPtr,
                out nuint outLen
            );
            Span<byte> span = new((void*)outPtr, (int)outLen);
            ThrowResultErrors(result, span, outPtr, outLen);
            var res = Parser.ParseFrom(span);
            free_buffer(outPtr, outLen);
            return res;
        }
    }

    // ========== Free instances for **Method Functions** ==========

    public delegate void _FreeDelegate(nint instance_ptr);

    public static void FreeInstance(_FreeDelegate func, nint instance_ptr)
    {
        func(instance_ptr);
    }
}

public abstract class ForeignInstance
{
    protected nint _instance_ptr;
    protected abstract ForeignFunctions._FreeDelegate FreeFunc { get; }
    ~ForeignInstance()
    {
        ForeignFunctions.FreeInstance(FreeFunc, _instance_ptr);
    }
}