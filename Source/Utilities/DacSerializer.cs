using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Drawing;



namespace DACarter.Utilities
{
	/// <summary>
	/// 
	/// </summary>
	public class DacSerializer
	{
		private System.Type _objectType;
		private string _fileName;

		private DacSerializer() {
		}

		public DacSerializer(string filebase, System.Type objectType) {

			_fileName = filebase;
			if (!_fileName.ToLower().EndsWith(".xml")) {
				_fileName += ".xml";
			}
			_objectType = objectType;
		}

		public bool Serialize(object obj) {
			try {
				XmlSerializer serializer = new XmlSerializer(_objectType);
				using (TextWriter writer = new StreamWriter(_fileName)) {
					serializer.Serialize(writer, obj);
				}
			}
			catch (Exception e1) {
				return false;
			}
			return true;
		}

		public object Deserialize() {
			try {
				XmlSerializer serializer = new XmlSerializer(_objectType);
				using (TextReader reader = new StreamReader(_fileName)) {
					return(serializer.Deserialize(reader));
				}
			}
			catch (Exception e1) {
				return null;
			}

		}

		public static bool SerializeToFile(string fileName, object obj, System.Type objectType) {
			DacSerializer ser = new DacSerializer(fileName, objectType);
			bool OK = ser.Serialize(obj);
			return OK;
		}

		public static object DeserializeFromFile(string fileName, System.Type objectType) { 
			DacSerializer ser = new DacSerializer(fileName, objectType);
			object obj = ser.Deserialize();
			return obj;
		}

		/// <summary>
		/// Create a file name, in the startup directory, based on application name
		/// </summary>
		/// <param name="name">Name to be attached to file.</param>
		/// <param name="obj">The object to store.</param>
        /// <param name="objectType">The class type (e.g. typeof(string))</param>
        /// <param name="baseName">[optional arg] specified base file name, used instead of application name</param>
        /// <returns>true if successful.</returns>
		public static bool SerializeAppObject(string name, object obj, System.Type objectType, string baseName = "") {

			// 
            string appName;
            if (baseName != String.Empty) {
                appName = baseName;
            }
            else {
                appName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            }
			string appPath = Application.StartupPath;
			string fileName = appName + "_" + name + ".xml";
			string fileNamePath = Path.Combine(appPath,fileName);

			return SerializeToFile(fileNamePath, obj, objectType);
			//DacSerializer ser = new DacSerializer(fileNamePath, objectType);
			//bool OK = ser.Serialize(obj);
			//return OK;
		}

        public static object DeserializeAppObject(string name, System.Type objectType, string baseName = "") {

			// create a file name, in the startup directory, based on application name
			//string appName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            string appName;
            if (baseName != String.Empty) {
                appName = baseName;
            }
            else {
                appName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            }
            string appPath = Application.StartupPath;
			string fileName = appName + "_" + name + ".xml";
			string fileNamePath = Path.Combine(appPath,fileName);

			return DeserializeFromFile(fileNamePath, objectType);
			//DacSerializer ser = new DacSerializer(fileNamePath, objectType);
			//object obj = ser.Deserialize();

			//return obj;
		}

		public static bool SaveDesktopPosition(Form form) {

			Rectangle rect = form.DesktopBounds;

			if (form.WindowState == FormWindowState.Minimized) {
				rect.X = -32000;
				rect.Y = -32000;
			}

			bool OK = SerializeAppObject("position", rect, typeof(Rectangle));

			return OK;
		}

		public static bool LoadDesktopPosition(Form form) {

			/*
			// create a file name, in the startup directory, based on application name
			string appName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
			string appPath = Application.StartupPath;
			string fileName = appName + "_position.xml";
			string fileNamePath = Path.Combine(appPath,fileName);

			DacSerializer ser = new DacSerializer(fileNamePath,typeof(Rectangle));
			object obj = ser.Deserialize();
			*/

			bool isMinimized = false;
			object obj = DeserializeAppObject("position",typeof(Rectangle));
			if (obj is Rectangle) {
				Rectangle rect = (Rectangle)obj;
				if (rect.X == -32000 || rect.Y == -32000) {
					// ultra-simple-minded way to handle minimized form
					// (we don't know what original window size was)
					rect.X = 100;
					rect.Y = 100;
					rect.Width = 200;
					rect.Height = 200;
					isMinimized = true;
				}
				KeepInView(ref rect);
				form.DesktopBounds = (Rectangle)rect;
				if (isMinimized) {
					form.WindowState = FormWindowState.Minimized;
				}
				return true;
			}
			else {
				return false;
			}
		}

		private static void KeepInView(ref Rectangle rect) {

			// make sure form rectangle is on a visible monitor screen
			System.Windows.Forms.Screen currentScreen = System.Windows.Forms.Screen.FromRectangle(rect);
			// Ensure top visible
			if ((rect.Top < currentScreen.Bounds.Top) || ((rect.Top + rect.Height) > (currentScreen.Bounds.Top + currentScreen.Bounds.Height))) {
				rect.Y = currentScreen.Bounds.Top;
			}

			// Ensure at least 120 px of Title Bar visible
			if (((rect.Left + rect.Width - 120) < currentScreen.Bounds.Left) || ((rect.Left + 120) > (currentScreen.Bounds.Left + currentScreen.Bounds.Width))) {
				rect.X = currentScreen.Bounds.Left;
			}

			/// FYI: here's how to look at all available screens (monitors):
			/*
			Screen[] screens = Screen.AllScreens;
			upperBound = screens.GetUpperBound(0);

			ArrayList list = new ArrayList();

			for (index = 0; index <= upperBound; index++) {

				// For each screen, add the screen properties to a list box.

				list.Add("Device Name: " + screens[index].DeviceName);
				list.Add("Bounds: " + screens[index].Bounds.ToString());
				list.Add("Type: " + screens[index].GetType().ToString());
				list.Add("Working Area: " + screens[index].WorkingArea.ToString());
				list.Add("Primary Screen: " + screens[index].Primary.ToString());

			}
			*/
		}

		/// <summary>
		/// Routine to convert any object to an array of bytes.
		/// </summary>
		/// <param name="anything"></param>
		/// <returns></returns>
		public static byte[] RawSerialize( object anything ) {
			int rawsize = Marshal.SizeOf( anything );
			IntPtr buffer = Marshal.AllocHGlobal( rawsize );
			Marshal.StructureToPtr( anything, buffer, false );
			byte[] rawdatas = new byte[ rawsize ];
			Marshal.Copy( buffer, rawdatas, 0, rawsize );
			Marshal.FreeHGlobal( buffer );
			return rawdatas;
		}

		/// <summary>
		/// Routine to convert an array of bytes to an object.
		/// Example of use:
		///		converting data structures in POP binary files to
		///		managed structures in .NET
		/// </summary>
		/// <param name="rawdatas"></param>
		/// <param name="anytype"></param>
		/// <returns></returns>
		public static object RawDeserialize( byte[] rawdatas, Type anytype ) {
			int rawsize = Marshal.SizeOf( anytype );
			if( rawsize > rawdatas.Length )
				return null;
			IntPtr buffer = Marshal.AllocHGlobal( rawsize );
			Marshal.Copy( rawdatas, 0, buffer, rawsize );
			object retobj = Marshal.PtrToStructure( buffer, anytype );
			Marshal.FreeHGlobal( buffer );
			return retobj;
		}


	}

}
