////////////////////////////////////////////////////////////////////////////////
// FILE NAME: CustomWorkerThread.CPP
//
// DESCRIPTION: This file performs up to three wavelet-clutter
// removal algorithms on the incoming time series data. 
//
//  [TsWA_Wavlet,TsWB_Wavlet]=Wavelet({NULL},{TsWA,TsWB})
//  {
//    iDaubClip                   = 1;
//    fThresholdRatio_DaubClip    = 0.1; 
//    iDeSpike                    = 1;
//    fThresholdRatio_DeSpike     = 0.1; 
//    iHarmonicSub                = 1;
//    fThresholdRatio_HarmonicSub = 0.1; 
//    sOLEID                      = 'Lapxm.Wavelet.1';
//  };
//
// TEMPLATE DESCRIPTION: This file is used as a secondary thread to process 
// data. Almost all of this module's custom code should reside in here. 
// It has been created with a specific structure so that all modules will look 
// and act the same. This will help decrease the time to debug and modify 
// modules. 
//
// PLEASE FOLLOW THE STATED STRUCTURE! 
// Only add custom code where you find "*** comments ***"
//
// When this secondary thread is created, the Start() method is called and
// given a pointer back to the IUnknown interface of the main thread. 
// The Start() method does some interface initialization and then walks 
// through a series of methods. They are Initialize(), WorkerMain() and 
// UnInitialize(). One should put their calls to custom methods into these 
// methods or add new methods to this list. The WorkerMain() method forms 
// an endless loop that calls GetFirstLapxmDataFromQue() and then 
// OutputLapxmData().  These two methods retrieve data and pass it on to 
// the next module via the main thread. In WorkerMain() there is a simple 
// switch that sends the data to the correct Processing method based on the 
// incoming channel. It is in these processing methods that one's custom data 
// processing code should reside. It is recommended that this data processing 
// code be a series of method calls that contain the actual code. This will 
// aid future debugging by others. There are also methods to handle empty 
// LapxmDataStructures that are sent with the lChangeFlag set to Start, Pause, 
// Stop, etc. These methods can be used to store and recover data.
//
// THREADING: All modules are fully multi-threaded and reside in one 
// multi-threaded apartment MTA. Since there are no incoming COM calls into 
// this thread, the code is naturally thread safe. All incoming COM calls 
// should be handled by the main thread for this module.
//
// TIMING: All modules are event driven and the individual threads will be 
// put to sleep if there is nothing for them to do. This happens when a call 
// is made to GetFirstLapxmDataStructureFromQue() or GetLastLapxmDataStructureFromQue() and 
// the queue is empty. Thus do not be suprised if the debugger appears to hang 
// on this call until a data structure is added to the empty queue. This will 
// also happen on OutputLapxmDataStructure() if the queue is full.
//
// STATE CHANGES: Changes in state are requested by sending empty data 
// structures with the Change State flag set to the requested state. These 
// state changes do not really change what is going on in this thread but 
// simply allow it to do some state specific tasks such as writing data to a 
// file on a stop.
//
// DATA: All data is passed inside the LapxmDataStructure which is defined in 
// the DataControlAggregate. If there needs to be a change to this data 
// structure, one of two things can be done. Either the change can be made to 
// the structure and all modules that use that structure recompiled. OR, the 
// methods in the DataControl aggregate can be duplicated with a new interface 
// to pass the new data. This way the old modules will not need to be 
// recompiled since they will be using the the old interface and old data 
// structure. Meanwhile, new modules can take advantage of the new interface, 
// methods and data structure.
//
// MFC - This module does not include MFC. To include it, search for 'MFC' in 
// all files in this project and follow the comments.
//
// EXCEPTIONS: To enable C++ exception handling in your code, open the Project 
// Settings  dialog box, select the C/C++ tab, select C++ Language in the Category 
// box, and  select Enable Exception Handling; or use the /GX compiler option. 
// The default is  /GX-, which disables exception handling unwind semantics.
//
// WRITTEN BY: Coy Chanders 10-1-2002
//
// CUSTOM DESCRIPTION:
//
// LAST MODIFIED BY:
////////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "CustomWorkerThread.h"
#include "math.h"
#include "nsp.h"
#include "nspwlt.h"
#include "nspwin.h"
#include "nspcvrt.h"
#include "nspvec.h"
#include <stdlib.h>
#include "nspfft.h"

int compare( const void *x, const void *y );
int compare_double( const void *x, const void *y ); // RHL 2005-01-18





////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::CCustomWorkerThread()
//
// TEMPLATE DESCRIPTION: Constructor 
//
// CUSTOM DESCRIPTION *** Put your custom description here ***
//
// INPUT:  None
//
// OUTPUT: None
//
// RETURN: None         
//
// WRITTEN BY: Coy Chanders 10-1-2002
//
// LAST MODIFIED BY:
//
////////////////////////////////////////////////////////////////////////////////
CCustomWorkerThread::CCustomWorkerThread()
{

  // initialize state of each channel to unknown

  // Channel 0 - Used for diagnostics only
  m_lCount0 = 0;
  m_iChannel0State = -1;
  
  // Channel 1
  m_bIsFirstData_Channel1 = true;
  strcpy(m_cDwellMode_Channel1,"");
  m_iChannel1State = -1;


  m_iCurrentState = -1;

  // *** Place all member variable initialization here, especially setting pointers to NULL.
  // This is because Unitialize() may be called without Initialize() being called. Thus
  // deletion of pointers may be called if they have not been set to NULL here.


  // *** Place all custom initialization code, especially code that may return a failure,
  // in the Initialize() method instead of here. This is because this constructor can not
  // pass an error code back to the calling method.
}





////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::~CCustomWorkerThread()
//
// TEMPLATE DESCRIPTION: Destructor
//
// CUSTOM DESCRIPTION *** Put your custom description here ***
//
// INPUT:  None
//
// OUTPUT: None
//
// RETURN: None         
//
// WRITTEN BY: Coy Chanders 10-1-2002
//
// LAST MODIFIED BY:
//
////////////////////////////////////////////////////////////////////////////////
CCustomWorkerThread::~CCustomWorkerThread()
{
  // Place custom uninitialization code, especially any code that may return a
  // failure, in the UnInitialize() method instead of here. This is because 
  // this destructor can not pass an error code back to the calling method.
}




////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::Start(void* pIStream)
//
// TEMPLATE DESCRIPTION: This is the method called by the Main Thread to get 
// this secondary thread started. This is where you should walk through 
// methods that do not process data. The methods that process data should be 
// called from within WorkerMain().
//
// Do not put a lot of code here. Instead, simply call other methods. 
// This will make it easier on others to follow the flow of your code.
//
// CUSTOM DESCRIPTION *** Put your custom description here ***
//
// INPUT: 
// void* pIStream - This is a marshalled pointer to the Control IUnknown interface
//
// OUTPUT: None
//
// RETURN: HRESULT hr        
//
// WRITTEN BY: Coy Chanders 10-1-2002
//
// LAST MODIFIED BY:
//
////////////////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::Start(void* pIStream)
{
  // Unmarshall the pointer to this module's controlling IUnknown interface.
  HRESULT hr = CoGetInterfaceAndReleaseStream((IStream*)pIStream, IID_IUnknown, (void **)&m_spIUnknown_Controlling);

  // Get pointer to the  IWavelet interface 
  hr = m_spIUnknown_Controlling->QueryInterface(IID_IWavelet, (void**)&m_spIWavelet);
  if(FAILED(hr))
  {
    return hr;
  }

  // Get pointer to the IControl interface.
  hr = m_spIUnknown_Controlling->QueryInterface(IID_IControl, (void**)&m_spIControl);
  if(FAILED(hr))
  {
    return hr;
  }

  // Get pointer to the IDataControl interface.
  hr = m_spIUnknown_Controlling->QueryInterface(IID_IDataControl, (void**)&m_spIDataControl);
  if(FAILED(hr))
  {
    return hr;
  }

  // Send a message to note that the secondary thread was started.
  m_spIControl->LapxmSendMessage(MESSAGE_COM_FRAMEWORK, CBstr("Secondary Thread Started"));

	
  // *** Put your Custom Code here ***

  // Walk through calls to other methods that contain your code.
  // This will make it easier for others to follow the flow of your code.
  // This section should only have calls to methods in it. All other code 
  // should be in those methods. 

  hr = Initialize();
  if(FAILED(hr))
  {
    hr = UnInitialize();
    m_spIControl->LapxmSendMessage(MESSAGE_MODULE_SHUTDOWN, CBstr("Module initialization failed."));
    return hr;
  }

  hr = WorkerMain(); // Call data processing methods from within this method
  if(FAILED(hr))
  {
    hr = UnInitialize();
    m_spIControl->LapxmSendMessage(MESSAGE_MODULE_SHUTDOWN, CBstr("CCustomWorkerThread::WorkerMain() failed."));
    return hr;
  }


  hr = UnInitialize();
  if(FAILED(hr))
  {
    m_spIControl->LapxmSendMessage(MESSAGE_MODULE_SHUTDOWN, CBstr("Module uninitialization failed."));
    return hr;
  }


  // *** End Custom Code ***

  return S_OK;
}





