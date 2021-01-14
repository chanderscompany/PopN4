using System.Collections;
using DACarter.Collections;

namespace DACarter.NOAA
{
	public class ComDataBlockCollection : CollectionWithEvents
	{
		public int Add(DACarter.NOAA.ComDataBlock value)
		{
			return base.List.Add(value as object);
		}

		public void Remove(DACarter.NOAA.ComDataBlock value)
		{
			base.List.Remove(value as object);
		}

		public void Insert(int index, DACarter.NOAA.ComDataBlock value)
		{
			base.List.Insert(index, value as object);
		}

		public bool Contains(DACarter.NOAA.ComDataBlock value)
		{
			return base.List.Contains(value as object);
		}

		public DACarter.NOAA.ComDataBlock this[int index]
		{
			get { return (base.List[index] as DACarter.NOAA.ComDataBlock); }
		}
	}
}
