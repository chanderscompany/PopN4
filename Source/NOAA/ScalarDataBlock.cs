using System;
using System.Collections.Generic;

using DACarter.Utilities;

namespace DACarter.NOAA {

	public class ScalarDataBlock {

		public List<DateTime> Times;
		public List<Double> DataValues;

		private TimeSpan _averagingInterval;
		private TimeSpan _halfAveragingInterval;
		private DateTime _endTimeOfAverage;
		private int _count;
		private double _sum;


		public TimeSpan AveragingInterval {
			get { return _averagingInterval; }
			set {
				_averagingInterval = value;
				double seconds = _averagingInterval.TotalSeconds;
				_halfAveragingInterval = TimeSpan.FromSeconds(seconds / 2.0);
			}
		}

		// constructors:
		//	Initialize averaging interval
		public ScalarDataBlock() {
			// default to 1-minute averages
			AveragingInterval = new TimeSpan(0, 1, 0);
			Init();
		}

		public ScalarDataBlock(int averagingMinutes) {
			AveragingInterval = new TimeSpan(0, averagingMinutes, 0);
			Init();
		}

		public ScalarDataBlock(TimeSpan averagingInterval) {
			AveragingInterval = averagingInterval;
			Init();
		}

		private void Init() {
			_endTimeOfAverage = DateTime.MinValue;
			_count = 0;
			_sum = 0.0;
			Times = new List<DateTime>();
			DataValues = new List<double>();
		}

		public void Add(DateTime time, double data) {
			if (time >= _endTimeOfAverage) {
				// outside of averaging interval
				TerminateCurrentAverage();
				// start new average
				_endTimeOfAverage = Tools.GetTimeIntervalBoundary(time, AveragingInterval);
				_count = 0;
				_sum = 0;
			}
			// add new data to sum
			_sum += data;
			_count++;
		}

		public void TerminateCurrentAverage() {
			if (_endTimeOfAverage != DateTime.MinValue) {
				// if previous data, average and store it
				if (_count > 0) {
					double averageValue = _sum / _count;
					DataValues.Add(averageValue);
					Times.Add(_endTimeOfAverage - _halfAveragingInterval);
				}
			}
		}
	}

}