////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::Initialize()
//
// TEMPLATE DESCRIPTION: Gets a pointer to the Configuration module.
// Gets the name assigned to this module in the Configuration module.
// This is where one should look up parameters in the Configuration file.
//
// CUSTOM DESCRIPTION *** Put your custom description here ***
//
// INPUT:  None
//
// OUTPUT: None
//
// RETURN: HRESULT hr        
//
// WRITTEN BY: Coy Chanders 10-1-2002
//
// LAST MODIFIED BY:
// Raisa Lehtinen 2005-01-07 / Added using CLapLogger class.
////////////////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::Initialize()
{
  HRESULT hr;

  // Get this module's name.
  BSTR bsTemp;
  hr = m_spIControl->GetModuleName(&bsTemp);
  if (FAILED(hr))
  {
    m_spIControl->LapxmSendMessage(MESSAGE_ERROR, 
      CBstr("CCustomWorkerThread::Initialize() - Could not get the name of module from the ControlAggregate."));
    return E_FAIL;
  }
  m_cbsModuleName = CBstr(bsTemp, false);


  // Create and initialize CLapConfig object for reading parameters from the Configuration Object.
  CLapConfig Config;
  Config.InitializeConfigAccess(m_spIControl);
  

  // Read input (Channel 1) and output data product names from this module.
  CBstr cbsModule = m_cbsModuleName; // Use shorter name.
  Config.ReadModuleInputsChannel1(cbsModule, &m_vecInputNames);
  Config.ReadModuleOutputs(cbsModule, &m_vecOutputNames);

  bool bErrorFlag = false; // Flag to indicate if any of the calls to Configuration fails.
  int  iNInputs   = (int) m_vecInputNames.size(); // number of input data to the module
  
  // Read the optional iDebug configuration parameter. Default is 0. If set >0, show 
  // information on missing parameters.
  Config.ReadParameter(SectDataProc, &cbsModule, "iDebug", false, 0, 9999, CFG_TEST_MIN, 
    NULL, 0, 0, 1, &bErrorFlag, &m_iDebug);
  

  
  // *** Begin custom code for Wavelet module. ***

  // Check that the number of inputs and outputs matches for this module.
  if ( m_vecOutputNames.size() != m_vecInputNames.size() )
  {
    m_spIControl->LapxmSendMessage(MESSAGE_ERROR, 
      CBstr("Number of outputs from this module must match the number of inputs on Channel 1."));
    bErrorFlag = true;
  }
  

  // Description strings for the parameters of this module.
  CBstr cbsDaubClip("0 = No, 1 = Yes apply Daubechies clipping algorithm for reducing persistent clutter.");
  CBstr cbsDeSpike("0 = No, 1 = Yes apply spike removal algorithm for reducing intermittent large signals.");
  CBstr cbsHarmonicSub("0 = No, 1 = Yes apply harmonic wavelet subtraction algorithm for reducing birds or similar interference.");
  CBstr cbsThreshold("Clipping threshold ratio. Clips large coefficients at median/threshold ratio.");
  CBstr cbsMinThres("Minimum wavelet transform level used in finding the clipping threshold in harmonic method.");
  CBstr cbsSymmThres("0 = Compute separate thresholds for left and right side of the transform. 1 = Compute symmetric (averaged) thresholds.");
  CBstr cbsThresMulti("Multiply threshold value (= standard deviation) to avoid clipping too much signal.");
  CBstr cbsNumSegment("Number of segments used in DaubClip threshold estimation.");
  CBstr cbsLogFileGate("Range gate from which to write data to the log file.");


  // Parameter declarations and initializations
  int   iLogLevel;
  CBstr cbsLogPath;
  CBstr cbsLogFormat;
  CBstr cbsLogFormatDef = "Plain";
  CBstr cbsValidFormats = "Matlab,Plain";
  
  // *** Declare parameters that are not member variables of this module, and initialize needed parameters. ***
  // Parameter declarations and initializations

  // Read the parameters from the Configuration Object. Short instructions:
  // ----------------------------------------------------------------------
  // Required: true = required parameter, false = optional parameter
  // Min/Max:  min and max valid values. Limits_to_test defines which limits are enforced (min, max, both or none)
  // Default:  default value is used only for optional parameters missing from the configuration file
  // Debug:    optional "iDebug" parameter defines if "debug" messages are shown. Default is 0 (no messages).
  // NValues:  number of values in the parameter. List type parameters typically must have as many values as there are inputs to the module.
  //           Set to -1 if the list type parameter can have an arbitrary number of values.
  // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  //                   Section       Subsection  Name                            Required  Min     Max    Limits_to_test   Custom_message   Default  Debug     NValues   Error_flag   Parameter
  // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Config.ReadParameter(SectDataProc, &cbsModule, "iDaubClip",                    true,     0,      1,     CFG_TEST_VALUES, &cbsDaubClip,    0,       m_iDebug, iNInputs, &bErrorFlag, &m_vecDaubClip);
  Config.ReadParameter(SectDataProc, &cbsModule, "iDeSpike",                     true,     0,      1,     CFG_TEST_VALUES, &cbsDeSpike,     0,       m_iDebug, iNInputs, &bErrorFlag, &m_vecDeSpike);
  Config.ReadParameter(SectDataProc, &cbsModule, "iHarmonicSub",                 true,     0,      1,     CFG_TEST_VALUES, &cbsHarmonicSub, 0,       m_iDebug, iNInputs, &bErrorFlag, &m_vecHarmonicSub);

  Config.ReadParameter(SectDataProc, &cbsModule, "fThresholdRatio_DaubClip",     false,    0.0,    1.0,   CFG_TEST_MINMAX, &cbsThreshold,   0.3,     m_iDebug, iNInputs, &bErrorFlag, &m_vecThresholdRatio_DaubClip);
  Config.ReadParameter(SectDataProc, &cbsModule, "fThresholdRatio_DeSpike",      false,    0.0,    1.0,   CFG_TEST_MINMAX, &cbsThreshold,   0.3,     m_iDebug, iNInputs, &bErrorFlag, &m_vecThresholdRatio_DeSpike);
  Config.ReadParameter(SectDataProc, &cbsModule, "iNumberOfSegments_DaubClip",   false,    3,      4,     CFG_TEST_MINMAX, &cbsNumSegment,  4,       m_iDebug, iNInputs, &bErrorFlag, &m_vecNumberOfSegments_DaubClip);
  Config.ReadParameter(SectDataProc, &cbsModule, "iMinThresholdLevel_Harmonic",  false,    4,      8,     CFG_TEST_MINMAX, &cbsMinThres,    6,       m_iDebug, iNInputs, &bErrorFlag, &m_vecMinThresholdLevel_Harmonic);
  Config.ReadParameter(SectDataProc, &cbsModule, "iSymmetricThreshold_Harmonic", false,    0,      1,     CFG_TEST_VALUES, &cbsSymmThres,   0,       m_iDebug, iNInputs, &bErrorFlag, &m_vecSymmetricThreshold_Harmonic);
  Config.ReadParameter(SectDataProc, &cbsModule, "iThresholdMultiplier_Harmonic",false,    1,      10,    CFG_TEST_MINMAX, &cbsThresMulti,  3,       m_iDebug, iNInputs, &bErrorFlag, &m_vecThresholdMultiplier_Harmonic);

  Config.ReadParameter(SectDataProc, &cbsModule, "iLogLevel",           false,    0,      9999,  CFG_TEST_MIN,    NULL,            0,       m_iDebug, 1,        &bErrorFlag, &iLogLevel);
  if (iLogLevel > 0)  {
    Config.ReadParameter(SectDataProc, &cbsModule, "sLogPath",          true,     NULL,            CFG_TEST_NONE,   NULL,            NULL,    m_iDebug, 1,      &bErrorFlag, &cbsLogPath);
    Config.ReadParameter(SectDataProc, &cbsModule, "iLogFileGate",      true,     0,      9999,    CFG_TEST_MIN,    &cbsLogFileGate, 5,       m_iDebug, 1,      &bErrorFlag, &m_iLogFileGate);
    Config.ReadParameter(SectDataProc, &cbsModule, "iLogOverwrite",     false,    0,      1,       CFG_TEST_VALUES, NULL,            1,       m_iDebug, 1,      &bErrorFlag, &m_iLogOverwrite);
    Config.ReadParameter(SectDataProc, &cbsModule, "sLogFormat",        false,    &cbsValidFormats,CFG_TEST_STR,    NULL,    &cbsLogFormatDef,m_iDebug, 1,      &bErrorFlag, &cbsLogFormat);
  }
  // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  
  // Check that parameters in configuration file are valid. Parameters for which the ReadParameter()
  // call was inside an if-clause must be given in the comma-separated cbsAppendList.
  CBstr cbsAppendList("sLogPath,iLogFileGate,iLogOverwrite,sLogFormat");
  hr = Config.CheckParameterNames(&cbsModule, &cbsAppendList);
  if (FAILED(hr)) 
  {
    bErrorFlag = true;
  }


  // If the error flag indicates that failure occurred in the parameter calls, return with E_FAIL.
  if (bErrorFlag == true)
  {
    return E_FAIL;
  }

  
  // Writes a log file if iLogLevel>0.
	if (iLogLevel > 0)
	{

    // Initialize the log file.
    CLapLogger::EReturnCode eReturn;
    eReturn = m_Logger.InitializeLogger((char*)cbsLogPath, iLogLevel, (char*)cbsLogFormat);
    if (eReturn == CLapLogger::eOK)
    {
      // OK. Give a warning message that a log file will be created.
      m_spIControl->LapxmSendMessage(MESSAGE_WARNING, CBstr("Writing a log file to ") + cbsLogPath); 
    }
    else
    {
      // Error. Show the last error from the m_Logger object and return with failure.
      m_spIControl->LapxmSendMessage(MESSAGE_ERROR, CBstr(m_Logger.GetLastError().c_str()) );
      return E_FAIL;
    }
    
    m_cbsCurrentLogFile = "NULL";
    
  }
  
  return S_OK;
}





////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::WorkerMain()
//
// TEMPLATE DESCRIPTION: This method creates an endless loop to process data. 
// On each loop, the StopThread event is checked to see if the Main thread is 
// requesting this thread to exit. When this is set, the loop will exit and 
// the thread can clean up and exit.
//
// Also on each loop, a call is made to GetFirstLapxmDataFromQue(), which 
// resides in the DataControlAggregate. This method queries the DataControl 
// aggregate to see if there is data waiting in the queue. If so, it receives a 
// LapxmDataStructure which is filled by the call. Make sure to free the memory
// used by this structure when you have finished with it. Next, the data is sent 
// to the correct method to process the data based on the input channel. It is 
// in these methods that all custom data processing code should reside. Once 
// the data is processed and placed into an outgoing LapxmDataStructure, a call
// is made to OutputLapxmData(). This call passes the data to the DataControl 
// aggregate which knows which modules to send it on to. If this module 
// requires only the newest LapxmDataStructure in the queue, call 
// GetLastLapxmDataFromQue(). This method will return the newest LapxmDataStructure
// and delete all others.
//
// If the queue is empty or full, this thread will be put to sleep until the
// condition changes. This will happen on the GetNextLapxmDataStructureFromQue() 
// method and OutputLapxmDataStructureFromQue() methods. Thus do not be suprised if 
// the debugger appears to hang on these calls. It is simply waiting for the 
// condition of the queue to change.
//
// State changes are sent via an empty data structure with the ChangeState flag
// set to the requested state. These state changes do not stop the looping,
// but simply give the module the ability to do state change tasks. Such as
// writing unused data out to a file on a stop change state.
//
// CUSTOM DESCRIPTION *** Put your custom description here ***
//
// INPUT:  None
//
// OUTPUT: None
//
// RETURN: HRESULT hr         
//
// WRITTEN BY: Coy Chanders 10-1-2002
//
// LAST MODIFIED BY:
//
////////////////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::WorkerMain()
{
  HRESULT hr;
  long lStopThread;
  hr = m_spIControl->GetStopThreadEventHandle(&lStopThread);
  m_hStopThread = (HANDLE)lStopThread;

  // Check to see if the main thread is requesting a shut down.
  while (WaitForSingleObject(m_hStopThread,0) !=  WAIT_OBJECT_0 )
  {
    // Create an instance of the LapxmDataStructure to hold incoming data.
    LapxmDataStructure LapxmDataIn;

    // Create a place to hold the incoming data's input channel.
    int iInputChannel; 

    // Fill LapxmDataIn with data waiting in the queue.
    // Note - This loop is put to sleep on this call if the queue is empty.
    hr = m_spIDataControl->GetFirstLapxmDataFromQue(&LapxmDataIn, &iInputChannel);
    if (SUCCEEDED(hr))
    {	
      // There was data in the queue and now it is in LapxmDataIn.
      
      // Check the input channel.
      if (iInputChannel == 0)
      {
        // Data came in on channel 0 - Used for diagnostics.
        // Channel 0 will simply pass the data on to next module.
        ProcessDataFromInputChannel0(&LapxmDataIn);
        // Always set the current state for this channel.
        m_iChannel0State = LapxmDataIn.lChangeState;
      }
      else if (iInputChannel == 1)
      {
        // Process the data that came in on channel 1.
        ProcessDataFromInputChannel1(&LapxmDataIn);
        // Always set the current state for this channel.
        m_iChannel1State = LapxmDataIn.lChangeState;
      }
      else
      {		
        // Data came in on a channel that this module can not handle.
        // Do nothing with the incoming data and print out a warning.
        char cStr[100];
        sprintf(cStr,"Can not handle input channel %i. Check mapping in configuration file", iInputChannel);
        ATLTRACE("*Lapxm* - ");ATLTRACE(cStr);ATLTRACE("/n");
        m_spIControl->LapxmSendMessage(MESSAGE_ERROR, CBstr(cStr));
      }	

      // Free up allocated memory.
      CoTaskMemFree(LapxmDataIn.pBeam);
      CoTaskMemFree(LapxmDataIn.pData);

      // Report the module's state.
      hr = ReportModuleState(LapxmDataIn.lChangeState);
    }
  }
  return S_OK;
}





////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::UnInitialize()
//
// TEMPLATE DESCRIPTION: If other methods fail or the thread is stopped,
// this method is called to clear out the queue and clean up memory 
// and pointers. Make sure to protect your deletes so that if the 
// pointer is not valid yet, the delete is not called. This is because
// this method can be called early if there is an error 
//
// CUSTOM DESCRIPTION *** Put your custom description here ***
//
// INPUT:  None
//
// OUTPUT: None
//
// RETURN: HRESULT hr         
//
// WRITTEN BY: Coy Chanders 10-1-2002
//
// LAST MODIFIED BY:
//
////////////////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::UnInitialize()
{
  HRESULT hr;

  // Get Last LapxmDataStructure which will empty the queue. 
  // This will free up memory.
  LapxmDataStructure LapxmDataIn;
  int iInputChannel; 
  hr = m_spIDataControl->GetLastLapxmDataFromQue(&LapxmDataIn, &iInputChannel);
  if (SUCCEEDED(hr))
  {
    // Free memory of last LapxmDataStructure.
    CoTaskMemFree(LapxmDataIn.pBeam);
    CoTaskMemFree(LapxmDataIn.pData);
  }


  // *** Put uninitialization code specific to your methods here. ***


  // *** End Custom Code ***

  return S_OK;
}





////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::ReportModuleState(int iRequestedState)
//
// DESCRIPTION: Since there may be more than one channel, it is hard to say
// when the module is really in the requested state. So each channel moves into
// the requested state as it sees a LapxmDataStructure assigned to it's channel
// with the change state flag set to the requested state. Then it calls this 
// method to see if all the other channels are also in this state. If they are,
// then it sets the state for the entire module. If a channel has not seen any
// data because nothing has been mapped to that channel, then it's state 
// remains at -1 and thus it's state is ignored.
//
// CUSTOM DESCRIPTION *** Put your custom description here ***
//
// INPUT: 
// iRequestedState = state that the module should be moving into.
//
// OUTPUT: None 
//
// RETURN: HRESULT hr          
//
// WRITTEN BY: Coy Chanders 10-1-2002
//
// LAST MODIFIED BY:
//
////////////////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::ReportModuleState(int iRequestedState)
{
  HRESULT hr;

  if (  ( (m_iChannel0State == iRequestedState) || (m_iChannel0State == -1) )
  && ( (m_iChannel1State == iRequestedState) || (m_iChannel1State == -1) ) )
  {
    if (m_iCurrentState != iRequestedState)
    {
      hr = m_spIDataControl->SetCurrentState(iRequestedState);
      m_iCurrentState = iRequestedState;
    }
  }

  return S_OK;
}





////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::CopyLapxmDataStructure
//	(LapxmDataStructure *pLapxmDataOut, LapxmDataStructure *pLapxmDataIn)
//
// TEMPLATE DESCRIPTION: Simply copies one LapxmDataStructure into the other.
//
// CUSTOM DESCRIPTION *** Put your custom description here ***
//
// INPUT: 
// LapxmDataStructure *LapxmDataOut - pointer to outgoing LapxmData structure
// LapxmDataStructure *LapxmDataIn  - pointer to incoming LapxmData structure
//
// OUTPUT: 
//
// RETURN: HRESULT hr          
//
// WRITTEN BY: Coy Chanders 10-1-2002
//
// LAST MODIFIED BY:
//
////////////////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::CopyLapxmDataStructure
	(LapxmDataStructure *pLapxmDataOut, LapxmDataStructure *pLapxmDataIn)
{
  // Copy Data from the incoming structure to the outgoing structure.
  *pLapxmDataOut =	*pLapxmDataIn;
  // Note that this creates a copy of the pointers to pBeam & pData
  // but not a copy of the actual data. Thus memory needs to be allocated
  // and the memory copied.

  // Allocate memory.
  pLapxmDataOut->pBeam = (BeamStructure*)CoTaskMemAlloc(sizeof(BeamStructure)*pLapxmDataIn->lNumBeams);
  if (pLapxmDataOut->pBeam == NULL) 
  {
    return E_FAIL;
  }

  pLapxmDataOut->pData = (float*)CoTaskMemAlloc(sizeof(float)*pLapxmDataIn->lNumFloatDataElements);
  if (pLapxmDataOut->pData == NULL)
  {
    CoTaskMemFree(pLapxmDataOut->pBeam);
    return E_FAIL;
  }

  // Copy data pointed to by pBeam & pData.
  memcpy(pLapxmDataOut->pBeam, pLapxmDataIn->pBeam, (sizeof(BeamStructure)*pLapxmDataIn->lNumBeams));
  memcpy(pLapxmDataOut->pData, pLapxmDataIn->pData, (sizeof(float)*pLapxmDataIn->lNumFloatDataElements));

  return S_OK;
}





