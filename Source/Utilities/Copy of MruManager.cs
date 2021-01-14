using System;
using System.Windows.Forms;
using LumiSoft.UI.Controls;
using System.Collections;


/// <summary>
/// The DACarter.Utilities namespace contains various .NET classes that may be useful in future projects.
/// </summary>
namespace DACarter.Utilities
{
	/// <summary>
	/// This class handles keeping MRU combobox updated with persistent list in config file.
	/// </summary>
	/// <remarks>This class is really good and should be used more often. End of remark.</remarks>
	public class MruManager
	{

		private object comboBoxObject;
		private string configFile;
		private string mruKey;
		private int maxMRU; 
		private string topItem="";

		/// <summary>
		/// Hidden default constructor
		/// </summary>
		private MruManager()
		{
		}

		/// <summary>
		/// Constructor that takes initialization parameters as arguments.
		/// </summary>
		/// <remarks>
		/// This is the only public constructor of the <c>MruManager</c> class.
		/// </remarks>
		/// <param name="comboBoxObj">
		/// The combo box object that will display the MRU list.
		/// The object must either be a Systems.Windows.Forms.ComboBox type or
		/// a LumiSoft.UI.Controls.WComboBox type.
		/// </param>
		/// <param name="configFile">The name of the config file that will store the MRU list.</param>
		/// <param name="mruKey">
		/// The root name of the key to use in the config to store the MRU list.
		/// The keys in the config file will have "1","2","3", etc. appended to the root name.
		/// </param>
		/// <example>
		/// <c>MruManager mru = new MruManager("MyProg.ini","Files",fileComboBox);</c>
		/// <c> </c>
		/// This <c>mru</c> object can be used to create entries such as the following in the <c>MyProg.ini</c> file.
		/// <code>
		/// [MRU]
		/// Files1="MostRecentlyUsedFile"
		/// Files2="NextMostRecentlyUsedFile"
		/// Files3="etc"
		/// </code>
		/// 
		/// </example>
		public MruManager(string configFile, string mruKey, object comboBoxObj) {

			this.maxMRU = 8;

			this.configFile = configFile;
			this.mruKey = mruKey;
			this.comboBoxObject = comboBoxObj;

			if (comboBoxObj is ComboBox) {
				//Console.WriteLine("Passing a ComboBox");
			}
			else if (comboBoxObj is WComboBox) {
				//Console.WriteLine("Passing a WComboBox");
			}
			else {
				Console.WriteLine("Error -- did not pass valid combobox type");
				this.comboBoxObject = null;
			}
		}

		/// <summary>
		/// Property that gets/sets maximum size of MRU list
		/// </summary>
		/// <remarks>A default value for <c>MaxSize</c> is set in the constructor.</remarks>
		public int MaxSize {
			get {
				return maxMRU; 
			}
			set {
				if ((value>0) && (value <= 50)) {
					maxMRU = value;
				}
			}
		}

		/// <summary>
		/// Summary for UpdataMRU
		/// </summary>
		public void UpdateMRU() {
			// fills output folder combobox with MRU list from grid.ini
			string ss;
			string key;
			string saveText;

			if (comboBoxObject is WComboBox) {
				saveText = ((WComboBox)comboBoxObject).Text;
				((WComboBox)comboBoxObject).Items.Clear();
				((WComboBox)comboBoxObject).Text = saveText;
				for (int i=0; i<maxMRU; i++) {
					key = mruKey + (i+1).ToString();
					ss = INIFileInterop.INIWrapper.GetINIValue(configFile,"MRU",key);
					if (ss.Length == 0)
						break;
					((WComboBox)comboBoxObject).Items.Add(ss);
					if (i==0) {
						topItem = ss;
					}
				}
				((WComboBox)comboBoxObject).VisibleItems = ((WComboBox)comboBoxObject).Items.Count;
			}
			else if (comboBoxObject is ComboBox) {
				saveText = ((ComboBox)comboBoxObject).Text;
				((ComboBox)comboBoxObject).Items.Clear();
				((ComboBox)comboBoxObject).Text = saveText;
				for (int i=0; i<maxMRU; i++) {
					key = mruKey + (i+1).ToString();
					ss = INIFileInterop.INIWrapper.GetINIValue(configFile,"MRU",key);
					if (ss.Length == 0)
						break;
					((ComboBox)comboBoxObject).Items.Add(ss);
					if (i==0) {
						topItem = ss;
					}
				}
				((ComboBox)comboBoxObject).MaxDropDownItems = ((ComboBox)comboBoxObject).Items.Count;
			}
		}

		/// <summary>
		/// Adds a new value to the top of the MRU list in the configuration file.
		/// </summary>
		/// <param name="newValue">the value to be added to the top of the MRU list</param>
		/// <remarks>
		/// The string newValue becomes the value of the key <c>mruKey</c>1
		/// in the <c>[MRU]</c> section of the configuration file.
		/// <see href="DACarter.Utilities.MruManagerConstructor2.html"/>
		/// </remarks>
		/// 
		public void AddToMRU(string newValue) {
			// adds newValue to top of MRU list in grid.ini
			string ss;
			string key="";

			topItem = newValue;

			if ((newValue[0] != '#') && (newValue.Length != 0)) {
				// get all old values, eliminating duplicates to newValue
				ArrayList list = new ArrayList();
				for (int i=0; i<maxMRU; i++) {
					key = mruKey + (i+1).ToString();
					ss = INIFileInterop.INIWrapper.GetINIValue(configFile,"MRU",key);
					if ((ss.Length != 0) && (ss != newValue)) {
						list.Add(ss);
					}
				}
				// put the list back, starting at second element
				for (int i=1; i<maxMRU; i++) {
					key = mruKey + (i+1).ToString();
					if (i <= list.Count) {
						ss = (string)list[i-1];
					}
					else {
						ss = "";
					}
					INIFileInterop.INIWrapper.WriteINIValue(configFile,"MRU",key,ss);
				}
				// add newValue to first element
				key = mruKey + (1).ToString();
				INIFileInterop.INIWrapper.WriteINIValue(configFile,"MRU",key,newValue);
			}
		}

		/// <summary>
		/// Gets the top item in the MRU list.
		/// </summary>
		public string TopItem {
			get {
				if (topItem != null) {
					return topItem;
				}
				else {
					return "";
				}
			}
		}
	}
}
