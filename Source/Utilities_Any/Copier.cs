using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DACarter.Utilities
{
	/// <summary>
	/// Contains a static method DeepCopyObject
	/// that makes a "deep" copy: a copy of an object
	/// such that all the members of the object are copied --
	/// not just the references, the actual objects are copied.
	/// The resulting object is an exact duplicate and completely
	/// independent of the original - all references in the copy
	/// refer to different objects than the original.
	/// </summary>
	/// <remarks>
	/// The object being copied (and all members)
	/// must be marked [Serializable]
	/// </remarks>
	public static class DacCopier
	{

		public static Object DeepCopyObject(Object obj) {

			Object result;

			//Create a new binary formatter and memory stream
			BinaryFormatter formatter = new BinaryFormatter();
			MemoryStream stream = new MemoryStream();

			//Serialize the source object to the stream
			formatter.Serialize(stream, obj);

			//Rewind the stream
			stream.Seek(0, SeekOrigin.Begin);

			//Deserialize the stream to a new object
			result = formatter.Deserialize(stream);
			stream.Close();

			//Return the new object
			return result;

		}
	}
}