////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::ProcessDataFromInputChannel0
// (LapxmDataStructure *LapxmDataIn)
//
// TEMPLATE DESCRIPTION: Process data that came in on channel 0.
// This is used for diagnostics and simply copies the data from
// the incoming LapxmData structure to the outgoing and sends it on
// to the next modules. This channel should always be left as it is.
//
// DO NOT MODIFY
//
// INPUT: 
// LapxmDataStructure *LapxmDataIn - pointer to  incoming LapxmData structure
//
// OUTPUT: None
//
// RETURN: HRESULT hr          
//
// WRITTEN BY: Coy Chanders 10-1-2002
//
// LAST MODIFIED BY:
//
////////////////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::ProcessDataFromInputChannel0
																			(LapxmDataStructure *pLapxmDataIn)
{
  HRESULT hr;

  // * DO NOT MODIFY THIS METHOD *
  // This is for diagnostic purposes.
  m_lCount0++;

  char cStr[256];
  sprintf(cStr,"*Lapxm* - Processing %i - Total = %i", pLapxmDataIn->lCount,m_lCount0);
  ATLTRACE(cStr);ATLTRACE("\n");
  m_spIControl->LapxmSendMessage(MESSAGE_MODULE_INFO, CBstr(cStr));
  
  
  if (pLapxmDataIn->lChangeState != DATA_RUNNING)
  {	
    Channel0ChangeState(pLapxmDataIn->lChangeState);
    
    // Pass this LapxmData structure on to the next modules.
    LapxmDataStructure LapxmDataOut = *pLapxmDataIn;
    
    // *** If this module can handle several dwell modes: ***
    // Step thru the list of input data names, and find the name of the current data.
    // Send the data to the corresponding output channel. Note: If the number of
    // output channels in this module can be different from the number of input data, 
    // this procedure needs to be modified.
    CBstr cbsCurrentData = CBstr((char*)pLapxmDataIn->cDataProductName);
    for (unsigned int iOutputChannel = 0; iOutputChannel < m_vecInputNames.size(); ++iOutputChannel)
    {
      if (m_vecInputNames[iOutputChannel].Compare(cbsCurrentData) == 0)
      {        
        strcpy((char*)LapxmDataOut.cDataProductName,(char*)m_vecOutputNames[iOutputChannel]);
        hr = m_spIDataControl->OutputLapxmData(LapxmDataOut, iOutputChannel);
      }
    }
  }
  else
  {    
    // Pass this LapxmData structure on to the next modules.
    LapxmDataStructure LapxmDataOut = *pLapxmDataIn;
    
    // *** If this module can handle several dwell modes: ***
    // Step thru the list of input data names, and find the name of the current data.
    // Send the data to the corresponding output channel. Note: If the number of
    // output channels in this module can be different from the number of input data, 
    // this procedure needs to be modified.
    CBstr cbsCurrentData = CBstr((char*)pLapxmDataIn->cDataProductName);
    for (unsigned int iOutputChannel = 0; iOutputChannel < m_vecInputNames.size(); ++iOutputChannel)
    {
      if (m_vecInputNames[iOutputChannel].Compare(cbsCurrentData) == 0)
      {        
        strcpy((char*)LapxmDataOut.cDataProductName,(char*)m_vecOutputNames[iOutputChannel]);
        hr = m_spIDataControl->OutputLapxmData(LapxmDataOut, iOutputChannel);
      }
    }
  }
  
  return S_OK;
}





////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::Channel0ChangeState(long lChangeState)
//
// TEMPLATE DESCRIPTION: This method deals with the change of state structures
// for the diagnostic channel. Do not change this channel. Instead,
// use channel 1 or higher
//
// INPUT: 
// long lChangeState - this is the state that is being requested.
//
// OUTPUT: 
//
// RETURN: HRESULT hr          
//
// WRITTEN BY: Coy Chanders 10-1-2002
//
// LAST MODIFIED BY:
//
////////////////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::Channel0ChangeState(long lChangeState)
{
  // * DO NOT MODIFY THIS METHOD *
  // This is for diagnostic purposes.

  char cStr[100];

  // Change in state 
  if (lChangeState == DATA_START)
  { 
    // Empty structure with ChangeState flag set to START.
    sprintf(cStr,"*Lapxm* - Channel 0 Start Processed\n");
  }
  else if (lChangeState == DATA_PAUSE)
  { 
    // Empty structure with ChangeState flag set to PAUSE.
    sprintf(cStr,"*Lapxm* - Channel 0 Pause Processed\n");
  }
  else if (lChangeState == DATA_STOP)
  { 
    // Empty structure with ChangeState flag set to STOP.
    sprintf(cStr,"*Lapxm* - Channel 0 Stop Processed\n");
  }
  else
  {
    // Empty structure with an unknown ChangeState flag value.
    sprintf(cStr,"*Lapxm* - Channel 0 Unknown State Change Processed\n");
  }
  ATLTRACE(cStr);
  m_spIControl->LapxmSendMessage(MESSAGE_MODULE_STATECHANGE, CBstr(cStr));

  return S_OK;
}





// BEGINNING CHANNEL1's 2 METHODS

////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::ProcessDataFromInputChannel1
//																		(LapxmDataStructure *LapxmDataIn)
//
// TEMPLATE DESCRIPTION: Proccess data that came in on channel 1.
// Put custom data processing code for channel 1 here.
// Use this method as a template for other channels.
//
// CUSTOM DESCRIPTION *** Put your custom description here ***
//
// INPUT: 
// LapxmDataStructure *LapxmDataIn - pointer to incoming LapxmData structure
//
// OUTPUT: None
//
// RETURN: HRESULT hr - make sure to pass back up error codes from your code.      
//
// WRITTEN BY: Coy Chanders 10-1-2002
//
// LAST MODIFIED BY:
// Raisa Lehtinen 2003-08-29: Added m_pFileMatlab for printing large arrays
// Raisa Lehtinen 2004-03-05: Improved error handling and messaging when NPTS != 4096.
// Raisa Lehtinen 2004-10-29: Accepting also other array sizes than NPTS=4096.
// Raisa Lehtinen 2005-01-20: Implemented adapted NR code for DeSpike and DaubClip.
//                            Using new CLapLogger class.
// Raisa Lehtinen 2006-10-06: Some cleaning, removed obsolete algorithm version.
////////////////////////////////////////////////////////////////////////////////
HRESULT 
CCustomWorkerThread::ProcessDataFromInputChannel1(LapxmDataStructure *pLapxmDataIn)
{
  HRESULT hr;

  // Check the ChangeState flag to see what to do.
  if (pLapxmDataIn->lChangeState != DATA_RUNNING)
  {	
    Channel1ChangeState(pLapxmDataIn->lChangeState);

    // Pass this LapxmData structure on to the next modules.
    LapxmDataStructure LapxmDataOut = *pLapxmDataIn;

    // Step thru the list of input data names, and find the name of the current data.
    // Send the data to the corresponding output channel.     
    CBstr cbsCurrentData = CBstr((char*)pLapxmDataIn->cDataProductName);
    for (unsigned int iOutputChannel = 0; iOutputChannel < m_vecInputNames.size(); ++iOutputChannel)
    {
      if (m_vecInputNames[iOutputChannel].Compare(cbsCurrentData) == 0)
      {        
        strcpy((char*)LapxmDataOut.cDataProductName,(char*)m_vecOutputNames[iOutputChannel]);
        hr = m_spIDataControl->OutputLapxmData(LapxmDataOut, iOutputChannel);
      }
    }
  }
  else
  {
    // Make sure the user mapped the correct data type into this channel.
    if (CBstr((char*)pLapxmDataIn->cDataType).Find("TimeSeries") != 0)
    {
      char cTemp[256];
      sprintf(cTemp, "Data product %s mapped into channel 1 must contain Time Series.",(char*)pLapxmDataIn->cDataProductName);
			m_spIControl->LapxmSendMessage(MESSAGE_ERROR, CBstr(cTemp) + CBstr(" This data product will be ignored."));
			return E_FAIL;
    }

    // Create a LapxmDataStructure to hold the outgoing data.
    LapxmDataStructure LapxmDataOut;
    // Initialize the LapxmDataStructure and copy incoming time-series data.
		hr = m_spIDataControl->InitializeLapxmDataStructure(&LapxmDataOut, 0, 0);
    hr = CopyLapxmDataStructure(&LapxmDataOut, pLapxmDataIn);
    
    
    // Output information to a log file for testing
    if (m_Logger.m_iLogLevel > 0) 
    {
      hr = OpenLogFile(pLapxmDataIn);
      if (FAILED(hr)) return E_FAIL;
    }

    // Select which algorithms to use for this data product.
    CBstr cbsCurrentData = CBstr((char*)pLapxmDataIn->cDataProductName);
    int iDaubClip    = 0;
    int iDeSpike     = 0;
    int iHarmonicSub = 0;
    unsigned int iNum;
    for (iNum = 0; iNum < m_vecInputNames.size(); ++iNum)
    {
      if (m_vecInputNames.at(iNum).Compare(cbsCurrentData) == 0)
      {        
        iDaubClip    = m_vecDaubClip.at(iNum);
        iDeSpike     = m_vecDeSpike.at(iNum);
        iHarmonicSub = m_vecHarmonicSub.at(iNum);
        break;
      }
    }
    
    // Setup timing for testing program speed. 
    clock_t start,stop;
    start = clock();
    
    // Perform the DeSpike Wavelet Algorithm
    bool bSizeUnfit = false;  // check that iNpts * iNspc is correct

    if (iDeSpike == 1)
    {
      NR_DeSpike(&LapxmDataOut, &bSizeUnfit);
      if (bSizeUnfit == true)
      {
        m_vecDeSpike.at(iNum) = 0; // if data record was wrong size, turn algorithm off.
      }
    }

    // Perform the DaubClip Wavelet Algorithm
    if (iDaubClip == 1)
    {
      NR_DaubClip(&LapxmDataOut, &bSizeUnfit);
      if (bSizeUnfit == true)
      {
        m_vecDaubClip.at(iNum) = 0;  // if data record was wrong size, turn algorithm off.
      }
      
    }
    
    // Perform the HarmonicSub Wavelet Algorithm
    if (iHarmonicSub == 1)
    {
      HarmonicSub(&LapxmDataOut, &bSizeUnfit);
      if (bSizeUnfit == true)
      {
        m_vecHarmonicSub.at(iNum) = 0; // if data record was wrong size, turn algorithm off.
      }
    }
    
    // Print timing test results.
    if (m_Logger.m_iLogLevel >= 10) 
    {
      stop = clock();
      m_Logger.Write("ProcessingTimeTicks",stop-start);
      m_Logger.Write("ProcessingTimeMs",double(stop-start)/double(CLOCKS_PER_SEC)*1000.0, LOG_NEWLINE);
    }
    
    // Fill in the DataType name to show that Wavelet algorithms have been applied
    if (m_vecDaubClip.at(iNum) != 0 || m_vecDeSpike.at(iNum) != 0 || m_vecHarmonicSub.at(iNum) != 0)
    {
      strcpy((char*)LapxmDataOut.cDataType, "TimeSeries_Wavelet");
    }
    
        
    // Step thru the list of input data names, and find the name of the current data.
    // Send the data to the corresponding output channel.     
    for (unsigned int iOutputChannel = 0; iOutputChannel < m_vecInputNames.size(); ++iOutputChannel)
    {
      if (m_vecInputNames[iOutputChannel].Compare(cbsCurrentData) == 0)
      {        
        strcpy((char*)LapxmDataOut.cDataProductName,(char*)m_vecOutputNames[iOutputChannel]);
        hr = m_spIDataControl->OutputLapxmData(LapxmDataOut, iOutputChannel);
      }
    }

    // Free up the memory allocated above.
    CoTaskMemFree(LapxmDataOut.pBeam);
    CoTaskMemFree(LapxmDataOut.pData);

    if (m_Logger.m_iLogLevel > 0) 
    {
      m_Logger.CloseFile(); // Log file to be closed at every dwell.
    }
  }

  return S_OK;
}





////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::Channel1ChangeState(long lChangeState)
//
// DESCRIPTION: This method deals with the change of state structures.
//
// CUSTOM DESCRIPTION *** Put your custom description here ***
//
// INPUT: 
// long lChangeState - this is the state that is being requested.
//
// OUTPUT: 
//
// RETURN: HRESULT hr          
//
// WRITTEN BY: Coy Chanders 10-1-2002
//
// LAST MODIFIED BY:
//
////////////////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::Channel1ChangeState(long lChangeState)
{
  char cStr[100];

  // Change in state.
  if (lChangeState == DATA_START)
  { 
    // Empty structure with ChangeState flag set to START.
    sprintf(cStr,"*Lapxm* - Channel 1 Start Processed\n");
    // *** Put Calls to Custom Code Here ***
  }
  else if (lChangeState == DATA_PAUSE)
  { 
    // Empty structure with ChangeState flag set to PAUSE.
    sprintf(cStr,"*Lapxm* - Channel 1 Pause Processed\n");
    // *** Put Calls to Custom Code Here ***
  }
  else if (lChangeState == DATA_STOP)
  { 
    // Empty structure with ChangeState flag set to STOP.
    sprintf(cStr,"*Lapxm* - Channel 1 Stop Processed\n");
    // *** Put Calls to Custom Code Here ***
  }
  else
  {
    // Empty structure with an unknown ChangeState flag value.
    sprintf(cStr,"*Lapxm* - Channel 1 Unknown State Change Processed\n");
    // *** Put Calls to Custom Code Here ***
  }
  ATLTRACE(cStr);
  m_spIControl->LapxmSendMessage(MESSAGE_MODULE_STATECHANGE, CBstr(cStr));

  return S_OK;
}


