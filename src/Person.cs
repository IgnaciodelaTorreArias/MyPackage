using System.Runtime.InteropServices;

using Msg = MyPackage.Messages;

namespace MyPackage;

public static partial class ForeignFunctions
{
    [DllImport("my_rust_protos", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int create_new_person(out nint instance_ptr, nint ptr, nuint len);

    [DllImport("my_rust_protos", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int person_greet(nint instance_ptr, nint ptr, nuint len, out nint outPtr, out nuint outLen);

    [DllImport("my_rust_protos", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void free_person(nint instance_ptr);

}

public class Person: ForeignInstance
{
    protected override ForeignFunctions._FreeDelegate FreeFunc => ForeignFunctions.free_person;

    public Person(string name, byte age)
    {
        _instance_ptr = ForeignFunctions.CreateNewArgs<Msg.PersonParams>(
            ForeignFunctions.create_new_person,
            new() {
                Name = name,
                Age = age
            }
        );
    }

    public string Greet(string other)
    {
        return ForeignFunctions.MethodArgsResult<Msg.Response, Msg.Greetings>(
            ForeignFunctions.person_greet,
            _instance_ptr,
            new() { Name = other },
            Msg.Response.Parser
        ).Text;
    }
}
