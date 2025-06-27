InstantRpc
---
[![NuGet](https://img.shields.io/nuget/v/InstantRpc.svg)](https://www.nuget.org/packages/InstantRpc)

Using InstantRpc, you can make RPC server without defining interface so that another process on the same PC can get/set properties and invoke methods of any specified object. This enables you to operate or check objects in your app without API from out-of-process test code.

On server, add one line to expose your instance:

```csharp
// case 1: for basic usage
InstantRpcService.Expose(this);
// case 2: for WPF app UI objects, specify Dispatcher.Invoke() as wrappers to access the object
InstantRpcService.Expose(this, (a) => Dispatcher.Invoke(a), (f) => Dispatcher.Invoke(f));
```

On client, refer the server assembly, then write calling with lamda expression:

```csharp
// client side
var client = new InstantRpcClient<YourClass>();
client.WaitUntilExposed(TimeSpan.FromSeconds(5)); // if needed, wait server gets ready

// get
var value = client.Get((x) => x.Value);
// set
client.Set((x) => x.YourProperty, "new value");
// invoke
var result = client.Invoke((x) => x.YourMethod());
```

Find further examples here [InstantRpc.Test/Test1.cs](https://github.com/cactuaroid/InstantRpc/blob/main/InstantRpc.Test/Test1.cs)

Remarks
---
- You can specify any public properties or methods with property chain and casting type.
- Limitations to get/set property and get returning value of method
  - ✔ Primitive type
  - ✔ Enum
  - ✔ ValueTuple (not nested)
  - ✔ Parsable type - the type has a method `public static Parse(string)` and it can parse `ToString()` result.
  - ✖ Other types
- For method parameter, you can give any type if you specify constructor including property initialization.
  - ✔ `client.Invoke((x) => x.YourMethod(new MyParam("1", "2"), new MyParam() { Value = "3" })));`
  - ✖ `client.Invoke((x) => x.YourMethod(myParam1, myParam2));` if myParams are not parsable.
- Multiple servers/clients on a PC is not supported.

Requirements
---
No dependencies. You can use this library for any projects, but I recommend to use on testing environment only.

Supported platform:
- .NET Framework 4.0+
- .NET Core 2.0+
