using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace DACarter.Utilities {
	/// <summary>
	/// ArchiverConfigReader
	/// This class contains one static method to 
	///		read an archiver config file and store its information
	///		into an ArchiverConfiguration object.
	/// </summary>
	/// 
	public class ArchiverConfigReader {
		private ArchiverConfigReader() {
		}

		public static void Read(string fileName, ArchiverConfiguration archiverConfig, ArchiverOptions options) {
			try {

				archiverConfig.Clear();

				if (!File.Exists(fileName)) {
					if ( options.NewConfig) {
						// if we are creating a new config file, nothing to read
						return;
					}
					else {
						throw new ApplicationException("Cannot read config file: " + fileName);
					}
				}
				XmlDocument doc = LoadXmlDocument(fileName);

				// look for initial comment block
				//   either the first node or 
				//   the first node after the XML declaration
				string commentText = "";
				XmlNode firstNode = doc.FirstChild;
				if (firstNode.NodeType == XmlNodeType.Comment) {
					commentText = firstNode.Value;
				}
				else if (firstNode.NodeType == XmlNodeType.XmlDeclaration) {
					XmlNode nextNode = firstNode.NextSibling;
					if (nextNode.NodeType == XmlNodeType.Comment) {
						commentText = nextNode.Value;
					}
				}
				archiverConfig.commentString = commentText;

				XmlNode root = doc.SelectSingleNode(CaseInsensitiveXPath("ArchiverConfig"));

				// read Path ID section
				XmlNode IdNode = root.SelectSingleNode(CaseInsensitiveXPath("ArchivePathID"));
				if (IdNode != null) {
					archiverConfig.PathID = GetSingleTextValue(IdNode);
				}

				// read LogFilePath section
				XmlNode logFileNode = root.SelectSingleNode(CaseInsensitiveXPath("LogFilePath"));
				if (logFileNode != null) {
					archiverConfig.LogFilePath = GetSingleTextValue(logFileNode);
				}
				else {
					archiverConfig.LogFilePath = ".";
				}

				// read time interval section
				archiverConfig.ArchiveInterval = ArchiveIntervalType.Daily; // default
				string intervalString = "";
				XmlNode intervalNode = root.SelectSingleNode(CaseInsensitiveXPath("TimeInterval"));
				if (intervalNode != null) {

					intervalString = GetSingleTextValue(intervalNode).ToLower();

					if ((intervalString.StartsWith("day")) ||
						(intervalString.StartsWith("daily"))) {
						archiverConfig.ArchiveInterval = ArchiveIntervalType.Daily;
					}
					else if (intervalString.StartsWith("hour")) {
						archiverConfig.ArchiveInterval = ArchiveIntervalType.Hourly;
					}
					else if (intervalString.StartsWith("all")) {
						archiverConfig.ArchiveInterval = ArchiveIntervalType.All;
					}

				}  // end of IntervalNode

				// read Destination Drives section
				XmlNode destNode = root.SelectSingleNode(CaseInsensitiveXPath("DestinationDrives"));
				if (destNode != null) {
					GetDestinationDrives(destNode, archiverConfig.DestDrives);
				}

				// read <sources> section 
				//	which contains one or more <source> child nodes
				//	The <source> nodes have 2 attributes: SourcePath and Destination 
				XmlNode sourcesNode = root.SelectSingleNode(CaseInsensitiveXPath("Sources"));
				if (sourcesNode != null) {
					XmlNodeList nodes = sourcesNode.ChildNodes;
					foreach (XmlNode node in nodes) {
						if (node.Name.ToLower() == "source") {
							XmlAttributeCollection attributes = node.Attributes;
							if (attributes.Count > 0) {
								string source = "", destination = "";
								foreach (XmlAttribute attribute in attributes) {
									if (attribute.Name.ToLower() == "sourcepath") {
										source = attribute.Value;
									}
									if (attribute.Name.ToLower() == "destination") {
										destination = attribute.Value;
									}
								} // end of foreach attribute
								archiverConfig.SourceList.Add(new SourceInfo(source, destination));
							}
						}
					} // end of foreach source node
				}  // end of sourcesNode

				// read <FileMatch> section
				//	Add regex string to RegexList
				XmlNodeList fileMatchNodeList = root.SelectNodes(CaseInsensitiveXPath("FileMatch"));
				foreach (XmlNode fileMatchNode in fileMatchNodeList) {
					if (fileMatchNode != null) {
						string regexPattern = GetSingleTextValue(fileMatchNode);
						Regex regex = new Regex(regexPattern,
							RegexOptions.IgnoreCase |
							RegexOptions.ExplicitCapture | 
							RegexOptions.Compiled);
						archiverConfig.RegexList.Add(regex);
					}  // end fileMatchNode					
				}

			}
			catch (Exception e) {
				throw new ApplicationException("Error reading config file: "+ fileName, e);
			}
			finally {
			}

		}

		/// <summary>
		/// If node has a single attribute or a single text child node,
		/// then that string is returned.
		/// If no attribute or text child node, returns empty string.
		/// If more than one attribute, throws ApplciationException.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private static string GetSingleTextValue(XmlNode node) {
			string value="";
			XmlAttributeCollection attributes = node.Attributes;
			if (attributes.Count > 0) {
				if (attributes.Count == 1) {
					// if only one attribute, use it
					value = attributes[0].Value;
				}
				else {
					// if more than one attribute
					throw new ApplicationException("Expected single attribute in config node: " + node.Name);
				}
			}
			// if has child subnode, use it instead
			else if (node.HasChildNodes) {
				if (node.FirstChild.Name == "#text") {
					value = node.FirstChild.Value;
				}
			}
			return value.Trim();
		}

		// end of Read() method

		//////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// LoadXmlDocument creates an XmlDocument object and loads the XML file into it.
		/// </summary>
		/// <param name="fileName">The full file name</param>
		/// <returns>The XML document as a XmlDocument type</returns>
		/// 
		private static XmlDocument LoadXmlDocument(string fileName) {
			XmlDocument doc = new XmlDocument();
			XmlTextReader reader = new XmlTextReader(fileName);
			reader.WhitespaceHandling = WhitespaceHandling.None;
			doc.Load(reader);
			reader.Close();
			return doc;
		}

		//////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Creates a string that can be used to locate a case-insensitive target
		/// in XmlDocument methods that require an XPath expression, 
		/// such as XmlDocument.SelectSingleNode() 
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		private static string CaseInsensitiveXPath(string target) {
			string begin = "*[translate(name(),'abcdefghijklmnopqrstuvwxyz','ABCDEFGHIJKLMNOPQRSTUVWXYZ')='";
			string end = "']";
			return begin + target.ToUpper() + end;
		}

		//////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		/// <param name="destNode"></param>
		/// <param name="destCollection"></param>
		/// <returns>bool</returns>
		///
		private static bool GetDestinationDrives(XmlNode destNode, DestinationDriveCollection destCollection) {
			// default values for attributes
			destCollection.ReservedSpaceString = "500 Mb";
			destCollection.CopiesRequired = 9999;
			XmlAttributeCollection attributes = destNode.Attributes;
			if (attributes.Count > 0) {
				foreach (XmlAttribute attribute in attributes) {
					if (attribute.Name.ToLower() == "reservedspace") {
						//Console.Write("reserved = " + attribute.Value);
						destCollection.ReservedSpaceString = attribute.Value;
					}
					else if (attribute.Name.ToLower() == "copiesrequired") {
						double nCopies;
						try {
							nCopies = Double.Parse(attribute.Value);
						}
						catch (Exception e) {
							nCopies = 100;
						}
						destCollection.CopiesRequired = (int)nCopies;
						
					}
				}
			}
			XmlNodeList nodes = destNode.ChildNodes;
			foreach (XmlNode node in nodes) {
				//Console.WriteLine("...." + node.Name);
				if (node.Name.ToLower() == "mirrorset") {
					GetMirrorSet(node, destCollection);
				}
			}
			return true;
		}

		//////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		/// <param name="mirrorNode"></param>
		/// <returns></returns>
		/// 
		private static bool GetMirrorSet(XmlNode mirrorNode, DestinationDriveCollection destCollection) {
			string type = "NONE";
			string reservedSpaceAttribute = "";
			XmlAttributeCollection msAttributes = mirrorNode.Attributes;
			if (msAttributes.Count > 0) {
				foreach (XmlAttribute attribute in msAttributes) {
					if (attribute.Name.ToLower() == "type") {
						//Console.WriteLine("......type = " + attribute.Value);
						type = attribute.Value;
					}
					else if (attribute.Name.ToLower() == "reservedspace") {
						reservedSpaceAttribute = attribute.Value;
					}
				}
			}
			XmlNodeList mirrorNodes = mirrorNode.ChildNodes;
			double reserved;
			if (reservedSpaceAttribute == "") {
				// default reserved space is specified with "-1"
				reserved = -1;
			}
			else {
				reserved = DestinationDriveCollection.ParseBytes(reservedSpaceAttribute);
			}
			destCollection.StartNewMirrorSet(type, reserved);
			foreach (XmlNode node in mirrorNodes) {
				//Console.Write("......"+node.Name);
				if (node.Name.ToLower() == "drive") {
					GetDestDrive(node, destCollection);
				}
				//Console.WriteLine();
			}
			return true;
		}

		//////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// 
		/// </summary>
		/// <param name="driveNode"></param>
		/// <returns></returns>
		/// 
		private static bool GetDestDrive(XmlNode driveNode, DestinationDriveCollection destCollection) {
			string drive = GetSingleTextValue(driveNode);
			if (drive.Length > 0) {
				if (!drive.EndsWith(@"\")) {
					drive += @"\";
				}
				destCollection.AddDrive(drive);
				return true;
			}
			else {
				return false;
			}
		}

	} // end of ArchiverReader class

	//////////////////////////////////////////////////////////////////////////
	//
	// The following classes need to be accessed by all of archiver
	//
	//////////////////////////////////////////////////////////////////////////

} // End of namespace Archiver2