////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: NR_DeSpike
//
// DESCRIPTION: 
// Perform the DeSpike Wavelet algorithm
// 1) Unlace the I and Q data into two separate buffers. 
//    (i.e. I,Q,I,Q,I,Q,I,Q... TO-> I,I,I,I AND-> Q,Q,Q,Q. 
//    Remove linear trend from the data while doing this.
// 2) Perform a Haar wavelet transform on the I and the Q buffers. 
// 3) Clip all values larger than a set threshold to that threshold value. 
// 4) Reconstruct the time series array.
// 5) Replace the I's & Q's into the outgoing LapxmDataStucture.
//
// INPUT/OUTPUT: LapxmDataStructure *pLapxmData,                          
//
// OUTPUT: bool *pbSizeUnfit - indicates if array size is unfit
//
// RETURN: HRESULT hr          
//
// WRITTEN BY: Raisa Lehtinen 2005-01-11 
//
// LAST MODIFIED BY:
// Raisa Lehtinen 2006-10-06 / Multiple receivers
// Do not clip if unusually many 'spikes' in signal; could be bird clutter.
////////////////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::NR_DeSpike(LapxmDataStructure *pLapxmData,
                                         bool *pbSizeUnfit)
{

  // Get parameters from incoming lapxm data structure.
  int iNRec  = pLapxmData->pBeam[0].Pulse.lNumReceivers;
  int iNGate = pLapxmData->pBeam[0].Pulse.lNumRangeGates;
  int iNpts0 = pLapxmData->pBeam[0].Dwell.lNumPointsInSpectrum * 
    pLapxmData->pBeam[0].Dwell.lNumSpectralInAverage;
  int iNpts  = iNpts0;
  
  // Allowed array size is 1024 or greater
  if (iNpts0 < 1024)
  {
    m_spIControl->LapxmSendMessage(MESSAGE_ERROR, CBstr("DaubClip algorithm requires") +
      CBstr(" iNpts * iNSpec to be equal or greater than 1024.") +
      CBstr(" DaubClip will be ignored for input data ") + CBstr((char*)pLapxmData->cDataProductName) +
      CBstr(" (iNpts * iNSpec = ") + CBstr((double)iNpts0) + CBstr(")."));
    *pbSizeUnfit = true;
    return E_FAIL;
  }
  else
  {
    *pbSizeUnfit = false;
  }
	
	// Calculate Order - iNpts0 must be between 2^(Order-1) and 2^Order.
	int iOrder =(int)ceil(log((double)iNpts0)/log(2.0));  

  // If iNpts is not a power of two, use the nearest larger power-of-two value with zero-padding.
  if (iNpts0 != (int)pow((double)2,(int)iOrder))
  {
    iNpts = (int)pow((double)2,(int)iOrder);
  }

  // Select the threshold ratio requested for this data product.
  CBstr cbsCurrentData = CBstr((char*)pLapxmData->cDataProductName); // still holds the input data name
  double dThresholdRatio_DeSpike = 0.1;
  for (unsigned int iNum = 0; iNum < m_vecInputNames.size(); ++iNum)
  {
    if (m_vecInputNames.at(iNum).Compare(cbsCurrentData) == 0)
    {        
      dThresholdRatio_DeSpike = m_vecThresholdRatio_DeSpike.at(iNum);
    }
  }
  
  if (m_Logger.m_iLogLevel >= 1) 
  {
    m_Logger.Write("\nResults from NR DeSpike algorithm\n");
  }
  
  double *pdDataI = new double[iNpts];
  double *pdDataQ = new double[iNpts];

  // Loop through every receiver.
  for (int iRec = 0; iRec < iNRec; iRec++)
  {
    int iReceiverOffset = iRec*iNGate*iNpts0*2;

    // Loop through every gate, decompose the Wavelet for that gate, clip, and reconstruct.
    for (int iGate = 0; iGate < iNGate; iGate++)
    {

      int iPoint0 = iReceiverOffset + 2*iGate*iNpts0; // First point of this gate.

      // Detrending algorithm.
      // Get signal value in the beginning and end of the array
      int iNMean = 5; // Compute mean over a small _odd_ number of points
      double dMI1 = 0.0; 
      double dMQ1 = 0.0; 
      double dMI2 = 0.0; 
      double dMQ2 = 0.0;
      for (int iPoint = 0; iPoint < iNMean; iPoint++)
      {
        dMI1 = dMI1 + pLapxmData->pData[iPoint0 + (2 * iPoint)];
        dMQ1 = dMQ1 + pLapxmData->pData[iPoint0 + (2 * iPoint) + 1];
        dMI2 = dMI2 + pLapxmData->pData[iPoint0 + (2 * (iNpts0 - 1 - iPoint))];
        dMQ2 = dMQ2 + pLapxmData->pData[iPoint0 + (2 * (iNpts0 - 1 - iPoint)) + 1];
      }
      dMI1 = dMI1 / iNMean;
      dMQ1 = dMQ1 / iNMean;
      dMI2 = dMI2 / iNMean;
      dMQ2 = dMQ2 / iNMean;

      double dHalfNMean = floor((double)iNMean/2.0);
      double dIncrI = (dMI2 - dMI1) / (double)(iNpts0 - 1 - 2.0*dHalfNMean);
      double dIncrQ = (dMQ2 - dMQ1) / (double)(iNpts0 - 1 - 2.0*dHalfNMean);

      // Copy data into temp buffers to unlace the Is & Qs into two seperate blocks
      // I,Q,I,Q,I,Q.... TO-> I,I,I,I.... AND-> Q,Q,Q,Q.....
      // Remove the trend from I and Q separately.
      double dTrendI = dMI1 - dHalfNMean * (dMI2 - dMI1) / (double)(iNpts0 - 1 - 2.0*dHalfNMean);
      double dTrendQ = dMQ1 - dHalfNMean * (dMQ2 - dMQ1) / (double)(iNpts0 - 1 - 2.0*dHalfNMean);
      for (int iPoint = 0; iPoint < iNpts0; iPoint++)
      {
        pdDataI[iPoint] = pLapxmData->pData[iPoint0 + (2 * iPoint)] - dTrendI;
        pdDataQ[iPoint] = pLapxmData->pData[iPoint0 + (2 * iPoint) + 1] - dTrendQ;
        dTrendI = dTrendI + dIncrI;
        dTrendQ = dTrendQ + dIncrQ;
      }

      // Zero-padding: make points after the original array into zeros.
      for (int iPoint = iNpts0; iPoint < iNpts; iPoint++)
      {
        pdDataI[iPoint] = 0.0;
        pdDataQ[iPoint] = 0.0;
      }

      if (m_Logger.m_iLogLevel >= 50 && iGate == m_iLogFileGate) 
      {
        m_Logger.Write("Receiver", iRec, LOG_NEWLINE);
        m_Logger.Write("RangeGate", iGate, LOG_NEWLINE);
        m_Logger.Write("OrigI", iNpts, pdDataI, LOG_NEWLINE);
        m_Logger.Write("OrigQ", iNpts, pdDataQ, LOG_NEWLINE);
      }

      // Wavelet decomposition using Haar.
      HaarWaveletTransform(pdDataI, iNpts, 1, iGate);
      HaarWaveletTransform(pdDataQ, iNpts, 1, iGate);

      if (m_Logger.m_iLogLevel >= 50 && iGate == m_iLogFileGate) 
      {
        m_Logger.Write("RangeGate", iGate, LOG_NEWLINE);
        m_Logger.Write("WaveI", iNpts, pdDataI, LOG_NEWLINE);
        m_Logger.Write("WaveQ", iNpts, pdDataQ, LOG_NEWLINE);
      }

      // Determine Threshold Value
      double dThreshold = 0.0;
      int iNumPoints = 64;                   // Number of Is or Qs in a segment
      int iNSegments = iNpts/(2*iNumPoints); // Number of segments
      int iStartPoint; // Where in the array of lNpts points the segment begins

      // Check 128 point groups from range (lNpts/2 - lNpts-1) * 64 Is & 64 Qs
      for (int iSegment = 0; iSegment < iNSegments; iSegment++)
      {

        // Set the point in the array where this segment begins
        iStartPoint = iNpts/2 + iSegment*64;

        // Create buffer to hold data for sorting. This includes both Is & Qs
        double *pdBuffer = new double[2*iNumPoints];

        // Move I + Q data into one buffer and change to absolute values
        for (int iPoint = 0; iPoint < iNumPoints; iPoint++)
        {
          pdBuffer[iPoint] = fabs(pdDataI[iStartPoint + iPoint]);
          pdBuffer[iNumPoints+iPoint] = fabs(pdDataQ[iStartPoint + iPoint]);
        }

        // Sort all 64 Is & 64 Qs together from smallest to largest
        qsort((void *)pdBuffer,(size_t)(2*iNumPoints),(size_t)sizeof(double),compare_double);

        // Find value of the median point
        double dMedian = pdBuffer[iNumPoints];

        if (m_Logger.m_iLogLevel >= 50) 
        {
          m_Logger.Write("RangeGate", iGate);
          m_Logger.Write("Segment", iSegment);
          m_Logger.Write("StartPoint", iStartPoint);
          m_Logger.Write("Median", dMedian);
          m_Logger.Write("LargestNumber", pdBuffer[2*iNumPoints-1]);
        }

        // Starting with the largest numbers, find where the median point/ largest point < .1
        // The value at this point is considered the threshold
        for (int iPoint = (2*iNumPoints-1); iPoint >= 0 ; iPoint--)
        {
          if (dMedian/pdBuffer[iPoint] > dThresholdRatio_DeSpike)
          {
            // Set fThreshold to threshold found
            dThreshold = 2.0 * pdBuffer[iPoint];

            if (m_Logger.m_iLogLevel >= 50) 
            {
              m_Logger.Write("Threshold", dThreshold);
              m_Logger.Write("ThresholdPoint", iPoint);
            }

            // Remove Spike - This steps through points in this segment looking for values 
            // above the threshold. When found, sets that point to zero and replaces 
            // the corresponding point on the left side (point-NPTS/2) with the 
            // previous value (point-NPTS/2-1).

            // Count first how many values are above threshold. If this number is
            // unusually large, skip the thresholding. Signal may contain components
            // that are not well handled with DeSpike.
            int iIClippedValues = 0;
            int iQClippedValues = 0;
            int iMaxValuesToClip = 5;
            for (int iPointArray = iStartPoint; iPointArray < (iStartPoint+iNumPoints); iPointArray++)
            {
              if ( pdDataI[iPointArray] > dThreshold
                || pdDataI[iPointArray] < (-1.0*dThreshold) )
              {
                iIClippedValues++;
              }
              if ( pdDataQ[iPointArray] > dThreshold
                || pdDataQ[iPointArray] < (-1.0*dThreshold) )
              {
                iQClippedValues++;
              }
            }
            if (iIClippedValues > iMaxValuesToClip)
            {
              iIClippedValues = -1;
            }
            if (iQClippedValues > iMaxValuesToClip)
            {
              iQClippedValues = -1;
            }

            for (int iPointArray = iStartPoint; iPointArray < (iStartPoint+iNumPoints); iPointArray++)
            {
              // Clip the Is
              if ( (pdDataI[iPointArray] > dThreshold
                || pdDataI[iPointArray] < (-1.0*dThreshold))
                && iIClippedValues > 0 )
              {
                if (m_Logger.m_iLogLevel >= 20) 
                {
                  m_Logger.Write("RangeGate", iGate);
                  m_Logger.Write("ChangedIPoint", iPointArray);
                  m_Logger.Write("Threshold", dThreshold);
                  m_Logger.Write("OldValue", pdDataI[iPointArray], LOG_NEWLINE);
                }
                pdDataI[iPointArray] = 0.0;
                if (iPointArray > iNpts/2)
                {
                  pdDataI[iPointArray - iNpts/2] = pdDataI[iPointArray - iNpts/2 - 1];
                }
                else
                {
                  pdDataI[iPointArray - iNpts/2] = pdDataI[iPointArray - iNpts/2 + 1];
                }
              }

              // Clip the Qs
              if ( (pdDataQ[iPointArray] > dThreshold
                || pdDataQ[iPointArray] < (-1.0*dThreshold))
                && iQClippedValues > 0 )
              {
                if (m_Logger.m_iLogLevel >= 20) 
                {
                  m_Logger.Write("RangeGate", iGate);
                  m_Logger.Write("ChangedQPoint", iPointArray);
                  m_Logger.Write("Threshold", dThreshold);
                  m_Logger.Write("OldValue", pdDataQ[iPointArray], LOG_NEWLINE);
                }
                pdDataQ[iPointArray] = 0.0;
                if (iPointArray > iNpts/2)
                {
                  pdDataQ[iPointArray - iNpts/2] = pdDataQ[iPointArray - iNpts/2 - 1];
                }
                else
                {
                  pdDataQ[iPointArray - iNpts/2] = pdDataQ[iPointArray - iNpts/2 + 1];
                }
              }
            }

            break;

          }
        }
        delete [] pdBuffer;
        if (m_Logger.m_iLogLevel >= 50) 
        {
          m_Logger.Write("\n");
        }
      } // Next Segment


      if (m_Logger.m_iLogLevel >= 50 && iGate == m_iLogFileGate) 
      {
        m_Logger.Write("RangeGate", iGate, LOG_NEWLINE);
        m_Logger.Write("ClippedWaveI", iNpts, pdDataI, LOG_NEWLINE);
        m_Logger.Write("ClippedWaveQ", iNpts, pdDataQ, LOG_NEWLINE);
      }

      // Inverse Wavelet transform.
      HaarWaveletTransform(pdDataI, iNpts, -1, iGate);
      HaarWaveletTransform(pdDataQ, iNpts, -1, iGate);

      if (m_Logger.m_iLogLevel >= 50 && iGate == m_iLogFileGate) 
      {
        m_Logger.Write("RangeGate", iGate, LOG_NEWLINE);
        m_Logger.Write("FinalI", iNpts, pdDataI, LOG_NEWLINE);
        m_Logger.Write("FinalQ", iNpts, pdDataQ, LOG_NEWLINE);
      }

      // Copy data out of the temp buffer and relace them into the lapxm data structure
      // I,Q,I,Q,I,Q......
      for (int iPoint = 0; iPoint < iNpts0; iPoint++)
      {
        pLapxmData->pData[iPoint0 + (2 * iPoint)] = (float)pdDataI[iPoint];
        pLapxmData->pData[iPoint0 + (2 * iPoint) + 1] = (float)pdDataQ[iPoint];
      }

  }

  }
  delete [] pdDataI;
  delete [] pdDataQ;


  return S_OK;
}

