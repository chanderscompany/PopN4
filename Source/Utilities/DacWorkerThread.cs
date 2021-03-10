using System.ComponentModel;

namespace DACarter.Utilities {

	/// <summary>
	/// Abstract base class to perform operations in another thread.
	/// Uses .NET2005 BackgroundWorker class.
	/// David A Carter, NOAA/OAR/ESRL/PSD, 
	/// 14 Nov. 2005
	/// Latest Rev: 1.0  16Nov2005
	/// </summary>
	/// <remarks>
	/// How to use:
	/// Derive your own class from DacWorkerThread:
	///		MyWorkerThread : DacWorkerThread
	/// In the derived class override the OnDoWork method.
	/// This method executes in the secondary thread.
	/// 
	/// In the calling thread:
	/// Create an instance of the derived worker thread class:
	///		MyWorkerThread worker = new MyWorkerThread();
	/// Provide event handlers for 2 worker events:
	///		worker.ProgressUpdated += OnProgressUpdated;
	///		worker.WorkCompleted += OnWorkCompleted;
	/// The first event is fired when the worker thread calls the
	///		base class method DacWorkerThread._ReportProgress()
	/// The second event is fired when the worker thread 
	///		exits the OnDoWork() method.
	/// Signatures of the event handlers:
	///		OnProgressUpdated(object sender, ProgressChangedEventArgs progressArgs)
	///		OnCompleted(object sender, RunWorkerCompletedEventArgs completedArgs)
	/// The arguments of the event handlers:
	///		progressArgs is set by the worker thread and contains
	///			int progressArgs.ProgressPercentage, and
	///			object progressArgs.UserState
	///		completedArgs is set by the worker thread and contains
	///			bool completedArgs.Cancelled
	///			Exception completedArgs.Error	// contains uncaught exception from worker
	///			object completedArgs.Result		// can be accessed only if no error and not cancelled
	/// Start the worker thread by
	///		MyWorkerThread.Start() or MyWorkerThread.Start(obj);
	/// Cancel the worker thread by
	///		MyWorkerThread.Cancel();
	/// Test status of worker thread by checking
	///		bool MyWorkerThread.IsBusy
	/// 
	/// In the worker thread OnDoWork method:
	/// Signature:
	///		void OnDoWork(object sender, DoWorkEventArgs doWorkArgs)
	/// The argument doWorkArgs contains
	///		object doWorkArgs.Argument;	// argument passed to Start() method
	///		bool doWorkArgs.Cancelled;	// set by worker and sent to OnCompleted handler
	///		object doWorkArgs.Result;	// set by worker and sent to OnCompleted handler
	/// Worker thread should check _CancellationPending property
	///		of the base class and if set, then
	///		set doWorkArgs.Cancelled and exit.
	/// Worker thread should call base class method 
	///		_ReportProgress(int progress) or _ReportProgress( int prog, object UserState)
	///		to send intermediate data to calling thread before completion.
	/// 
	/// Note on passing data to calling thread:
	///		If sending a reference type in UserState or Result
	///			then this object will be modified in the calling thread 
	///			whenever the worker thread modifies it.
	///		If sending a value type (e.g. struct, int, etc.),
	///			then a copy is sent to calling thread and will not change
	///			-- except for struct members that are reference types.
	///		But remember that a value type must be boxed to be sent as object type.
	/// 
	/// To guarantee that the calling thread keeps up with the worker thread as the
	///		calling thread handles the ProgressUpdated event,
	///		the caller can set the WaitForProgressHandled property of
	///		the worker thread to true AND then call the 
	///		ThreadContinue() method of the worker thread when finished handling
	///		the ProgressUpdated event.
	/// </remarks>
	public abstract class DacWorkerThread {

		#region Private Fields
		private BackgroundWorker _bgWorker;
		private RunWorkerCompletedEventHandler _workCompleted;
		private ProgressChangedEventHandler _progressUpdated;
		private bool _wait;
		private bool _waitForProgressHandled;
		#endregion

		public DacWorkerThread() {

			_bgWorker = new BackgroundWorker();
			_bgWorker.WorkerReportsProgress = true;
			_bgWorker.WorkerSupportsCancellation = true;

			_workCompleted = null;
			_progressUpdated = null;

			_bgWorker.DoWork += OnDoWork;

			_wait = false;
			_waitForProgressHandled = false;
		}


		#region Public Abstract Method
		public abstract void OnDoWork(object sender, DoWorkEventArgs doWorkArgs);
		#endregion

		#region Public Methods
		public void Start() {
			if (!_bgWorker.IsBusy) {
				_bgWorker.RunWorkerAsync();
			}
		}

		public void Start(object arg) {
			if (!_bgWorker.IsBusy) {
				_bgWorker.RunWorkerAsync(arg);
			}
		}

		public void Cancel() {
			_bgWorker.CancelAsync();
		}

		public void ThreadWait() {
			_wait = true;
		}

		public void ThreadContinue() {
			_wait = false;
		}

		#endregion

		#region Public Properties
		public bool IsBusy {
			get { return _bgWorker.IsBusy; }
		}

