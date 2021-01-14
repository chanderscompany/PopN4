using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.ComponentModel;

namespace DACarter.Utilities {
	//////////////////////////////////////////////////////////////////////////
	///
	/// <summary>
	/// SimpleWorkerThread
	/// Quick way to execute a method asynchronously in a background thread.
	///		
	/// Method to run in background must have one argument of type 'object' and return 'object'.
	///		object workerFunc(object arg);  // arg can be ignored and return value can be null
	///	Method to execute when background work completed must return void and have 2 arguments
	///		which will contain return object from background method and its argument:
	///		void completedFunc(object result, object arg);
	///	Usage example:
	///		_simpleWorker = new SimpleWorkerThread();
	///		_simpleWorker.SetWorkerMethod(workerFunc);
	///		_simpleWorker.SetCompletedMethod(completedFunc);  // optional
	///		_simpleWorker.Go(arg);  // where arg is argument object to send to workerFunc
	///	Signatures:
	///		object workerFunc(object arg); 
	///		void completedFunc(object result, object arg);
	///
	/// </summary>
	/// 
	/// <remarks>
	/// The basic useage of this class does not allow for premature cancellation of the worker.
	/// To handle cancellation, the worker and completed methods must match the signatures:
	///		object workerFunc(object arg, out bool cancelled);  or
	///		object workerFunc(object arg, out bool cancelled, BackgroundWorker backWorker);  
	///		void completedFunc(object result, object arg, bool cancelled);
	///	workerFunc sets 'cancelled' true if it aborts early.
	///	External cancellation happens by calling SimpleWorkerThread.Cancel() in main thread.
	///		Then workerFunc checks backWorker.CancellationPending for true.
	///		
	/// For progress update:
	///		_simpleWorker.SetProgressMethod(progressFunc);
	///	where
	///		void progressFunc(int percentProgress, object userState);
	///	Worker method calls
	///		backWorker.ReportProgress(int percentProgress); or
	///		backWorker.ReportProgress(int percentProgress, object userState);
	/// </remarks>
	/// 
	public class SimpleWorkerThread {

		// Delegate type defined
		public delegate object WorkerDelegate(object arg);
		public delegate object WorkerDelegate2(object arg, out bool Cancelled);
		public delegate object WorkerDelegate3(object arg, out bool Cancelled, BackgroundWorker backWorker);
		public delegate void CompletedDelegate(object result, object arg);
        public delegate void CompletedDelegate2(object result, object arg, bool bCancelled);
        public delegate void CompletedDelegate3(object result, object arg, bool bCancelled, Exception bError);
        public delegate void ProgressDelegate(int progressPercent, object userState);

		// declare private delegate that will hold method to run in background.
		private WorkerDelegate _workerDelegate;
		private WorkerDelegate2 _workerDelegate2;
		private WorkerDelegate3 _workerDelegate3;
		private CompletedDelegate _completedDelegate;
        private CompletedDelegate2 _completedDelegate2;
        private CompletedDelegate3 _completedDelegate3;
        private ProgressDelegate _progressDelegate;

		private BackgroundWorker _worker;
		private int _callingThreadID, _workerThreadID, _completedThreadID, progressThreadID;
		private object _argument, _result;
		private bool _isDone;

		// Constructor
		public SimpleWorkerThread() {
			_worker = new BackgroundWorker();
			_worker.WorkerSupportsCancellation = true;
			_worker.WorkerReportsProgress = true;
			_worker.DoWork += DoWorkMethod;
			_worker.RunWorkerCompleted += WorkCompletedHandler;
			_worker.ProgressChanged += ProgressUpdate;
			_callingThreadID = Thread.CurrentThread.ManagedThreadId;
			_isDone = false;
		}

		public bool IsBusy {
			get { return _worker.IsBusy; }
		}
		public bool IsDone {
			get { return _isDone; }
		}

		public bool CancellationPending {
			get { return _worker.CancellationPending; }
		}

		public int CallingThreadID {
			get { return _callingThreadID; }
		}

		public int WorkerThreadID {
			get { return _workerThreadID; }
		}