////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: NR_DaubClip
//
// DESCRIPTION: 
// Perform the Daubechies Clip Wavelet algorithm
// 1) Unlace the I and Q data into two separate buffers. 
//    (i.e. I,Q,I,Q,I,Q,I,Q... TO-> I,I,I,I AND-> Q,Q,Q,Q. 
//    Remove linear trend from the data while doing this.
// 2) Perform a daub20 wavelet transform on the I and the Q buffers. 
// 3) Clip all values larger than a set threshold to that threshold value. 
// 4) Reconstruct the time series array.
// 5) Replace the I's & Q's into the outgoing LapxmDataStucture.
//
// INPUT/OUTPUT: LapxmDataStructure *pLapxmData,                          
//
// OUTPUT: bool *pbSizeUnfit - indicates if array size is unfit
//
// RETURN: HRESULT hr          
//
// WRITTEN BY: Raisa Lehtinen 2005-01-11 / based on older DaubClip method
//
// LAST MODIFIED BY:
// Raisa Lehtinen 2005-01-31 / Number of segments variable.
// Raisa Lehtinen 2006-10-06 / Multiple receivers
////////////////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::NR_DaubClip(LapxmDataStructure *pLapxmData,
                                         bool *pbSizeUnfit)
{
  
  // Get parameters from incoming lapxm data structure.
  int iNRec  = pLapxmData->pBeam[0].Pulse.lNumReceivers;
  int iNGate = pLapxmData->pBeam[0].Pulse.lNumRangeGates;
  int iNpts0 = pLapxmData->pBeam[0].Dwell.lNumPointsInSpectrum * 
    pLapxmData->pBeam[0].Dwell.lNumSpectralInAverage;
  int iNpts  = iNpts0;
  
  // Allowed array size is 1024 or greater
  if (iNpts0 < 1024)
  {
    m_spIControl->LapxmSendMessage(MESSAGE_ERROR, CBstr("DaubClip algorithm requires") +
      CBstr(" iNpts * iNSpec to be equal or greater than 1024.") +
      CBstr(" DaubClip will be ignored for input data ") + CBstr((char*)pLapxmData->cDataProductName) +
      CBstr(" (iNpts * iNSpec = ") + CBstr((double)iNpts0) + CBstr(")."));
    *pbSizeUnfit = true;
    return E_FAIL;
  }
  else
  {
    *pbSizeUnfit = false;
  }
	
	// Calculate Order - iNpts0 must be between 2^(Order-1) and 2^Order.
	int iOrder =(int)ceil(log((double)iNpts0)/log(2.0));  

  // If iNpts is not a power of two, use the nearest larger power-of-two value with zero-padding.
  if (iNpts0 != (int)pow((double)2,iOrder))
  {
    iNpts = (int)pow((double)2,iOrder);
  }

  // Select the parameters requested for this data product.
  CBstr cbsCurrentData = CBstr((char*)pLapxmData->cDataProductName); // still holds the input data name
  double dThresholdRatio_DaubClip = 0.1;
  int iNumberOfSegments = 4;
  for (unsigned int iNum = 0; iNum < m_vecInputNames.size(); ++iNum)
  {
    if (m_vecInputNames.at(iNum).Compare(cbsCurrentData) == 0)
    {        
      iNumberOfSegments        = m_vecNumberOfSegments_DaubClip.at(iNum);
      dThresholdRatio_DaubClip = m_vecThresholdRatio_DaubClip.at(iNum);
    }
  }

	double *pdDataI = new double[iNpts];
	double *pdDataQ = new double[iNpts];

 
  if (m_Logger.m_iLogLevel >= 1) 
  {
    m_Logger.Write("\nResults from NR DaubClip algorithm\n");
  }

  // Loop through every receiver.
  for (int iRec = 0; iRec < iNRec; iRec++)
  {
    int iReceiverOffset = iRec*iNGate*iNpts0*2;

    // Loop through every gate, decompose the Wavelet for that gate, clip, and reconstruct.
    for (int iGate = 0; iGate < iNGate; iGate++)
    {

      int iPoint0 = iReceiverOffset + 2*iGate*iNpts0; // First point of this gate.

      // Detrending algorithm.
      // Get signal value in the beginning and end of the array
      int iNMean = 5; // Compute mean over a small _odd_ number of points
      double dMI1 = 0.0; 
      double dMQ1 = 0.0; 
      double dMI2 = 0.0; 
      double dMQ2 = 0.0;
      for (int iPoint = 0; iPoint < iNMean; iPoint++)
      {
        dMI1 = dMI1 + pLapxmData->pData[iPoint0 + (2 * iPoint)];
        dMQ1 = dMQ1 + pLapxmData->pData[iPoint0 + (2 * iPoint) + 1];
        dMI2 = dMI2 + pLapxmData->pData[iPoint0 + (2 * (iNpts0 - 1 - iPoint))];
        dMQ2 = dMQ2 + pLapxmData->pData[iPoint0 + (2 * (iNpts0 - 1 - iPoint)) + 1];
      }
      dMI1 = dMI1 / iNMean;
      dMQ1 = dMQ1 / iNMean;
      dMI2 = dMI2 / iNMean;
      dMQ2 = dMQ2 / iNMean;

      double dHalfNMean = floor((double)iNMean/2.0);
      double dIncrI = (dMI2 - dMI1) / (double)(iNpts0 - 1 - 2.0*dHalfNMean);
      double dIncrQ = (dMQ2 - dMQ1) / (double)(iNpts0 - 1 - 2.0*dHalfNMean);

      // Copy data into temp buffers to unlace the Is & Qs into two seperate blocks
      // I,Q,I,Q,I,Q.... TO-> I,I,I,I.... AND-> Q,Q,Q,Q.....
      // Remove the trend from I and Q separately.
      double dTrendI = dMI1 - dHalfNMean * (dMI2 - dMI1) / (double)(iNpts0 - 1 - 2.0*dHalfNMean);
      double dTrendQ = dMQ1 - dHalfNMean * (dMQ2 - dMQ1) / (double)(iNpts0 - 1 - 2.0*dHalfNMean);
      for (int iPoint = 0; iPoint < iNpts0; iPoint++)
      {
        pdDataI[iPoint] = pLapxmData->pData[iPoint0 + (2 * iPoint)] - dTrendI;
        pdDataQ[iPoint] = pLapxmData->pData[iPoint0 + (2 * iPoint) + 1] - dTrendQ;
        dTrendI = dTrendI + dIncrI;
        dTrendQ = dTrendQ + dIncrQ;
      }

      // Zero-padding: make points after the original array into zeros.
      for (int iPoint = iNpts0; iPoint < iNpts; iPoint++)
      {
        pdDataI[iPoint] = 0.0;
        pdDataQ[iPoint] = 0.0;
      }


      // FOR TESTING. Create and use a dummy 32pt array. This has been stored in .MAT file wavelet_A.
      //double A[32] = {0.4949,    0.0383,    0.2274,    0.3279,    0.8995,    0.3137,    0.2517,
      //  0.4330,    0.8424,    0.1845,    0.5082,    0.4522,    0.3256,    0.3801,
      //  0.8865,    0.7613,    0.8838,    0.4574,    0.7992,    0.1341,    0.0653,
      //  0.3751,    0.3735,    0.4840,    0.9695,    0.3421,    0.2527,    0.5849,
      //  0.5237,    0.1634,    0.4864,    0.4961};
      //double *pA = A;
      //DiscreteWaveletTransform(pA, 32, 1, "daub20", iGate);
      //DiscreteWaveletTransform(pA, 32, -1, "daub20", iGate);
      // END TESTING

      if (m_Logger.m_iLogLevel >= 50 && iGate == m_iLogFileGate) 
      {
        m_Logger.Write("RangeGate", iGate, LOG_NEWLINE);
        m_Logger.Write("OrigI", iNpts, pdDataI, LOG_NEWLINE);
        m_Logger.Write("OrigQ", iNpts, pdDataQ, LOG_NEWLINE);
      }

      // Wavelet decomposition using Daubechies20.
      DiscreteWaveletTransform(pdDataI, iNpts, 1, "daub20", iGate);
      DiscreteWaveletTransform(pdDataQ, iNpts, 1, "daub20", iGate);

      if (m_Logger.m_iLogLevel >= 50 && iGate == m_iLogFileGate) 
      {
        m_Logger.Write("RangeGate", iGate, LOG_NEWLINE);
        m_Logger.Write("WaveI", iNpts, pdDataI, LOG_NEWLINE);
        m_Logger.Write("WaveQ", iNpts, pdDataQ, LOG_NEWLINE);
      }

      // Determine Threshold Value
      double dThreshold = 0.0;
      int iNumPoints, iStartPoint;

      int iFirstSegment = 3;
      if (iNumberOfSegments == 3)
      {
        iFirstSegment = 2;
      }
      // Check up to four ranges 0-(Npts/8-1), Npts/8-(Npts/4-1), Npts/4-(Npts/2-1), Npts/2-(Npts-1)
      // If only 3 segments requested, skip first range 0-(Npts/8-1).
      for (int iSegment = iFirstSegment; iSegment >= 0; iSegment--)
      {
        // Set number of points in this segment

        if (iSegment == 3)
        { 
          iNumPoints = iNpts/8;  
          iStartPoint = 0;
        }
        else
        {
          iNumPoints = (int)(pow((double)2,(iOrder-iSegment-1)));
          iStartPoint = (int)(pow((double)2,(iOrder-iSegment-1)));
        }

        // Create buffer to hold data for sorting
        double *pdBuffer = new double[2*iNumPoints];

        // Move I + Q data into one buffer and change to absolute values
        for (int iPoint = 0; iPoint < iNumPoints; iPoint++)
        {
          pdBuffer[iPoint] = fabs(pdDataI[iStartPoint + iPoint]);
          pdBuffer[iNumPoints+iPoint] = fabs(pdDataQ[iStartPoint + iPoint]);
        }

        // Sort from smallest to largest
        qsort((void *)pdBuffer, (size_t)(2*iNumPoints), (size_t)sizeof(double), compare_double);

        if (m_Logger.m_iLogLevel >= 50 && iGate == m_iLogFileGate) 
        {
          m_Logger.Write("RangeGate", iGate);
          m_Logger.Write("Segment", iSegment);
          m_Logger.Write("NumPoints", iNumPoints);
          m_Logger.Write("StartPoint", iStartPoint, LOG_NEWLINE);
          m_Logger.Write("SortedBuffer", 2*iNumPoints, pdBuffer, LOG_NEWLINE);
        }

        // Find value of the median point
        double dMedian = pdBuffer[iNumPoints];

        // Set through the points until the median point/ largest point < .1
        for (int iPoint = 1; iPoint <= 2*iNumPoints; iPoint++)
        {
          if (dMedian/pdBuffer[(2*iNumPoints-iPoint)] > dThresholdRatio_DaubClip)
          {

            // Set the threshold to this largest point if larger than previous threshold
            if (pdBuffer[(2*iNumPoints-iPoint)] > dThreshold)
            {
              dThreshold = pdBuffer[(2*iNumPoints-iPoint)];
            }

            break;
          }
        }
        delete [] pdBuffer;
      } // Next Segment


      // Output threshold information to a test file for testing
      if (m_Logger.m_iLogLevel >= 20) 
      {
        m_Logger.Write("RangeGate", iGate);
        m_Logger.Write("FinalThreshold", dThreshold);
      }

      // Clip - This clips all values to a maximum of a set threshold.
      // The threshold is set in the config file.
      int iCount = 0;
      for (int iPoint = 0; iPoint < iNpts; iPoint++)
      {
        // Clip the Is
        if (fabs(pdDataI[iPoint]) > dThreshold)
        {
          if (m_Logger.m_iLogLevel >= 50) 
          {
            m_Logger.Write("I", pdDataI[iPoint]);
          }
          pdDataI[iPoint] = pdDataI[iPoint]/fabs(pdDataI[iPoint]) * dThreshold;
          iCount++;
        }

        // Clip the Qs
        if (fabs(pdDataQ[iPoint]) > dThreshold)
        {
          if (m_Logger.m_iLogLevel >= 50) 
          {
            m_Logger.Write("Q", pdDataQ[iPoint]);
          }
          pdDataQ[iPoint] = pdDataQ[iPoint]/fabs(pdDataQ[iPoint]) * dThreshold;
          iCount++;
        }
      }

      if (m_Logger.m_iLogLevel >= 20) 
      {
        m_Logger.Write("PointsClippedTotal", iCount, LOG_NEWLINE);
      }
      if (m_Logger.m_iLogLevel >= 50 && iGate == m_iLogFileGate) 
      {
        m_Logger.Write("RangeGate", iGate, LOG_NEWLINE);
        m_Logger.Write("ClippedWaveI", iNpts, pdDataI, LOG_NEWLINE);
        m_Logger.Write("ClippedWaveQ", iNpts, pdDataQ, LOG_NEWLINE);
      }

      // Inverse Wavelet transform.
      DiscreteWaveletTransform(pdDataI, iNpts, -1, "daub20", iGate);
      DiscreteWaveletTransform(pdDataQ, iNpts, -1, "daub20", iGate);

      if (m_Logger.m_iLogLevel >= 50 && iGate == m_iLogFileGate) 
      {
        m_Logger.Write("\nClippedI", iNpts, pdDataI, LOG_NEWLINE);
        m_Logger.Write("ClippedQ", iNpts, pdDataQ, LOG_NEWLINE);
      }

      // Copy data out of the temp buffer and relace them into the lapxm data structure
      // I,Q,I,Q,I,Q......
      for (int iPoint = 0; iPoint < iNpts0; iPoint++)
      {
        pLapxmData->pData[iPoint0 + (2 * iPoint)] = (float)pdDataI[iPoint];
        pLapxmData->pData[iPoint0 + (2 * iPoint) + 1] = (float)pdDataQ[iPoint];
      }

    }

  }
  delete [] pdDataI;
  delete [] pdDataQ;

  return S_OK;
}


