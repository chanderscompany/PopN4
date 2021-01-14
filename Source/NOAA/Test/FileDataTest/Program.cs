using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using DACarter.NOAA;
using NUnit.Framework;

namespace FileDataTest {
	class TestProgram {
		static void Main(string[] args) {
		}
	}

	[TestFixture]
	public class TestFixture1 {
		[Test]
		public void Test1() {
			WindsData windData = new WindsData();
			Assert.AreEqual(WindsData.WindsDataType.None, windData.DataType, "New WindsData object");
		}

		[Test][Ignore]
		public void TestFileTypeFromName1() {
			string fileName;
			DacDataFileBase.ParsedFileNameStruct fileInfo;
			DataFileType fileType;

			fileName = "H04123a.spc";
			fileType = DacDataFileBase.GetFileTypeFromName(fileName, out fileInfo);
			Assert.AreEqual(DataFileType.PopDayFile, fileType, fileName);
		}

		[Test]
		public void TestFileTypeFromName2() {
			string fileName;
			DacDataFileBase.ParsedFileNameStruct fileInfo;
			DataFileType fileType;

			fileName = "c:\\something\\D04123a.mom";
			fileType = DacDataFileBase.GetFileTypeFromName(fileName, out fileInfo);
			Assert.AreEqual(DataFileType.PopDayFile, fileType, fileName);
		}