		public bool WaitForProgressHandled {
			get { return _waitForProgressHandled; }
			set {
				// tell worker thread whether it 
				// should wait for _wait flag to clear
				_waitForProgressHandled = value;
				if (value == true) {
					// if so, initialize _wait flag
					_wait = true;
				}
				else {
					_wait = false;
				}
			}
		}

		public RunWorkerCompletedEventHandler WorkCompleted {
			set {
				_workCompleted = value;
				_bgWorker.RunWorkerCompleted += _workCompleted;
			}
			get { return _workCompleted; }
		}

		public ProgressChangedEventHandler ProgressUpdated {
			set {
				_progressUpdated = value;
				_bgWorker.ProgressChanged += _progressUpdated;
			}
			get { return _progressUpdated; }
		}
		#endregion

		#region Protected Methods
		protected void _ReportProgress(int percentProgress, object userState) {
			_bgWorker.ReportProgress(percentProgress, userState);
			if (WaitForProgressHandled) {
				while (_wait) {
				}
				_wait = true;
			}
		}

		protected void _ReportProgress(int percentProgress) {
			_bgWorker.ReportProgress(percentProgress);
		}
		#endregion

		#region Protected Properties
		protected bool _CancellationPending {
			get { return _bgWorker.CancellationPending; }
		}
		#endregion

	}  // end abstract class DacWorkerThread


	/*
	/////////////////////////////////////////////////////////////////////
	// Sample client
	//
	/////////////////////////////////////////////////////////////////////
		static void Main() {

			mainForm = new Form1();
			workerThread = new TestWorkerThread();
			//workerThread.WaitForProgressHandled = true;

			mainForm.ButtonStart.Click += ButtonStart_Click;
			mainForm.ButtonCancel.Click += ButtonCancel_Click;

			workerThread.ProgressUpdated += OnProgressUpdated;
			workerThread.WorkCompleted += OnCompleted;

			Application.Run(mainForm);
		}


		private static void ButtonStart_Click(object sender, EventArgs e) {
			workerThread.Start();
		}
		private static void ButtonCancel_Click(object sender, EventArgs e) {
			workerThread.Cancel();
		}

		public static void OnProgressUpdated(object sender, ProgressChangedEventArgs progressArgs) {
			int progress = progressArgs.ProgressPercentage;
			string msg = "Completed " + progress;
			mainForm.ListBoxMessages.Items.Add(msg);
			TestWorkerThread.MyStruct threadData, threadData2 ;
			threadData = (TestWorkerThread.MyStruct)progressArgs.UserState;
			threadData2 = workerThread.dataStruct;
			int value = workerThread.Value;
			mainForm.ListBoxMessages.Items.Add("   sent data: " + threadData.MyInt + threadData.MyString + threadData.MyIntArray[0]);
			mainForm.ListBoxMessages.Items.Add("   read data: " + threadData2.MyInt + threadData2.MyString + " " + threadData.MyIntArray[0]);
			if (workerThread.WaitForProgressHandled) {
				workerThread.ContinueThread();
			}
		}

		public static void OnCompleted(object sender, RunWorkerCompletedEventArgs completedArgs) {
			if (completedArgs.Cancelled) {
				mainForm.ListBoxMessages.Items.Add("Status: Cancelled");
			}
			else if (completedArgs.Error != null) {
				mainForm.ListBoxMessages.Items.Add("Error: " + completedArgs.Error.Message);
			}
			else {
				mainForm.ListBoxMessages.Items.Add("Status: Completed");
				object ss = completedArgs.UserState;
				if (completedArgs.Result != null) {
					mainForm.ListBoxMessages.Items.Add((string)completedArgs.Result);
				}
			}
		}
	 * 
	 ////////////////////////////////////////////////////////////
	 * 
	 * Sample derived worker thread
	 * 
	 ///////////////////////////////////////////////////////////
	public class TestWorkerThread : DacWorkerThread {

		public MyData Data;
		public MyStruct dataStruct;
		public int Value;

		public override void OnDoWork(object sender, DoWorkEventArgs doWorkArgs) {
			int count = 500;
			doWorkArgs.Result = "nothing";

			dataStruct.MyIntArray = new int[3];

			for (int progress = 0; progress <= count; progress += count / 10) {
				if (_CancellationPending) {
					doWorkArgs.Cancel = true;
					break;
				}
				Thread.Sleep(500);

				dataStruct.MyInt = 44;
				dataStruct.MyString = "-Good-";
				dataStruct.MyIntArray[0] = 3;

				// report info to calling thread
				_ReportProgress(progress, dataStruct);

				// modify data, see what changes in calling thread
				dataStruct.MyString = "xxBADxx";// this won't
				dataStruct.MyInt = 99;			// this won't
				dataStruct.MyIntArray[0] = 9;	// this will
			}

			doWorkArgs.Result = "Finished doing the work.";
		}

		public struct MyStruct {
			public int MyInt;
			public string MyString;
			public int[] MyIntArray;
		}

	}  // end class TestWorkerThread

	
	*/


}  // end namespace