////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: HaarWaveletTransform
//
// DESCRIPTION: Performs **only one level** of the the discrete Haar wavelet 
// transform or inverse Haar wavelet transform.
// The method DiscreteWaveletTransform() was originally used, but this optimized
// method offers a small speed advantage.
//
// INPUT/OUTPUT: double *pdData - data array to be transformed
//
// INPUT: 
// int iLength   - length of *pdData
// int iMethod   - transform methods: 1: forward transform, only largest hierarchy level
//                 -1: inverse transform, only largest hierarchy level
// int iGate     - current range gate, for log file printing (not used)
//
// RETURN: HRESULT hr          
//
// WRITTEN BY: Raisa Lehtinen 2005-01-27
//
// LAST MODIFIED BY:
//
////////////////////////////////////////////////////////////////////////////////
HRESULT 
CCustomWorkerThread::HaarWaveletTransform(double *pdData, // data array I/O
                                          int iLength,    // length of data
                                          int iMethod,    // 1,2 = transform -1,-2 = inverse tr.
                                          int iGate)      // current range gate
{
  
  
  int iNH = iLength/2;

  std::vector<double> vecWksp(iLength, 0.0);

  if (iMethod >= 0) // wavelet transform
  {
    for (int i = 0; i < iNH-1; i++)
    {
      vecWksp[i] = (pdData[2*i+1] + pdData[2*i+2]) * 0.70710678118655;
      vecWksp[i+iNH] = (pdData[2*i+1] - pdData[2*i+2]) * 0.70710678118655;
    } 
    vecWksp[iNH-1] = (pdData[iLength-1] + pdData[0]) * 0.70710678118655;
    vecWksp[iLength-1] = (pdData[iLength-1] - pdData[0]) * 0.70710678118655;
  }
  else  // inverse transform
  {
    for (int i = 0, j = 0; i < iNH-1; i++, j += 2)
    {
      vecWksp[j+1] = (pdData[i] + pdData[i+iNH]) * 0.70710678118655;
      vecWksp[j+2] = (pdData[i] - pdData[i+iNH]) * 0.70710678118655;
    } 
    vecWksp[0] = (pdData[iNH-1] - pdData[iLength-1]) * 0.70710678118655;
    vecWksp[iLength-1] = (pdData[iNH-1] + pdData[iLength-1]) * 0.70710678118655;
  }
  
  for (int i = 0; i < iLength; i++)
  {
    pdData[i] = vecWksp[i];
  }
  
  return S_OK;
}





////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: DiscreteWaveletTransform
//
// DESCRIPTION: Performs the discrete wavelet transform or inverse wavelet
// transform.
//
// INPUT/OUTPUT: double *pdData - data array to be transformed
//
// INPUT: 
// int iLength   - length of *pdData
// int iMethod   - transform methods: 1: full transform -1: full inverse transform
//                 2: transform, only largest hierarchy level 
//                 -2: inverse transform, only largest hierarchy level
// char *cFilter - filter name "haar" or "daub20"
// int iGate     - current range gate, for log file printing
//
// RETURN: HRESULT hr          
//
// WRITTEN BY: Raisa Lehtinen 2005-01-11
//
// LAST MODIFIED BY:
//
////////////////////////////////////////////////////////////////////////////////
HRESULT 
CCustomWorkerThread::DiscreteWaveletTransform(double *pdData, // data array I/O
                                              int iLength,    // length of data
                                              int iMethod,    // 1,2 = transform -1,-2 = inverse tr.
                                              char *cFilter,  // name of wavelet filter
                                              int iGate)      // current range gate
{
  
  int iSmallestLevel = 4;
  if (iMethod == -2 || iMethod == 2)
  {
    iSmallestLevel = iLength;
  }

  int iNCoeff;
  std::vector<double> vecCC; // coefficients of the smoothing filter
  std::vector<double> vecCR; // coefficients of the wavelet function
  if (strcmp(cFilter, "haar") == 0) // Haar wavelet = Daubechies 2
  {
    iNCoeff = 2;
    vecCC.resize(iNCoeff);
    vecCC[0] = 0.70710678118655;
    vecCC[1] = 0.70710678118655;
    vecCR.resize(iNCoeff);
    vecCR[0] = 0.70710678118655;
    vecCR[1] = -0.70710678118655;
  }
  else if (strcmp(cFilter, "daub20") == 0) // Daubechies filter with 20 coefficients.
  {
    iNCoeff = 20;
    vecCC.resize(iNCoeff);
    vecCC[0]  =  0.026670057901; vecCC[1]  =  0.188176800078; vecCC[2]  =  0.527201188932;
    vecCC[3]  =  0.688459039454; vecCC[4]  =  0.281172343661; vecCC[5]  = -0.249846424327;
    vecCC[6]  = -0.195946274377; vecCC[7]  =  0.127369340336; vecCC[8]  =  0.093057364604;
    vecCC[9]  = -0.071394147166; vecCC[10] = -0.029457536822; vecCC[11] =  0.033212674059;
    vecCC[12] =  0.003606553567; vecCC[13] = -0.010733175483; vecCC[14] =  0.001395351747;
    vecCC[15] =  0.001992405295; vecCC[16] = -0.000685856695; vecCC[17] = -0.000116466855;
    vecCC[18] =  0.000093588670; vecCC[19] = -0.000013264203;
    double dMult = -1.0;
    vecCR.resize(iNCoeff);
    for (int i = 0; i < iNCoeff; i++)
    {
      vecCR[iNCoeff-1-i] = dMult * vecCC[i];
      dMult = -dMult;
    }
  }
  else
  {
    return E_FAIL; // filter name invalid.
  }
  
  int iIoff = -(iNCoeff >> 1); // Handle wrap-around of wavelets. iIoff and iJoff are
  int iJoff = -(iNCoeff >> 1); // here identical to center the 'support' of wavelets.
  
  if (m_Logger.m_iLogLevel >= 60 && iGate == m_iLogFileGate) 
  {
    m_Logger.Write("DWTinput_60", iLength, pdData, LOG_NEWLINE);
  }

  std::vector<double> vecWksp;
  int iNMod, iNN1, iNH;
  int iNI, iNJ, iJF, iJR;
  double dAi, dAi1;
  
  if (iMethod >= 0) // wavelet transform
  {
    for (int iNN = iLength; iNN >= iSmallestLevel; iNN >>= 1)
    {
      vecWksp.assign(iNN, 0.0);
      iNMod = iNCoeff * iNN;
      iNN1 = iNN - 1;
      iNH = iNN >> 1;
      for (int ii = 0, i = 0; i < iNN; i += 2, ii++)
      {
        iNI = i+1+iNMod+iIoff;
        iNJ = i+1+iNMod+iJoff; 
        for (int k = 0; k < iNCoeff; k++)
        {
          iJF = iNN1 & (iNI+k+1);
          iJR = iNN1 & (iNJ+k+1);
          vecWksp[ii] += vecCC[k] * pdData[iJF];
          vecWksp[ii+iNH] += vecCR[k] * pdData[iJR];
        }
      }
      for (int i = 0; i < iNN; i++)
      {
        pdData[i] = vecWksp[i];
      }
      if (m_Logger.m_iLogLevel >= 60 && iGate == m_iLogFileGate) 
      {
        m_Logger.Write("DWTlevel_60", vecWksp, LOG_NEWLINE);
      }
    }
  }
  else // inverse wavelet transform
  {
    for (int iNN = iSmallestLevel; iNN <= iLength; iNN <<= 1)
    {
      vecWksp.assign(iNN, 0.0);
      iNMod = iNCoeff * iNN;
      iNN1 = iNN - 1;
      iNH = iNN >> 1;
      for (int ii = 0, i = 0; i < iNN; i += 2, ii++)
      {
        dAi = pdData[ii];
        dAi1 = pdData[ii+iNH];
        iNI = i+1+iNMod+iIoff;
        iNJ = i+1+iNMod+iJoff; 
        for (int k = 0; k < iNCoeff; k++)
        {
          iJF = iNN1 & (iNI+k+1);
          iJR = iNN1 & (iNJ+k+1);
          vecWksp[iJF] += vecCC[k] * dAi;
          vecWksp[iJR] += vecCR[k] * dAi1;
        }
      }
      for (int i = 0; i < iNN; i++)
      {
        pdData[i] = vecWksp[i];
      }
      if (m_Logger.m_iLogLevel >= 60 && iGate == m_iLogFileGate) 
      {
        m_Logger.Write("IDWTlevel_60", vecWksp, LOG_NEWLINE);
      } 
    }
  }
  
  if (m_Logger.m_iLogLevel >= 60 && iGate == m_iLogFileGate) 
  {
    m_Logger.Write("DWTresult_60", iLength, pdData, LOG_NEWLINE);
  }
  
  return S_OK;
}