		[Test]
		public void TestPdtArrays1() {
			string fileName;
			fileName = @"C:\MyProjects8\dotNet2005\DacDataFileTest\gal_ob_2001_009.txt";
			DacPdtFile dataFile = new DacPdtFile();
			dataFile.OpenFileReader(fileName);
			PdtData data = new PdtData();

			// record 1
			// default values for dataFile.Get... properties
			dataFile.ReadNextRecord(data);
			Assert.IsEmpty(data.Speed, "Speed is empty");
			Assert.IsEmpty(data.ObliqueSNR, "Obliq SNR is empty");
			Assert.IsEmpty(data.VerticalSNR, "Vert SNR is empty");
			Assert.IsNotEmpty(data.UWind, "UWind is not empty");
			Assert.IsTrue(data.NHts <= data.UWind.Length, "UWind[NHts]");
			Assert.IsTrue(data.NHts <= data.WWind.Length, "WWind[NHts]");
			Assert.AreEqual(0.292, data.Hts[0], "Rec 1, Hts[0] value");
			Assert.AreEqual(0.312, data.VertHts[0], "Rec 1, VertHts[0] value");
			Assert.AreEqual(9999.0, data.UWind[0], "Rec 1, UWind[0] value");
			Assert.IsTrue(data.HasHorizontal, "Rec 1 HasHorizontal test" );
			Assert.IsTrue(data.HasVertical, "Rec 1 HasVertical test");
			Assert.IsTrue(data.HasWinds, "Rec 1 HasWinds test");
			Assert.IsFalse(data.HasSNR, "Rec 1 HasSNR test");
			Assert.IsTrue(data.HasVerticalHts, "Rec 1 HasVertHts test");
			Assert.IsTrue(data.NHts <= data.VertHts.Length, "Rec 1 VertHts[NHts]");

			// record 2
			dataFile.GetWinds = false;
			dataFile.GetVertical = true;
			dataFile.GetHorizontal = false;
			dataFile.GetSNR = false;
			dataFile.ReadNextRecord(data);
			Assert.IsEmpty(data.Speed, "testing Speed empty no winds");
			Assert.IsEmpty(data.UWind, "testing UWind empty no winds");
			Assert.AreEqual(0, data.UWind.Length, "testing UWind[0] no winds");
			Assert.AreEqual(0, data.WWind.Length, "testing WWind[0] no winds");
			Assert.IsEmpty(data.ObliqueSNR, "Obliq SNR is empty no winds");
			Assert.IsEmpty(data.VerticalSNR, "Vert SNR is empty no winds");
			Assert.IsFalse(data.HasHorizontal, "Rec 2 HasHorizontal test");
			Assert.IsTrue(data.HasVertical, "Rec 2 HasVertical test");
			Assert.IsFalse(data.HasWinds, "Rec 2 HasWinds test");
			Assert.IsFalse(data.HasSNR, "Rec 2 HasSNR test");

			// record 3
			dataFile.GetWinds = true;
			dataFile.GetVertical = false;
			dataFile.GetHorizontal = true;
			dataFile.GetSNR = false;
			dataFile.ReadNextRecord(data);
			Assert.IsEmpty(data.Speed, "testing Speed empty no vert");
			Assert.IsNotEmpty(data.UWind, "testing UWind not empty no vert");
			Assert.IsTrue(data.NHts <= data.UWind.Length, "testing UWind[NHts] no vert");
			Assert.AreEqual(0, data.WWind.Length, "testing WWind[0] no vert");
			Assert.IsEmpty(data.ObliqueSNR, "Obliq SNR is empty no vert");
			Assert.IsEmpty(data.VerticalSNR, "Vert SNR is empty no vert");
			Assert.AreEqual(-2.47, data.UWind[0], "Rec 3, UWind[0] value, no vert");
			Assert.AreEqual(0.36, data.VWind[0], "Rec 3, VWind[0] value, no vert");
			Assert.IsTrue(data.HasHorizontal, "Rec 3 HasHorizontal test");
			Assert.IsFalse(data.HasVertical, "Rec 3 HasVertical test");
			Assert.IsTrue(data.HasWinds, "Rec 3 HasWinds test");
			Assert.IsFalse(data.HasSNR, "Rec 3 HasSNR test");
			Assert.IsFalse(data.HasVerticalHts, "Rec 3 HasVertHts test");
			Assert.AreEqual(0,data.VertHts.Length, "Rec 3 VertHts[NHts]");

			// record 4
			dataFile.GetWinds = true;
			dataFile.GetVertical = true;
			dataFile.GetHorizontal = false;
			dataFile.GetSNR = false;
			dataFile.ReadNextRecord(data);
			Assert.IsEmpty(data.Speed, "testing Speed empty no horz");
			Assert.IsEmpty(data.UWind, "testing UWind empty no horz");
			Assert.AreEqual(0, data.UWind.Length, "testing UWind[0] no horz");
			Assert.IsTrue(data.NHts <= data.WWind.Length, "testing WWind[NHts] no horz");
			Assert.IsEmpty(data.ObliqueSNR, "Obliq SNR is empty no horz");
			Assert.IsEmpty(data.VerticalSNR, "Vert SNR is empty no horz");
			Assert.AreEqual(-0.35, data.WWind[0], "Rec 4, WWind[0] value, no horz");
			Assert.IsFalse(data.HasHorizontal, "Rec 4 HasHorizontal test");
			Assert.IsTrue(data.HasVertical, "Rec 4 HasVertical test");
			Assert.IsTrue(data.HasWinds, "Rec 4 HasWinds test");
			Assert.IsFalse(data.HasSNR, "Rec 4 HasSNR test");

			// record 5
			dataFile.GetWinds = true;
			dataFile.GetVertical = true;
			dataFile.GetHorizontal = true;
			dataFile.GetSNR = true;
			dataFile.ReadNextRecord(data);
			Assert.IsTrue(data.NHts <= data.WWind.Length, "testing WWind[NHts] all");
			Assert.IsTrue(data.NHts <= data.UWind.Length, "testing UWind[NHts] all");
			Assert.IsTrue(data.NHts <= data.VWind.Length, "testing VWind[NHts] all");
			Assert.IsTrue(data.NHts <= data.VerticalSNR.Length, "testing VSNR[NHts] all");
			Assert.IsTrue(data.NHts <= data.ObliqueSNR.Length, "testing oSNR[NHts] all");
			Assert.AreEqual(-2.73, data.UWind[0], "Rec 5, UWind[0] value, all");
			Assert.AreEqual(2.83, data.VWind[0], "Rec 5, VWind[0] value, all");
			Assert.AreEqual(-0.10, data.WWind[0], "Rec 5, WWind[0] value, all");
			Assert.AreEqual(-18.0, data.ObliqueSNR[data.NHts - 1], "Rec 5, vSNR[Nhts-1] value, all");
			Assert.AreEqual(9999.0, data.ObliqueSNR[data.NHts - 2], "Rec 5, vSNR[Nhts-2] value, all");
			Assert.IsTrue(data.HasHorizontal, "Rec 5 HasHorizontal test");
			Assert.IsTrue(data.HasVertical, "Rec 5 HasVertical test");
			Assert.IsTrue(data.HasWinds, "Rec 5 HasWinds test");
			Assert.IsTrue(data.HasSNR, "Rec 5 HasSNR test");
			Assert.IsTrue(data.HasVerticalHts, "Rec 5 HasVertHts test");
			Assert.IsTrue(data.NHts <= data.VertHts.Length, "Rec 5 VertHts[NHts]");

			// record 6
			dataFile.GetWinds = false;
			dataFile.GetVertical = false;
			dataFile.GetHorizontal = true;
			dataFile.GetSNR = true;
			dataFile.ReadNextRecord(data);
			Assert.IsEmpty(data.Speed, "testing Speed empty no vert, SNR");
			Assert.IsEmpty(data.UWind, "testing UWind empty no vert, SNR");
			Assert.IsTrue(data.NHts <= data.ObliqueSNR.Length, "testing oSNR[NHts] no vert, SNR");
			Assert.IsEmpty(data.VerticalSNR, "Vert SNR is empty no vert, SNR");
			Assert.IsTrue(data.HasHorizontal, "Rec 6 HasHorizontal test");
			Assert.IsFalse(data.HasVertical, "Rec 6 HasVertical test");
			Assert.IsFalse(data.HasWinds, "Rec 6 HasWinds test");
			Assert.IsTrue(data.HasSNR, "Rec 6 HasSNR test");
			Assert.IsFalse(data.HasVerticalHts, "Rec 6 HasVertHts test");
			Assert.AreEqual(0, data.VertHts.Length, "Rec 6 VertHts[NHts]");

			// record 7
			dataFile.GetWinds = false;
			dataFile.GetVertical = true;
			dataFile.GetHorizontal = false;
			dataFile.GetSNR = true;
			dataFile.ReadNextRecord(data);
			Assert.IsEmpty(data.Speed, "testing Speed empty no horz, SNR");
			Assert.IsEmpty(data.UWind, "testing UWind empty no horz, SNR");
			Assert.IsTrue(data.NHts <= data.VerticalSNR.Length, "testing vSNR[NHts] no horz, SNR");
			Assert.IsEmpty(data.ObliqueSNR, "Obliq SNR is empty no horz, SNR");
			Assert.AreEqual(0.292, data.Hts[0], "Rec 7, Hts[0] value");
			Assert.AreEqual(0.312, data.VertHts[0], "Rec 7, VertHts[0] value");
			Assert.AreEqual(-8.0, data.VerticalSNR[0], "Rec 7, vSNR[0] value, no horz SNR");
			Assert.IsFalse(data.HasHorizontal, "Rec 7 HasHorizontal test");
			Assert.IsTrue(data.HasVertical, "Rec 7 HasVertical test");
			Assert.IsFalse(data.HasWinds, "Rec 7 HasWinds test");
			Assert.IsTrue(data.HasSNR, "Rec 7 HasSNR test");
			Assert.IsTrue(data.HasVerticalHts, "Rec 7 HasVertHts test");
			Assert.IsTrue(data.NHts <= data.VertHts.Length, "Rec 7 VertHts[NHts]");
		}


