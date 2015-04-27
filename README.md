# CompactBinaryFormat
A lightweight library for serializing data in a compact way in C#.

##Example usage

```csharp
//Writing a string to a file
Cbf.WriteFile("filename.cbf", "Hello Filesystem!");

//Multiple objects can be serialized with an array
Cbf.WriteFile("randomnumbers.cbf", new[] {2,6,5,7,4,8,2,9,7});

//Read a file; The type parameter (string in this case) can be used if the type of the object is known
var hello = Cbf.ReadFile<string>("filename.cbf");

//If the type is unknown at compile time, the method can be called without a type parameter
//This will always return an object of type System.Object
var obj = Cbf.ReadFile("filename.cbf");
```

##Public methods
All methods are static public methods in class CBF.Cbf

The supported file version can be retrieved with

```csharp
public static int Version{get;}
```

####Serializing data
The following methods are used to serialize data.
```csharp
public static byte[] Write(object o);
public static void WriteFile(string file, object o);
public static void Write(Stream str, object o, bool autoClose = false);
```

####Deserializing data
These methods are used to deserialize data.  
All methods can be called without a type parameter.
```csharp
public static T Read<T>(byte[] data);
public static T ReadFile<T>(string file);
public static T Read<T>(Stream stream, bool autoClose = false);
```