		//
		/// <summary>
		/// Assign method to execute in background.
		/// Method must be of the form: object func(object arg)
		///		or the form: object func(object arg, out bool cancelled),
		///			where the function will set cancelled if method failed;
		///		or the form: object func(object arg, out bool cancelled, BackgroundWorker bw),
		///			where bw.CancellationPending will be tested for external cancel,
		///			which is set by calling SimpleWorkerThread.Cancel();
		/// </summary>
		/// <param name="func"></param>
		public void SetWorkerMethod(WorkerDelegate func) {
			_workerDelegate = func;
			_workerDelegate2 = null;
			_workerDelegate3 = null;
		}
		//
		public void SetWorkerMethod(WorkerDelegate2 func) {
			_workerDelegate2 = func;
			_workerDelegate3 = null;
			_workerDelegate = null;
		}
		//
		public void SetWorkerMethod(WorkerDelegate3 func) {
			_workerDelegate3 = func;
			_workerDelegate2 = null;
			_workerDelegate = null;
		}
		//
		public void SetProgressMethod(ProgressDelegate func) {
			_progressDelegate = func;
		}

		/// <summary>
		/// Assign method to execute when background thread is finished.
		/// Method must be of form: void func(object result, object arg),
		///		or of the form: void func(object result, object arg, bool cancelled),
		///		where 'result' is the return value of the worker function,
		///		'arg' is the argument that was sent to the worker (possibly modified),
		///		and 'cancelled' is set if the function was exited early.
		/// </summary>
		/// <param name="func"></param>
		public void SetCompletedMethod(CompletedDelegate func) {
			_completedDelegate = func;
		}
		//
        public void SetCompletedMethod(CompletedDelegate2 func) {
            _completedDelegate2 = func;
        }
        //
        public void SetCompletedMethod(CompletedDelegate3 func) {
            _completedDelegate3 = func;
        }


		/// <summary>
		/// Methods to call to begin execution of worker thread.
		/// </summary>
		public void Go() {
			if (!_worker.IsBusy) {
				_isDone = false;
				_result = null;
				_argument = null;
				_worker.RunWorkerAsync();
			}
		}

		public void Go(object arg) {
			if (!_worker.IsBusy) {
				_isDone = false;
				_result = null;
				_argument = null;
				_worker.RunWorkerAsync(arg);
			}
		}

		/// <summary>
		/// Called to instruct the worker thread to immediately abort.
		/// Works only if worker method is of type WorkerDelegate3,
		///		i.e. receives BackgroundWorker reference as 3rd argument,
		///		and tests for BackgroundWorker.CancellationPending,
		///		and sets 2nd argument bCancelled to true;
		/// </summary>
		public void Cancel() {
			_worker.CancelAsync();
		}

		public void Dispose() {
			if (_worker != null) {
				_worker.Dispose();
			}
		}

		/// <summary>
		/// This contains the code that executes in the background thread.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DoWorkMethod(object sender, System.ComponentModel.DoWorkEventArgs e) {

			_workerThreadID = Thread.CurrentThread.ManagedThreadId;

			BackgroundWorker backgroundWorker = sender as BackgroundWorker;
			// To abort worker thread, check backgroundWorker.CancellationPending.
			// If true, set e.Cancel true and exit DoWorkMethod

			if (_workerDelegate != null) {
				e.Result = _workerDelegate(e.Argument);
				_result = e.Result;
				_argument = e.Argument;
			}
			else if (_workerDelegate2 != null) {
				bool bCancelled = false;
				e.Result = _workerDelegate2(e.Argument, out bCancelled);
				_argument = e.Argument;
				if (bCancelled) {
					_result = null;
					e.Cancel = true;
					return;
				}
				else {
					_result = e.Result;
				}
			}
			else if (_workerDelegate3 != null) {
				bool bCancelled = false;
				e.Result = _workerDelegate3(e.Argument, out bCancelled, backgroundWorker);
				_argument = e.Argument;
				if (bCancelled) {
					_result = e.Result;
					e.Cancel = true;
					return;
				}
				else {
					_result = e.Result;
				}
			}
		}  // end of DoWorkMethod()

		private void WorkCompletedHandler(object sender, RunWorkerCompletedEventArgs e) {
            _completedThreadID = Thread.CurrentThread.ManagedThreadId;
            _isDone = true;
			if (_completedDelegate != null) {
				_completedDelegate(_result, _argument);
			}
            else if (_completedDelegate2 != null) {
                _completedDelegate2(_result, _argument, e.Cancelled);
            }
            else if (_completedDelegate3 != null) {
                _completedDelegate3(_result, _argument, e.Cancelled, e.Error);
            }
        }

		private void ProgressUpdate(object sender, ProgressChangedEventArgs e) {
            progressThreadID = Thread.CurrentThread.ManagedThreadId;
            if (_progressDelegate != null) {
				_progressDelegate(e.ProgressPercentage, e.UserState);
			}
		}

	}  // end class SimpleWorkerThread
}
