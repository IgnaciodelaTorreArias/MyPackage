using System.Runtime.InteropServices;

using Msg = MyPackage.Messages;

namespace MyPackage;

public static partial class ForeignFunctions
{
    [DllImport("my_rust_protos", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int rust_protos_greet(
        nint ptr,
        nuint len,
        out nint outPtr,
        out nuint outLen
    );
    /// <summary>
    /// 
    /// </summary>
    /// <param name="greetings"></param>
    /// <returns></returns>
    /// <exception cref="ResultUnavailableException"> asdf</exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    public static string CallGretings(string name)
    {
        return FuncArgsResult<Msg.Response, Msg.Greetings>(rust_protos_greet, new() { Name = name}, Msg.Response.Parser).Text;
    }
}