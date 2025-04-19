# FFI: Calling Functions via C ABI with Protobuf-based Arguments and Results

**Description**: This project demonstrates how to call functions via the C ABI using Protocol Buffers (protobuf) for argument and result serialization.

This project only wraps the `my_rust_protos` library in a way that follows C# syntax.


## Usage

```C#

using MyPackage;

Console.WriteLine($"Response: {ForeignFunctions.CallGretings("Steve")}"); // "Response: Hello Steve, have a good day!!!"

Person p = new Person("Steve", 20);
Console.WriteLine($"Response: {p.Greet("Ray")}"); // "Response: Hi Ray, my name is Steve, I'm 20 years old."
```