		[Test]
		public void TestCnsArrays1() {
			string fileName;
			fileName = @"C:\MyProjects8\dotNet2005\DacDataFileTest\W05046.CNS";
			DacCnsFile dataFile = new DacCnsFile();
			dataFile.OpenFileReader(fileName);
			WindsData data = new WindsData();

			// record 1
			dataFile.ReadNextRecord(data);
			Assert.IsTrue(data.HasHorizontal, "Rec 1 HasHorizontal test");
			Assert.IsTrue(data.HasVertical, "Rec 1 HasVertical test");
			Assert.IsTrue(data.HasWinds, "Rec 1 HasWinds test");
			Assert.IsFalse(data.HasSNR, "Rec 1 HasSNR test");
			Assert.IsFalse(data.HasVerticalHts, "Rec 1 HasVertHts test");
			Assert.IsNotEmpty(data.Speed, "Rec 1, Speed not is empty");
			Assert.IsEmpty(data.ObliqueSNR, "Rec 1, Obliq SNR is empty");
			Assert.IsEmpty(data.VerticalSNR, "Rec 1, Vert SNR is empty");
			Assert.IsEmpty(data.UWind, "Rec 1, UWind is empty");
			Assert.IsTrue(data.NHts <= data.Speed.Length, "Rec 1, Speed[NHts]");
			Assert.IsTrue(data.NHts <= data.Direction.Length, "Rec 1, Direction[NHts]");
			Assert.AreEqual(9.5, data.Speed[0], "Rec 1, Speed[0] value");
			Assert.AreEqual(320, data.Direction[0], "Rec 1, Direction[0] value");
			Assert.AreEqual(-0.1, data.WWind[0], "Rec 1, Direction[0] value");
			Assert.AreEqual(0.182 + 0.349, data.Hts[0], "Rec 1, Hts[0] value");
			Assert.AreEqual(46, data.NHts, "Rec 1, NHts value");

			// record 2
			dataFile.GetWinds = true;
			dataFile.GetVertical = true;
			dataFile.GetHorizontal = true;
			dataFile.GetSNR = true;
			dataFile.ReadNextRecord(data);
			Assert.IsNotEmpty(data.ObliqueSNR, "Rec 2, Obliq SNR is not empty");
			Assert.IsNotEmpty(data.VerticalSNR, "Rec 2, Vert SNR is not empty");
			Assert.IsTrue(data.NHts <= data.Speed.Length, "Rec 2, Speed[NHts]");
			Assert.IsTrue(data.NHts <= data.Direction.Length, "Rec 2, Direction[NHts]");
			Assert.IsTrue(data.NHts <= data.ObliqueSNR.Length, "Rec 2, oSNR[NHts]");
			Assert.IsTrue(data.NHts <= data.VerticalSNR.Length, "Rec 2, vSNR[NHts]");
			Assert.AreEqual(0.588 + 0.349, data.Hts[0], "Rec 2, Hts[0] value");
			Assert.AreEqual(36, data.NHts, "Rec 1, NHts value");

			// record 3
			dataFile.GetWinds = true;
			dataFile.GetVertical = true;
			dataFile.GetHorizontal = false;
			dataFile.GetSNR = true;
			dataFile.ReadNextRecord(data);
			Assert.IsFalse(data.HasHorizontal, "Rec 3 HasHorizontal test");
			Assert.IsTrue(data.HasVertical, "Rec 3 HasVertical test");
			Assert.IsTrue(data.HasWinds, "Rec 3 HasWinds test");
			Assert.IsTrue(data.NHts <= data.WWind.Length, "Rec 3, Speed[NHts]");
			Assert.IsEmpty(data.Speed, "Rec 3, Speed is empty");
			Assert.AreEqual(46, data.NHts, "Rec 3, NHts value");
			Assert.AreEqual(11.0, data.VerticalSNR[0], "Rec 3, vSNR[0] value");

			// record 4
			dataFile.GetWinds = false;
			dataFile.GetVertical = false;
			dataFile.GetHorizontal = true;
			dataFile.GetSNR = true;
			dataFile.ReadNextRecord(data);
			Assert.IsTrue(data.HasHorizontal, "Rec 4 HasHorizontal test");
			Assert.IsFalse(data.HasVertical, "Rec 4 HasVertical test");
			Assert.IsFalse(data.HasWinds, "Rec 4 HasWinds test");
			Assert.IsTrue(data.NHts <= data.ObliqueSNR.Length, "Rec 4, oSNR[NHts]");
			Assert.IsEmpty(data.Speed, "Rec 4, Speed is empty");
			Assert.IsEmpty(data.VerticalSNR, "Rec 4, vSNR is empty");
			Assert.IsEmpty(data.WWind, "Rec 4, wWind is empty");
			Assert.AreEqual(36, data.NHts, "Rec 4, NHts value");
			Assert.AreNotEqual(0.0, data.ObliqueSNR[0], "Rec 4, oSNR[0] not 0 value");
			Assert.IsTrue(data.ObliqueSNR[0] > 19.0f, "Rec 4, oSNR[0]  > value");
			Assert.IsTrue(data.ObliqueSNR[0] < 20.0f, "Rec 4, oSNR[0]  < value");
		}

	}
}