////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::HarmonicSub(LapxmDataStructure *pLapxmDataOut)
//
// DESCRIPTION: Performs the Harmonic Wavelet algorithm.
//
// INPUT: 
// LapxmDataStructure *LapxmDataIn - pointer to incoming LapxmData structure
//
// OUTPUT: 
//
// RETURN: HRESULT hr          
//
// WRITTEN BY: Coy Chanders 7-1-03
//
// LAST MODIFIED BY:
// Raisa Lehtinen 2006-10-06 / Multiple receivers
////////////////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::HarmonicSub(LapxmDataStructure *pLapxmDataOut,
                                         bool *pbSizeUnfit)
{

	// Get parameters from incoming lapxm data structure.
  int iNRec   = pLapxmDataOut->pBeam[0].Pulse.lNumReceivers;
	long lNGate = pLapxmDataOut->pBeam[0].Pulse.lNumRangeGates;
	long lNpts  = pLapxmDataOut->pBeam[0].Dwell.lNumPointsInSpectrum * pLapxmDataOut->pBeam[0].Dwell.lNumSpectralInAverage;
	
	if (lNpts != 4096)
	{
    m_spIControl->LapxmSendMessage(MESSAGE_ERROR, CBstr("HarmonicSub algorithm requires iNpts * iNSpec equal to 4096. ") +
      CBstr(" HarmonicSub will be ignored for input data ") + CBstr((char*)pLapxmDataOut->cDataProductName) +
      CBstr(" (iNpts * iNSpec = ") + CBstr((double)lNpts) + CBstr(")."));
    *pbSizeUnfit = true;
    return E_FAIL;
  }
  else
  {
    *pbSizeUnfit = false;
  }
	
	// Calculate Order - lNpts must equal 2^Order.
	int iOrder =(int)ceil(log((double)lNpts)/log(2.0));
  
  
  // Select the parameters requested for this data product.
  CBstr cbsCurrentData = CBstr((char*)pLapxmDataOut->cDataProductName); // still holds the input data name
  int iMinThresholdLevel_Harmonic   = 6;
  int iSymmetricThreshold_Harmonic  = 0;
  int iThresholdMultiplier_Harmonic = 3;
  for (unsigned int iNum = 0; iNum < m_vecInputNames.size(); ++iNum)
  {
    if (m_vecInputNames.at(iNum).Compare(cbsCurrentData) == 0)
    {        
      iMinThresholdLevel_Harmonic   = m_vecMinThresholdLevel_Harmonic.at(iNum);
      iSymmetricThreshold_Harmonic  = m_vecSymmetricThreshold_Harmonic.at(iNum);
      iThresholdMultiplier_Harmonic = m_vecThresholdMultiplier_Harmonic.at(iNum);
    }
  }

  if (m_Logger.m_iLogLevel >= 1) 
  {
    m_Logger.Write("\nResults from HarmonicSub algorithm\n");
  }

  float *pfWav;

  // Loop through every receiver.
  for (int iRec = 0; iRec < iNRec; iRec++)
  {
    int iReceiverOffset = iRec*lNGate*lNpts*2;

    // Loop through every gate, decompose the Wavelet for that gate, clip, and reconstruct.
    for (int lGate = 0; lGate < lNGate; lGate++)
    {

      // Set pointer to the correct part of the data
      pfWav = &pLapxmDataOut->pData[iReceiverOffset + (2*lGate*lNpts)];

      if (m_Logger.m_iLogLevel >= 40 && lGate == m_iLogFileGate) 
      {
        m_Logger.Write("RangeGate", lGate, LOG_NEWLINE);
        m_Logger.Write("OrigData", 2*lNpts, pfWav, LOG_NEWLINE);
      }

      // ******* Decompose Harmonic Wavelet Transform
      HarmonicWaveletTransform(pfWav, iOrder, lGate);

      if (m_Logger.m_iLogLevel >= 40 && lGate == m_iLogFileGate) 
      {
        m_Logger.Write("\nWave", 2*lNpts, pfWav, LOG_NEWLINE);
      }

      double dThresholdLeftSide;
      double dThresholdRightSide;
      double dRandomNumber;
      double dFinalThresholdLeftSide;
      double dFinalThresholdRightSide;
      int    iPointStartLeft; 
      int    iPointStartRight;
      int    iNumSegments;
      int    iPointStart;
      int    iPointEnd;
      int    iRightOffSet;
      int    iFinalThresholdLeftLevel  = (iOrder-2);
      int    iFinalThresholdRightLevel = (iOrder-2);

      // ***** Find threshold values 

      // Loop through levels (iOrder-2) to 1
      for (int iLevel = (iOrder-2); iLevel >= 1; iLevel--)
      {

        if (iLevel >= iMinThresholdLevel_Harmonic)
        {

          // Break this level into segments of 16 points
          iNumSegments = (int)pow((double)2,(iLevel-4));

          for (int iSegment = 0; iSegment < iNumSegments; iSegment++)
          {					
            iPointStartLeft  = (int)pow((double)2,(iLevel+1)) + (iSegment * 32);
            iPointStartRight = (int)(pow((double)2,(iOrder+1)) - pow((double)2,(iLevel+2)) + 2) + (iSegment * 32);

            // **** Calculate the standard deviation
            float dStdDevLeft  = nspsStdDev((const float*)&pfWav[iPointStartLeft], 32);
            float dStdDevRight = nspsStdDev((const float*)&pfWav[iPointStartRight], 32);

            // If using symmetric thresholds, calculate the mean.
            if (iSymmetricThreshold_Harmonic == 1)
            {
              dStdDevLeft = (dStdDevLeft + dStdDevRight) / (float)2.0;
              dStdDevRight = dStdDevLeft;
            }

            // Find the smallest standard deviation among the segments of this level.
            if (iSegment == 0)
            {
              dThresholdLeftSide  = dStdDevLeft;  // starting value for the left side
              dThresholdRightSide = dStdDevRight; // starting value for the right side
            }
            else
            {
              // Left
              if (dStdDevLeft < dThresholdLeftSide)
              {
                dThresholdLeftSide = dStdDevLeft;
              }

              // Right
              if (dStdDevRight < dThresholdRightSide)
              {
                dThresholdRightSide = dStdDevRight;
              }
            }

            if (m_Logger.m_iLogLevel >= 40 && lGate == m_iLogFileGate) 
            {
              m_Logger.Write("Level", iLevel);
              m_Logger.Write("Segment", iSegment);
              m_Logger.Write("iPointStartLeft", iPointStartLeft);
              m_Logger.Write("iPointStartRight", iPointStartRight);
              m_Logger.Write("STDLeft", dStdDevLeft);
              m_Logger.Write("STDRight", dStdDevRight, LOG_NEWLINE);
            }

          }  // for iSegment

          if (iLevel == (iOrder-2))
          {
            // Initialize final threshold values.
            dFinalThresholdLeftSide   = dThresholdLeftSide;
            dFinalThresholdRightSide  = dThresholdRightSide;
            iFinalThresholdLeftLevel  = iLevel;
            iFinalThresholdRightLevel = iLevel;

            if (m_Logger.m_iLogLevel >= 40 && lGate == m_iLogFileGate) 
            {
              m_Logger.Write("Level", iLevel);
              m_Logger.Write("ThresholdLeft", dThresholdLeftSide);
              m_Logger.Write("ThresholdRight", dThresholdRightSide, LOG_NEWLINE);
            }

          }
          else if (iLevel < iOrder-2)
          {
            // Scale the current selected threshold by powers of 1/sqrt(2) to make
            // the values comparable between different levels. Choose the larger 
            // for the selected threshold.
            double dTemp = dFinalThresholdLeftSide / pow(sqrt(2.0), (iFinalThresholdLeftLevel - iLevel));
            if (dThresholdLeftSide > dTemp)
            {
              dFinalThresholdLeftSide  = dThresholdLeftSide;
              iFinalThresholdLeftLevel = iLevel;
            }
            double dTemp2 = dFinalThresholdRightSide / pow(sqrt(2.0), (iFinalThresholdRightLevel - iLevel));
            if (dThresholdRightSide > dTemp2)
            {
              dFinalThresholdRightSide  = dThresholdRightSide;
              iFinalThresholdRightLevel = iLevel;
            }

            if (m_Logger.m_iLogLevel >= 40 && lGate == m_iLogFileGate) 
            {
              m_Logger.Write("Level", iLevel);
              m_Logger.Write("ThresholdLeft", dThresholdLeftSide);
              m_Logger.Write("ThresholdRight", dThresholdRightSide);
              m_Logger.Write("ComparedFinalThresholdLeft", dTemp);
              m_Logger.Write("ComparedFinalThresholdRight", dTemp2, LOG_NEWLINE);
            }
          }

        } // for iLevels

        // Set coefficients on the Left and Right side to zero for smallest Levels
        // / Raisa Lehtinen 2003-09-10: This was replaced by clipping the levels with threshold.
        // iPointStart  = (int)pow(2,(iLevel+1));
        // iPointEnd    = (int)(pow(2,(iLevel+2)) - 1);
        // iRightOffSet = (int)pow(2,(iOrder+1));
        // for (int iPoint = iPointStart; iPoint <= iPointEnd; iPoint++)
        // {
        // 	pfWav[iPoint] = 0;
        // 	pfWav[iRightOffSet - iPoint + 1] = 0;
        // }

        // Output threshold information to a test file for testing
        /*      if (m_iDebug > 9 && lGate == m_iLogFileGate)
        {
        sprintf(cTemp, "FinalThresholdLeft=%.2f, Level = %d, FinalThresholdRight=%.2f, Level = %d\n",
        dFinalThresholdLeftSide,iFinalThresholdLeftLevel,dFinalThresholdRightSide,iFinalThresholdRightLevel);
        fwrite(cTemp,sizeof(char), strlen(cTemp), m_pFile);
        } */

      } // for iLevel

      // Increase the final threshold values by a multiplier for not clipping too much.
      dFinalThresholdLeftSide  *= (double) iThresholdMultiplier_Harmonic;
      dFinalThresholdRightSide *= (double) iThresholdMultiplier_Harmonic;

      if (m_Logger.m_iLogLevel >= 40 && lGate == m_iLogFileGate) 
      {
        m_Logger.Write("FINALThresholdLeft", dFinalThresholdLeftSide);
        m_Logger.Write("Level", iFinalThresholdLeftLevel);
        m_Logger.Write("FINALThresholdRight", dFinalThresholdRightSide);
        m_Logger.Write("Level", iFinalThresholdRightLevel, LOG_NEWLINE);
      }


      // ***** Apply thresholds to each level

      double dAppliedThresholdLeft;
      double dAppliedThresholdRight;

      // Loop through levels (iOrder-2) to 1
      for (int iLevel = (iOrder-2); iLevel >= 1; iLevel--)
      {

        // Calculate the threshold to be applied for this level.
        dAppliedThresholdLeft  = dFinalThresholdLeftSide * pow(sqrt(2.0),(iLevel - iFinalThresholdLeftLevel));
        dAppliedThresholdRight = dFinalThresholdRightSide * pow(sqrt(2.0),(iLevel - iFinalThresholdRightLevel));

        if (m_Logger.m_iLogLevel >= 40 && lGate == m_iLogFileGate) 
        {
          m_Logger.Write("Applying thresholds to Level", iLevel);
          m_Logger.Write("Applied ThresholdLeft", dAppliedThresholdLeft);
          m_Logger.Write("Applied ThresholdRight", dAppliedThresholdRight, LOG_NEWLINE);
        }

        // If a point is greater than (+ or -) the threshold, cut the value down
        // to the threshold value (sign defined by the sign of the value).
        iPointStart  = (int)pow((double)2,(iLevel+1));
        iPointEnd    = (int)(pow((double)2,(iLevel+2)) - 1);
        iRightOffSet = (int)pow((double)2,(iOrder+1));
        for (int iPoint = iPointStart; iPoint <= iPointEnd; iPoint++)
        {
          // Apply to Left
          if ( pfWav[iPoint] > dAppliedThresholdLeft || pfWav[iPoint] < (-1 * dAppliedThresholdLeft) )
          {
            if (pfWav[iPoint] < 0.0)
            {
              dRandomNumber = -1.0; // 'random number' is currently -1 or 1.
            }
            else
            {
              dRandomNumber = 1.0;
            }

            // Old version (before 2003-12-23) changed the value to a random number between (+ or -) 
            // times the threshold. rand() returns values 0...0x7fff (=32767), and so dRandomNumber = -1...+1.
            // dRandomNumber = rand()/16383.0 - 1; 

            // if (m_iDebug > 9)
            // {
            // 	sprintf(cTemp, "\nILeftChanged=%d OldValue=%.2f NewValue=%.2f\n",iPoint,pfWav[iPoint],float(dRandomNumber * dAppliedThresholdLeft));
            //	fwrite(cTemp,sizeof(char), strlen(cTemp), m_pFile);
            // }

            pfWav[iPoint] = float(dRandomNumber * dAppliedThresholdLeft);

          }

          // Apply to Right
          if ( pfWav[iRightOffSet - iPoint + 1] > dAppliedThresholdRight || pfWav[iRightOffSet - iPoint + 1] < (-1 * dAppliedThresholdRight) )
          {
            if (pfWav[iRightOffSet - iPoint + 1] < 0.0)
            {
              dRandomNumber = -1.0;
            }
            else
            {
              dRandomNumber = 1.0;
            }
            // dRandomNumber = rand()/16383.0 - 1;

            // if (m_iDebug > 9)
            // {
            // 	sprintf(cTemp, "\nIRightChanged=%d OldValue=%.2f NewValue=%.2f\n",(iRightOffSet - iPoint + 1),pfWav[iRightOffSet - iPoint + 1],float(dRandomNumber * dAppliedThresholdRight));
            //	fwrite(cTemp,sizeof(char), strlen(cTemp), m_pFile);
            // }

            pfWav[iRightOffSet - iPoint + 1] = float(dRandomNumber * dAppliedThresholdRight);
          }
        }
      } // End all levels

      // TBD 2: experimental code. Currently testing leaving the short levels as they are / RHL 2003-9
      int iZeroSmallLevels = 0;
      if (iZeroSmallLevels == 1)
      {
        // Zero out point[0] and point[lNpts/2]
        pfWav[0] = 0;
        pfWav[1] = 0;
        pfWav[2] = 0;
        pfWav[3] = 0;
        pfWav[lNpts]   = 0;
        pfWav[lNpts+1] = 0;
        pfWav[2*lNpts - 1] = 0;
        pfWav[2*lNpts - 2] = 0;
      }

      // Calculate random value between threshold and -threshold for point[1] and point[lNpts-1]
      /*
      pfWav[1] = float(dRandomNumber * dThresholdLeftSide);

      dRandomNumber = rand()/16383.0 - 1;
      pfWav[1] = float(dRandomNumber * dThresholdLeftSide);

      dRandomNumber = rand()/16383.0 - 1;
      pfWav[2] = float(dRandomNumber * dThresholdLeftSide);

      dRandomNumber = rand()/16383.0 - 1;
      pfWav[2*lNpts - 1] = float(dRandomNumber * dThresholdRightSide);

      dRandomNumber = rand()/16383.0 - 1;
      pfWav[2*lNpts - 2] = float(dRandomNumber * dThresholdRightSide);
      */

      // ****** END Calculate and apply thresholds

      if (m_Logger.m_iLogLevel >= 40 && lGate == m_iLogFileGate) 
      {
        m_Logger.Write("\nWaveFilt", 2*lNpts, pfWav, LOG_NEWLINE);
      }

      // **** Reconstruct Harmonic Wavelet Transform
      InverseHarmonicWaveletTransform(pfWav, iOrder);

      if (m_Logger.m_iLogLevel >= 40 && lGate == m_iLogFileGate) 
      {
        m_Logger.Write("\nFinal", 2*lNpts, pfWav, LOG_NEWLINE);
      }

    }  
  }

  return S_OK;
}




  
///////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::HarmonicWaveletTransform(float* pfDataIn, float* pfDataOut, long lNpts, int iLevel)
//
// DESCRIPTION: This method deconstructs the Harmonic Wavelet Transform
//
// INPUT PARAMETER(s): 
//
// OUTPUT PARAMETER(s): 
//
// RETURN: HRESULT hr 
//
// WRITTEN BY: Coy Chanders 07-07-01
//
// LAST MODIFIED BY:
//
///////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::HarmonicWaveletTransform(float* pfData, int iOrder, int iHeight)
{
  
  // Calculate the length of complex FFT - Data is 2 times this for I & Q
  int iLength = (int)pow((double)2,iOrder);
  
  
  // Init Twiddle Table (Do we really need to do this?)
  void* pBlank = NULL;
  nspcFft((struct _SCplx *)&pBlank, iOrder, NSP_Init); 
  
  // Calculate FFT
  nspcFft((struct _SCplx *)pfData, iOrder, NSP_Forw); 

	// Free Twiddle Table (Do we really need to do this?)
	nspcFft((struct _SCplx *)&pBlank, iOrder, NSP_Free); 

	// Scale FFT - do both Real and Imaginary parts
	for (int iPosition = 0; iPosition < 2*iLength; iPosition++)
	{
		pfData[iPosition] = pfData[iPosition]/iLength;
  }

  int iLevelLength;
  int iLeftIndex;
  int iRightIndex;
  
  for (int iLevel = 1; iLevel <= (iOrder-2); iLevel++)
  {
		iLevelLength = (int)pow((double)2,iLevel);
		iLeftIndex   = iLevelLength*2;
		iRightIndex  = (int) ( (2*iLength) - (iLevelLength*4) + 2);

		// Init Twiddle Table (Do we really need to do this?)
		nspcFft((struct _SCplx *)&pBlank, iLevel, NSP_Init); 

		// Calculate the inverse FFT
		nspcFft((struct _SCplx *)&pfData[iLeftIndex], iLevel, NSP_Inv); 

		// Free Twiddle Table (Do we really need to do this?)
		nspcFft((struct _SCplx *)&pBlank, iLevel, NSP_Free); 

		// Scale Inverse FFT - do both Real & Imaginary
		for (int iCount = 0; iCount < 2*iLevelLength; iCount++)
		{
      pfData[iLeftIndex + iCount] *= (float) iLevelLength;
    }

    if (m_Logger.m_iLogLevel >= 45 && iHeight == m_iLogFileGate) 
    {
      m_Logger.Write("Harmonic wavelet transform, after IFFT at level", iLevel);
      m_Logger.Write("\nTemp", 2*iLength, pfData, LOG_NEWLINE);
    }

    // Reverse order of Real & Imaginary pairs
    ReverseOrderComplex(&pfData[iRightIndex], iLevelLength);

		// Init Twiddle Table (Do we really need to do this?)
		nspcFft((struct _SCplx *)&pBlank, iLevel, NSP_Init); 

		// Calculate the  FFT
		nspcFft((struct _SCplx *)&pfData[iRightIndex], iLevel, NSP_Forw); 

		// Free Twiddle Table (Do we really need to do this?)
		nspcFft((struct _SCplx *)&pBlank, iLevel, NSP_Free); 
    
    // Reverse order of Real & Imaginary pairs
    ReverseOrderComplex(&pfData[iRightIndex], iLevelLength);

    if (m_Logger.m_iLogLevel >= 45 && iHeight == m_iLogFileGate) 
    {
      m_Logger.Write("Harmonic wavelet transform, after FFT and Reverse at level", iLevel);
      m_Logger.Write("\nTemp", 2*iLength, pfData, LOG_NEWLINE);
    }

  }
  
  
  return S_OK;
  
}




///////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::InverseHarmonicWaveletTransform(float* pfDataIn, float* pfDataOut, long lNpts, int iLevel)
//
// DESCRIPTION: This method reconstructs the Harmonic Wavelet Transform
//
// INPUT PARAMETER(s): 
//
// OUTPUT PARAMETER(s): 
//
// RETURN: HRESULT hr 
//
// WRITTEN BY: Coy Chanders 07-07-01
//
// LAST MODIFIED BY:
//
///////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::InverseHarmonicWaveletTransform(float* pfData, int iOrder)
{

	// Calculate length of FFT. Data is 2 times this due to Real & Imaginary parts
  int iLength = (int) pow((double)2,iOrder);
  
  void* pBlank = NULL;
  
  int iLevelLength;
  int iLeftIndex;
  int iRightIndex;
  
  for (int iLevel=1; iLevel <= (iOrder-2); iLevel++)
  {
		iLevelLength = (int)pow((double)2,iLevel);
		iLeftIndex   = iLevelLength*2;
		iRightIndex  = (int) ( (2*iLength) - (iLevelLength*4) + 2);

		// Init Twiddle Table (Do we really need to do this?)
		nspcFft((struct _SCplx *)&pBlank, iLevel, NSP_Init); 

		// Calculate the FFT
		nspcFft((struct _SCplx *)&pfData[iLeftIndex], iLevel, NSP_Forw); 

		// Free Twiddle Table (Do we really need to do this?)
		nspcFft((struct _SCplx *)&pBlank, iLevel, NSP_Free); 

		// Scale - both Real & Imaginary
		for (int iCount=0; iCount < 2*iLevelLength; iCount++)
		{
			pfData[iLeftIndex + iCount] /= iLevelLength;
		}

		// Reverse order of Real & Imaginary pairs
		ReverseOrderComplex(&pfData[iRightIndex], iLevelLength);

		// Init Twiddle Table (Do we really need to do this?)
		nspcFft((struct _SCplx *)&pBlank, iLevel, NSP_Init); 

		// Calculate the Inverse FFT
		nspcFft((struct _SCplx *)&pfData[iRightIndex], iLevel, NSP_Inv); 

		// Free Twiddle Table (Do we really need to do this?)
		nspcFft((struct _SCplx *)&pBlank, iLevel, NSP_Free); 

		// Reverse order of Real & Imaginary pairs
		ReverseOrderComplex(&pfData[iRightIndex], iLevelLength);
	}

	// Init Twiddle Table (Do we really need to do this?)
	nspcFft((struct _SCplx *)&pBlank, iOrder, NSP_Init); 

	// Calculate the Inverse FFT
	nspcFft((struct _SCplx *)pfData, iOrder, NSP_Inv); 

	// Free Twiddle Table (Do we really need to do this?)
	nspcFft((struct _SCplx *)&pBlank, iOrder, NSP_Free); 

	// Scale
	for (int iCount=0; iCount < 2*iLength; iCount++)
	{
		pfData[iCount] *= iLength;
	}


	return S_OK;

}


///////////////////////////////////////////////////////////////////////
// METHOD NAME: CCustomWorkerThread::ReverseOrderComplex(float* pfData, int iLength)
//
// DESCRIPTION: This method takes a block or complex numbers and reverses their order
//
// INPUT PARAMETER(s): 
//
// OUTPUT PARAMETER(s): 
//
// RETURN: HRESULT hr 
//
// WRITTEN BY: Coy Chanders 07-07-01
//
// LAST MODIFIED BY:
//
///////////////////////////////////////////////////////////////////////
HRESULT CCustomWorkerThread::ReverseOrderComplex(float* pfData, int iLength)
{
		float fRealTemp;
    float fImaginaryTemp;
    int   iEndIndex = (iLength*2) - 2;

    // Reverse order of Real & Imaginary pairs
    for (int iCount = 0; iCount < iLength; iCount += 2)
    {
      // Store Real and Imaginary from beginning end of sequence
			fRealTemp      = pfData[iCount];
			fImaginaryTemp = pfData[iCount + 1];
			
			// Replace with values from other end of sequence
			pfData[iCount]			= pfData[iEndIndex];
			pfData[iCount + 1]	= pfData[iEndIndex+1];

			// Put values from beginning end of sequence at the end of sequence
			pfData[iEndIndex]     = fRealTemp;
			pfData[iEndIndex + 1] = fImaginaryTemp;

			// Decrement iEndIndex
			iEndIndex -= 2;
		}

	return S_OK;

}





///////////////////////////////////////////////////////////////////////
// METHOD NAME: int compare( const void *x, const void *y )
//
// DESCRIPTION: 
//
// INPUT PARAMETER(s): const void *x, const void *y 
//
// OUTPUT PARAMETER(s):
//
// RETURN: HRESULT         
//
// WRITTEN BY: 
//
// LAST MODIFIED BY:
//
///////////////////////////////////////////////////////////////////////
int compare( const void *x, const void *y )
{
	float val = 0.0F;

	val = (float)( *(float *)x -  *(float *)y);

	if( val == 0.0 )
	{
		return(0);
	}

	if( val > 0.0 ) 
	{
		return(1); 
	}
	else 
	{
		return(-1);
	}
}


///////////////////////////////////////////////////////////////////////
// METHOD NAME: int compare_double( const void *x, const void *y )
//
// DESCRIPTION: Compares elements in an array of doubles. Auxiliary function
// for qsort.
//
// INPUT PARAMETER(s): const void *x, const void *y 
//
// OUTPUT PARAMETER(s):
//
// RETURN: HRESULT         
//
// WRITTEN BY: Raisa Lehtinen 2005-01-18 / modified from 'compare' for floats.
//
// LAST MODIFIED BY:
//
///////////////////////////////////////////////////////////////////////
int compare_double( const void *x, const void *y )
{
	double val = 0.0F;

	val = (double)( *(double *)x -  *(double *)y);

	if ( val == 0.0 )
	{
		return(0);
	}

	if ( val > 0.0 ) 
	{
		return(1); 
	}
	else 
	{
		return(-1);
	}
}


///////////////////////////////////////////////////////////////////////
// METHOD NAME: OpenLogFile
//
// DESCRIPTION: Open the log file and write the header for each dwell.
//
// INPUT PARAMETER(s): pLapxmDataIn
//
// OUTPUT PARAMETER(s): None
//
// RETURN: None         
//
// WRITTEN BY: Raisa Lehtinen 2006-10-06
//
// LAST MODIFIED BY:
//
///////////////////////////////////////////////////////////////////////
HRESULT 
CCustomWorkerThread::OpenLogFile(LapxmDataStructure *pLapxmDataIn)
{
  // Create time object to get the timestamp of this dwell
  CLapTime TimeObject;
  TimeObject.InitializeTimeFromLapxmDataStructure(
    pLapxmDataIn->pBeam[0].Dwell.lDwellBeginTime,
    pLapxmDataIn->lTimeStampMilliseconds,
    pLapxmDataIn->lTime2UTMin);
  std::string strDate;
  TimeObject.GetDateStringYYJJJ(CLapTime::eLocal, &strDate);
  CBstr cbsLogFileName = m_cbsModuleName;
  cbsLogFileName = cbsLogFileName + strDate.c_str();

  int iOverWrite = 0;
  if (cbsLogFileName.Compare(m_cbsCurrentLogFile) != 0)
  {
    iOverWrite = m_iLogOverwrite;
    m_cbsCurrentLogFile = cbsLogFileName;
  }

  // Open the log file for use.
  CLapLogger::EReturnCode eReturn;
  eReturn = m_Logger.OpenFile((char*)cbsLogFileName, iOverWrite);
  if (eReturn != CLapLogger::eOK)
  {
    m_spIControl->LapxmSendMessage(MESSAGE_ERROR, CBstr(m_Logger.GetLastError().c_str()) );
    return E_FAIL;
  }

  TimeObject.GetIsoTimeString(CLapTime::eLocal, &strDate);
  m_Logger.Write("\n\n*** Wavelet results from Beam Direction", pLapxmDataIn->pBeam[0].Dwell.lDirectionCode);
  m_Logger.Write((char *)pLapxmDataIn->cStationName);
  m_Logger.Write(" ");
  m_Logger.Write(strDate.c_str());
  m_Logger.Write(" NPTS",pLapxmDataIn->pBeam[0].Dwell.lNumPointsInSpectrum * pLapxmDataIn->pBeam[0].Dwell.lNumSpectralInAverage, LOG_NEWLINE);

  return S_OK;
}


////////////////////////////////////////////////////////////////////////////////
// METHOD NAME: Daub20WaveletTransform
//
// DESCRIPTION: This was a test code, trying to make DaubClip transform faster.
// Did not look promising in terms of winning time, and working with the indices
// was too difficult - they are still wrong, and inverse transform missing.
// If speed becomes real issue, maybe new work needs to be done here.
// WRITTEN BY: Raisa Lehtinen 2005-01-31
//
////////////////////////////////////////////////////////////////////////////////
/*HRESULT 
CCustomWorkerThread::Daub20WaveletTransform(double *pdData, // data array I/O
                                            int iLength,    // length of data
                                            int iMethod,    // 1,2 = transform -1,-2 = inverse tr.
                                            int iGate)      // current range gate
{
  
  int iSmallestLevel = 4;

  int iNCoeff = 20;
  std::vector<double> vecCC(iNCoeff, 0.0); // coefficients of the smoothing filter
  std::vector<double> vecCR(iNCoeff, 0.0); // coefficients of the wavelet function
  vecCC[0]  =  0.026670057901; vecCC[1]  =  0.188176800078; vecCC[2]  =  0.527201188932;
  vecCC[3]  =  0.688459039454; vecCC[4]  =  0.281172343661; vecCC[5]  = -0.249846424327;
  vecCC[6]  = -0.195946274377; vecCC[7]  =  0.127369340336; vecCC[8]  =  0.093057364604;
  vecCC[9]  = -0.071394147166; vecCC[10] = -0.029457536822; vecCC[11] =  0.033212674059;
  vecCC[12] =  0.003606553567; vecCC[13] = -0.010733175483; vecCC[14] =  0.001395351747;
  vecCC[15] =  0.001992405295; vecCC[16] = -0.000685856695; vecCC[17] = -0.000116466855;
  vecCC[18] =  0.000093588670; vecCC[19] = -0.000013264203;
  double dMult = -1.0;
  for (int i = 0; i < iNCoeff; i++)
  {
    vecCR[iNCoeff-1-i] = dMult * vecCC[i];
    dMult = -dMult;
  }
  
  std::vector<double> vecWksp;
  
  int iIoff = -(iNCoeff >> 1); // Handle wrap-around of wavelets. iIoff and iJoff are
  int iJoff = -(iNCoeff >> 1); // here identical to center the 'support' of wavelets.

  int iNMod, iNN1, iNH;
  int iNI, iNJ, iJF, iJR;
  int j, ii;

  if (iMethod >= 0) // wavelet transform
  {
    for (int iNN = iLength; iNN >= iSmallestLevel; iNN >>= 1)
    {
      iNH = iNN >> 1;
      vecWksp.assign(iNN, 0.0);

      // begin part
      iNMod = iNCoeff * iNN;
      iNN1 = iNN - 1;
      for (ii = 0, i = 0; ii < 4 && i < iNN; i += 2, ii++)
      {
        iNI = i+1+iNMod+iIoff;
        iNJ = i+1+iNMod+iJoff; 
        for (int k = 0; k < iNCoeff; k++)
        {
          iJF = iNN1 & (iNI+k+1);
          iJR = iNN1 & (iNJ+k+1);
          vecWksp[ii] += vecCC[k] * pdData[iJF];
          vecWksp[ii+iNH] += vecCR[k] * pdData[iJR];
          if (m_Logger.m_iLogLevel >= 50 && iGate == m_iLogFileGate) 
          {
            m_Logger.Write("ii", ii);
            m_Logger.Write("k", k);
            m_Logger.Write("iJF", iJF, LOG_NEWLINE);
          }
        }
      }

      // middle part
      for (i = 4, j = 0; i < iNH-5; i++, j+=2)  
      {
        for (int k = 0; k < iNCoeff; k++)
        {
          vecWksp[i] += vecCC[k] * pdData[j+k];
          vecWksp[i+iNH] += vecCR[k] * pdData[j+k];
          if (m_Logger.m_iLogLevel >= 50 && iGate == m_iLogFileGate) 
          {
            m_Logger.Write("ii", i);
            m_Logger.Write("k", k);
            m_Logger.Write("iJF", j+k, LOG_NEWLINE);
          }
        }
      }
      

  //    for (ii = iNH-6, i = 2*ii; ii < iNH, i < iNN; i += 2, ii++)
  //    {
  //      iNI = i+1+iNMod+iIoff;
  //      iNJ = i+1+iNMod+iJoff; 
  //      for (int k = 0; k < iNCoeff; k++)
  //      {
  //        iJF = iNN1 & (iNI+k+1);
  //        iJR = iNN1 & (iNJ+k+1);
  //        vecWksp.at(ii) += vecCC[k] * pdData[iJF];
  //        vecWksp.at(ii+iNH) += vecCR[k] * pdData[iJR];
  //        if (m_Logger.m_iLogLevel >= 60 && iGate == m_iLogFileGate) 
  //        {
  //          m_Logger.Write("iJF",iJF, LOG_NEWLINE);
  //        }
  //      }
  //    }  
      
      for (i = 0; i < iNN; i++)
      {
        pdData[i] = vecWksp[i];
      }
      if (m_Logger.m_iLogLevel >= 60 && iGate == m_iLogFileGate) 
      {
        m_Logger.Write("DWTlevel_60", vecWksp, LOG_NEWLINE);
      }
    }
  }
  else
  {
  }
  
  return S_OK;
}
*/
