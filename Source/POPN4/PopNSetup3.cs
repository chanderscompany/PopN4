using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using DACarter.NOAA.Hardware;
using DACarter.PopUtilities;
using DACarter.Utilities.Graphics;
using DACarter.Utilities;
using POPCommunication;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace POPN {
	public partial class PopNSetup3 : Form {

        [DllImport("user32")]
        public static extern int MessageBeep(int wType);

        public static bool IsLiving = false;

        #region Constructor
        //
        public PopNSetup3() {

            IsLiving = true;
            InitializeComponent();
            RxSampleOrder = new List<string>();
            _parameters = null;
            checkBoxTxOn_CheckedChanged(null, null);
			_rampIsOriginal = true;
			_originalRampRate = 0.0;
            //PulseBox = null;

			_lastNegFirstGate = 0;
			_lastNegLastGate = 0;
			_lastNegOffset = 999.99;
			_lastPosFirstGate = 0;
			_lastPosLastGate = 0;
			_lastPosOffset = -999.99;
			_usesMaxGate = true;
			_maxGate = -99;
            _nDaq = 0;

            _communicator = null;
            _saveButtonClicked = false;

            _currentDirectory = Application.StartupPath;

            // These textboxes only allow integer values, so we validate them
            textBoxFmCwSysDelayNs.Validating += textBox_ValidatingInteger;
            textBoxFmCwPostBlankUs.Validating += textBox_ValidatingInteger;
            textBoxFmCwTimeStepClocks.Validating += textBox_ValidatingInteger;
            textBoxSweepNPts.Validating += textBox_ValidatingInteger;
            textBoxSweepNSpec.Validating += textBox_ValidatingInteger;
            textBoxSweepSampleDelay.Validating += textBox_ValidatingInteger;
            textBoxSweepSpacing.Validating += textBox_ValidatingInteger;
            textBoxXCMaxLag.Validating += textBox_ValidatingInteger;
            textBoxPolyFitOrder.Validating += textBox_ValidatingInteger;
            textBoxXCLineFitPts.Validating += textBox_ValidatingInteger;
            textBoxLagsToFit.Validating += textBox_ValidatingInteger;
            textBoxLagsToInterp.Validating += textBox_ValidatingInteger;
            textBoxDopNPts.Validating += textBox_ValidatingInteger;
            textBoxDopNSpec.Validating += textBox_ValidatingInteger;
            textBoxXCNptMult.Validating += textBox_ValidatingInteger;


        }
        //
        #endregion Constructor

        #region Private Fields
        //
        private List<string> RxSampleOrder;
        private DACarter.PopUtilities.PopParameters _parameters;
        private PopParameters _backupParameters;
        private PopParameters _savedParameters;
        private PulseGeneratorDevice _pulseBox;
		//private AD9959EvalBd _DDS;
		private double _originalRampRate, _originalOffset;
		private bool _rampIsOriginal;

		private double _lastPosOffset, _lastNegOffset, _lastOffset;
		private int _lastPosFirstGate, _lastPosLastGate;
		private int _lastNegFirstGate, _lastNegLastGate;
		private bool _usesMaxGate;
		private int _maxGate;
        private int _nDaq;
        private POPCommunicator _communicator;

        private string _currentDirectory;
        string _filterFolderFullPath;
        string _filterFolderRelPath;

        private int _savedPostBlankNs;

        private bool _saveButtonClicked;

		// when in FMCW mode, this is the 
		//	pulsed Doppler sample delay that would produce the same first gate range:
		private int _virtualRangeDelayNs = -999;
		//
        #endregion

        public POPCommunicator Communicator {
            get { return _communicator; }
            set { _communicator = value; }
        }

        #region Public Methods
        //
        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// To set or get the parameter set displayed in this form.
        /// When setting, the form is filled in with the parameter values.
        /// When set to null, the form is left blank.
        /// </summary>
        public PopParameters SetupParameters {
            get { return _parameters; }
            set {
                _parameters = value.DeepCopy();
                if (value != null) {

                    //_currentDirectory = Application.StartupPath;
                    string filterFileName = _parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile;
                    string filterFolder = Path.GetDirectoryName(filterFileName);
                    Tools.GetFullRelPath(_currentDirectory, filterFolder, out _filterFolderFullPath, out _filterFolderRelPath);

                    LayoutParameterScreens();
                    FillParameterScreens();
                }
            }
        }
        //
        #endregion

        #region Private Tab Page Methods
        //
        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void LayoutParameterScreens() {
            // Set up the Beam Sequence grid
            dataGridViewSequence.RowCount = _parameters.ArrayDim.MAXDIRECTIONS + 1;
            dataGridViewSequence.ColumnCount = _parameters.ArrayDim.MAXBEAMS + 1;
            dataGridViewSequence.Font = new Font("Tahoma", 9.0F);
            dataGridViewSequence.Rows[0].DefaultCellStyle.BackColor = Color.AliceBlue;
            dataGridViewSequence.Rows[0].Cells[0].ReadOnly = true;
            dataGridViewSequence.Rows[0].Cells[0].Value = "NReps:";
            dataGridViewSequence.Rows[0].Cells[0].Style.Font = new Font("Tahoma", 8.0F);
            for (int i = 1; i < dataGridViewSequence.RowCount; i++) {
                //dataGridViewSequence.Rows[i].Cells[0].Value = _parameters.SystemPar.RadarPar.BeamDirections[i - 1].Label;
                dataGridViewSequence.Rows[i].Cells[0].ReadOnly = true;
                dataGridViewSequence.Rows[i].Cells[0].Style.BackColor = Color.LightCyan;
            }
            dataGridViewBeamPar.RowCount = 16;
            dataGridViewBeamPar.Rows[0].HeaderCell.Value = "IPP (usec):";
            dataGridViewBeamPar.Rows[1].HeaderCell.Value = "PW (nsec):";
            dataGridViewBeamPar.Rows[2].HeaderCell.Value = "Code Bits:";
            dataGridViewBeamPar.Rows[3].HeaderCell.Value = "Phase Flip:";
            dataGridViewBeamPar.Rows[4].HeaderCell.Value = "Nhts:";
            dataGridViewBeamPar.Rows[5].HeaderCell.Value = "1st Sample (nsec):";
            dataGridViewBeamPar.Rows[6].HeaderCell.Value = "Spacing (nsec):";
            dataGridViewBeamPar.Rows[7].HeaderCell.Value = "Coh. Avg:";
            dataGridViewBeamPar.Rows[8].HeaderCell.Value = "Spectral Avg:";
            dataGridViewBeamPar.Rows[9].HeaderCell.Value = "Npts:";
            dataGridViewBeamPar.Rows[10].HeaderCell.Value = "Full Scale (m/s):";
            dataGridViewBeamPar.Rows[11].HeaderCell.Value = "Dwell (sec):";
            dataGridViewBeamPar.Rows[12].HeaderCell.Value = "First Range (km):";
            dataGridViewBeamPar.Rows[13].HeaderCell.Value = "Last Range:";
            dataGridViewBeamPar.Rows[14].HeaderCell.Value = "IPP (km):";
            dataGridViewBeamPar.Rows[15].HeaderCell.Value = "Pulse Res (m):";
            dataGridViewBeamPar.Rows[10].ReadOnly = true;
            dataGridViewBeamPar.Rows[11].ReadOnly = true;
            dataGridViewBeamPar.Rows[12].ReadOnly = true;
            dataGridViewBeamPar.Rows[13].ReadOnly = true;
            dataGridViewBeamPar.Rows[14].ReadOnly = true;
            dataGridViewBeamPar.Rows[15].ReadOnly = true;
            dataGridViewBeamPar.Rows[10].DefaultCellStyle.BackColor = Color.Linen;
            dataGridViewBeamPar.Rows[11].DefaultCellStyle.BackColor = Color.Linen;
            dataGridViewBeamPar.Rows[12].DefaultCellStyle.BackColor = Color.Linen;
            dataGridViewBeamPar.Rows[13].DefaultCellStyle.BackColor = Color.Linen;
            dataGridViewBeamPar.Rows[14].DefaultCellStyle.BackColor = Color.Linen;
            dataGridViewBeamPar.Rows[15].DefaultCellStyle.BackColor = Color.Linen;
            dataGridViewBeamPar.Rows[10].DefaultCellStyle.SelectionBackColor = Color.Linen;
            dataGridViewBeamPar.Rows[11].DefaultCellStyle.SelectionBackColor = Color.Linen;
            dataGridViewBeamPar.Rows[12].DefaultCellStyle.SelectionBackColor = Color.Linen;
            dataGridViewBeamPar.Rows[13].DefaultCellStyle.SelectionBackColor = Color.Linen;
            dataGridViewBeamPar.Rows[14].DefaultCellStyle.SelectionBackColor = Color.Linen;
            dataGridViewBeamPar.Rows[15].DefaultCellStyle.SelectionBackColor = Color.Linen;


            // Set up the Beam Parameters grid
            /*
            dataGridViewBeamPar.RowCount = 16 + 1;
            dataGridViewBeamPar.ColumnCount = _parameters.ArrayDim.MAXBEAMPAR+1;
            dataGridViewBeamPar.Rows[1].Cells[0].Value = "IPP (usec:)";
            dataGridViewBeamPar.Rows[2].Cells[0].Value = "PW (nsec):";
            dataGridViewBeamPar.Rows[3].Cells[0].Value = "Code Bits:";
            dataGridViewBeamPar.Rows[4].Cells[0].Value = "Phase Flip:";
            dataGridViewBeamPar.Rows[5].Cells[0].Value = "NHts:";
            dataGridViewBeamPar.Rows[6].Cells[0].Value = "1st Sample (nsec):";
            dataGridViewBeamPar.Rows[7].Cells[0].Value = "Spacing (nsec):";
            dataGridViewBeamPar.Rows[8].Cells[0].Value = "Coh. Avg.:";
            dataGridViewBeamPar.Rows[9].Cells[0].Value = "Spectral Avg:";
            dataGridViewBeamPar.Rows[10].Cells[0].Value = "Npts:";
            dataGridViewBeamPar.Rows[11].Cells[0].Value = "Scale (m/s):";
            dataGridViewBeamPar.Rows[12].Cells[0].Value = "Dwell (sec):";
            dataGridViewBeamPar.Rows[13].Cells[0].Value = "First Range (km):";
            dataGridViewBeamPar.Rows[14].Cells[0].Value = "Last Range:";
            dataGridViewBeamPar.Rows[15].Cells[0].Value = "IPP (km):";
            dataGridViewBeamPar.Rows[16].Cells[0].Value = "Pulse (m):";
            for (int i = 0; i < dataGridViewBeamPar.RowCount; i++) {
                dataGridViewBeamPar.Rows[i].Cells[0].ReadOnly = true;
                dataGridViewBeamPar.Rows[i].Cells[0].Style.BackColor = Color.LightCyan;
                dataGridViewBeamPar.Rows[i].Cells[0].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                if (i>=11) {
                    dataGridViewBeamPar.Rows[i].DefaultCellStyle.BackColor = Color.OldLace;
                    dataGridViewBeamPar.Rows[i].ReadOnly = true;
                }
            }
            for (int i = 1; i < dataGridViewBeamPar.ColumnCount; i++) {
                dataGridViewBeamPar.Rows[0].Cells[i].ReadOnly = true;
                dataGridViewBeamPar.Rows[0].Cells[i].Value = "    #" + i.ToString() + "    ";
            }
            dataGridViewBeamPar.Rows[0].DefaultCellStyle.BackColor = Color.LightCyan;
            DataGridViewCheckBoxCell cbCell = new DataGridViewCheckBoxCell();
            cbCell.TrueValue = "True";
            cbCell.FalseValue = "False";
            //dataGridViewBeamPar[1,4] = cbCell;
            //dataGridViewBeamPar[1, 4].Style = cbCell.Style;
             * */

            //
            dataGridViewDirections.RowCount = _parameters.ArrayDim.MAXDIRECTIONS;

            //
            dataGridViewRxBw.RowCount = _parameters.ArrayDim.MAXBW;
            for (int i = 0; i < dataGridViewRxBw.RowCount; i++) {
                dataGridViewRxBw.Rows[i].HeaderCell.Value = i + " :";
            }

			// in FMCW page
            comboBoxSampleWindow.Items.Clear();
            comboBoxDopWindow.Items.Clear();
			string[] windowNames = Enum.GetNames(typeof(PopParameters.WindowType));
			comboBoxSampleWindow.Items.AddRange(windowNames);
			comboBoxDopWindow.Items.AddRange(windowNames);

            /*
			comboBoxFilterFile.Items.Clear();
			string appPath = Application.StartupPath;
			string[] filterFileNames = Directory.GetFiles(appPath, "*.coeff");
			foreach (string fullPath in filterFileNames) {
				comboBoxFilterFile.Items.Add(Path.GetFileName(fullPath));
			}
             * */


            /*
            comboBoxSampleWindow.Items.Add(PopParameters.WindowType.Riesz.ToString());
            comboBoxSampleWindow.Items.Add(PopParameters.WindowType.Hanning.ToString());
            comboBoxSampleWindow.Items.Add(PopParameters.WindowType.Blackman.ToString());
            comboBoxSampleWindow.Items.Add(PopParameters.WindowType.Rectangular.ToString());
            comboBoxDopWindow.Items.Clear();
            comboBoxDopWindow.Items.Add(PopParameters.WindowType.Riesz.ToString());
            comboBoxDopWindow.Items.Add(PopParameters.WindowType.Hanning.ToString());
            comboBoxDopWindow.Items.Add(PopParameters.WindowType.Blackman.ToString());
            comboBoxDopWindow.Items.Add(PopParameters.WindowType.Rectangular.ToString());
            */
        }
        
        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void FillParameterScreens() {
            FillDwellPage();
            FillSystemPage();
			FillProcessingPage();
			FillFmCwPage();
			FillOutputPage();
			FillMeltingLayerPage();
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private bool SaveAllTabPages(bool showErrorMsg) {
            bool sysPageOk;
            bool FmCwPageOk;
            bool OutputPageOk;
            SaveDwellPage();
            sysPageOk = SaveSystemPage(showErrorMsg);
			SaveProcessingPage();
            FmCwPageOk = SaveFmCwPage(showErrorMsg);
            OutputPageOk = SaveOutputPage(showErrorMsg);
			SaveMeltingLayerPage();
			//_parameters.MomentExcludeIntervals = new PopParameters.MomentExcludeInterval[2];
            return (sysPageOk && FmCwPageOk && OutputPageOk);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        ///
        /// </summary>
        /// <remarks>
        /// Starting with rev 2.12, IPP can be entered as float -- dac
        /// </remarks>
        private void FillDwellPage()
        {
            /*
            string[] strArray;
            string line = "  Label:   1.234,   77444  333";
            char[] delim = { ' ', ',', '\t' };
            strArray = line.Split(delim);
            string dub = "1.0e4";
            double val;
            bool ok = double.TryParse(dub, out val);
             * */


            // Fill text boxes
            numericUpDownBmPars.Value = _parameters.ArrayDim.MAXBEAMPAR;
			textBoxFileName.Text = _parameters.Source;
			textBoxFileName.SelectionStart = textBoxFileName.TextLength;
			if (File.Exists(_parameters.Source)) {
                Environment.CurrentDirectory = Path.GetDirectoryName(_parameters.Source);
            }
            labelStationName.Text = _parameters.SystemPar.StationName;
            labelRadarName.Text = _parameters.SystemPar.RadarPar.RadarName;
            labelTxFreq.Text = _parameters.SystemPar.RadarPar.TxFreqMHz.ToString("F3") + " MHz";
            if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwDop) {
                labelRadartype.Text = "FM CW Doppler Radar";
            }
            else if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA) {
                labelRadartype.Text = "FM CW Spaced Ant Radar";
            }
            else if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.PulsedTx) {
                labelRadartype.Text = "Pulsed Doppler Radar";
            }
            // Fill in the Beam Parameters grid

            if (_parameters.SystemPar.RadarPar.TxIsOn) {
                checkBoxTxOn.Checked = true;
            }
            else {
                checkBoxTxOn.Checked = false;
            }

            // add or subtract columns as needed
            int neededColumns = _parameters.ArrayDim.MAXBEAMPAR;
            int curColumns = dataGridViewBeamPar.Columns.Count;
            int extraColumns = curColumns - neededColumns;
            if (extraColumns > 0) {
                for (int i = 0; i < extraColumns; i++) {
                    dataGridViewBeamPar.Columns.RemoveAt(curColumns - 1 - i);
                }
            }
            else if (extraColumns < 0) {
                if (neededColumns > 1 && curColumns < 2) {
                    dataGridViewBeamPar.Columns.Add(this.Column2);
                }
                if (neededColumns > 2 && curColumns < 3) {
                    dataGridViewBeamPar.Columns.Add(this.Column3);
                }
                if (neededColumns > 3 && curColumns < 4) {
                    dataGridViewBeamPar.Columns.Add(this.Column4);
                }
            }
            // populate grid with beam parameters
            for (int ipar = 0; ipar < _parameters.ArrayDim.MAXBEAMPAR; ipar++) {
                dataGridViewBeamPar.Columns[ipar].HeaderCell.Value = "#" + (ipar + 1).ToString();
                DataGridViewRowCollection rows = dataGridViewBeamPar.Rows;
                PopParameters.BeamParameters[] bmpar = _parameters.SystemPar.RadarPar.BeamParSet;
                rows[0].Cells[ipar].Value = bmpar[ipar].IppMicroSec.ToString();
                rows[1].Cells[ipar].Value = bmpar[ipar].PulseWidthNs;
                rows[2].Cells[ipar].Value = bmpar[ipar].NCode;
                if (_parameters.SystemPar.RadarPar.BeamParSet[ipar].TxPhaseFlipIsOn) {
                    rows[3].Cells[ipar].Value = 1;
                }
                else {
                    rows[3].Cells[ipar].Value = 0;
                }
                rows[4].Cells[ipar].Value = bmpar[ipar].NHts;
                rows[5].Cells[ipar].Value = bmpar[ipar].SampleDelayNs;
                rows[6].Cells[ipar].Value = bmpar[ipar].SpacingNs;
                rows[7].Cells[ipar].Value = bmpar[ipar].NCI;
                rows[8].Cells[ipar].Value = bmpar[ipar].NSpec;
                rows[9].Cells[ipar].Value = bmpar[ipar].NPts;
                rows[10].Cells[ipar].ReadOnly = true;
                rows[11].ReadOnly = true;

            }
            // Fill in the Beam Sequence grid
            for (int i = 1; i < dataGridViewSequence.RowCount; i++) {
                dataGridViewSequence.Rows[i].Cells[0].Value = _parameters.SystemPar.RadarPar.BeamDirections[i - 1].Label;
            }
            for (int j = 1; j < dataGridViewSequence.ColumnCount; j++) {
                for (int idir = 0; idir < _parameters.ArrayDim.MAXDIRECTIONS; idir++) {
                    dataGridViewSequence.Rows[idir + 1].Cells[j].Value = "";
                }
                int nReps = _parameters.SystemPar.RadarPar.BeamSequence[j - 1].NumberOfReps;
                if (nReps != 0) {
                    dataGridViewSequence.Rows[0].Cells[j].Value = nReps.ToString();
                    int directionIndex = _parameters.SystemPar.RadarPar.BeamSequence[j - 1].DirectionIndex;
                    dataGridViewSequence.Rows[directionIndex + 1].Cells[j].Value = (_parameters.SystemPar.RadarPar.BeamSequence[j - 1].ParameterIndex + 1).ToString();
                }
                else {
                    dataGridViewSequence.Rows[0].Cells[j].Value = "";
                    int directionIndex = _parameters.SystemPar.RadarPar.BeamSequence[j - 1].DirectionIndex;
                    for (int idir = 0; idir < _parameters.ArrayDim.MAXDIRECTIONS; idir++) {
                        dataGridViewSequence.Rows[idir + 1].Cells[j].Value = "";
                    }
                }
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        ///
        /// </summary>
        /// <remarks>
        /// Starting with rev 2.12, IPP can be entered as float -- dac
        /// </remarks>
        private void SaveDwellPage() {
            bool isOK;
            //double value;
            int iValue;

            dataGridViewBeamPar.EndEdit();
            dataGridViewSequence.EndEdit();

            isOK = int.TryParse(numericUpDownBmPars.Text, out _parameters.ArrayDim.MAXBEAMPAR);
            if (checkBoxTxOn.Checked) {
                _parameters.SystemPar.RadarPar.TxIsOn = true;
            }
            else {
                _parameters.SystemPar.RadarPar.TxIsOn = false;
            }

            int flipNum;
            for (int ipar = 0; ipar < _parameters.ArrayDim.MAXBEAMPAR; ipar++) {
                isOK = double.TryParse(dataGridViewBeamPar.Rows[0].Cells[ipar].Value.ToString(),
                                    out _parameters.SystemPar.RadarPar.BeamParSet[ipar].IppMicroSec);
                isOK = int.TryParse(dataGridViewBeamPar.Rows[1].Cells[ipar].Value.ToString(),
                                    out _parameters.SystemPar.RadarPar.BeamParSet[ipar].PulseWidthNs);
                isOK = int.TryParse(dataGridViewBeamPar.Rows[2].Cells[ipar].Value.ToString(),
                                    out _parameters.SystemPar.RadarPar.BeamParSet[ipar].NCode);
                isOK = int.TryParse(dataGridViewBeamPar.Rows[3].Cells[ipar].Value.ToString(),
                                    out flipNum);
                if (flipNum == 0) {
                    _parameters.SystemPar.RadarPar.BeamParSet[ipar].TxPhaseFlipIsOn = false;
                }
                else {
                    _parameters.SystemPar.RadarPar.BeamParSet[ipar].TxPhaseFlipIsOn = true;
                }
                isOK = int.TryParse(dataGridViewBeamPar.Rows[4].Cells[ipar].Value.ToString(),
                                    out _parameters.SystemPar.RadarPar.BeamParSet[ipar].NHts);
                isOK = int.TryParse(dataGridViewBeamPar.Rows[5].Cells[ipar].Value.ToString(),
                                    out _parameters.SystemPar.RadarPar.BeamParSet[ipar].SampleDelayNs);
                isOK = int.TryParse(dataGridViewBeamPar.Rows[6].Cells[ipar].Value.ToString(),
                                    out _parameters.SystemPar.RadarPar.BeamParSet[ipar].SpacingNs);
                isOK = int.TryParse(dataGridViewBeamPar.Rows[7].Cells[ipar].Value.ToString(),
                                    out _parameters.SystemPar.RadarPar.BeamParSet[ipar].NCI);
                isOK = int.TryParse(dataGridViewBeamPar.Rows[8].Cells[ipar].Value.ToString(),
                                    out _parameters.SystemPar.RadarPar.BeamParSet[ipar].NSpec);
                isOK = int.TryParse(dataGridViewBeamPar.Rows[9].Cells[ipar].Value.ToString(),
                                    out _parameters.SystemPar.RadarPar.BeamParSet[ipar].NPts);

				// find proper bandwidth setting
				int ibw = 0;
				if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.PulsedTx) {
					int pw = _parameters.SystemPar.RadarPar.BeamParSet[ipar].PulseWidthNs;
					int bwBoundary1 = (_parameters.SystemPar.RadarPar.RxBw[0].BwPwNs + _parameters.SystemPar.RadarPar.RxBw[1].BwPwNs) / 2;
					int bwBoundary2 = (_parameters.SystemPar.RadarPar.RxBw[1].BwPwNs + _parameters.SystemPar.RadarPar.RxBw[2].BwPwNs) / 2;
					int bwBoundary3 = (_parameters.SystemPar.RadarPar.RxBw[2].BwPwNs + _parameters.SystemPar.RadarPar.RxBw[3].BwPwNs) / 2;
					if (pw < bwBoundary1) {
						ibw = 0;
					}
					else if (pw < bwBoundary2) {
						ibw = 1;
					}
					else if (pw < bwBoundary3) {
						ibw = 2;
					}
					else {
						ibw = 3;
					}
				}
				else {
					// FMCW
					ibw = 0;
				}

				_parameters.SystemPar.RadarPar.BeamParSet[ipar].BwCode = ibw;
				_parameters.SystemPar.RadarPar.BeamParSet[ipar].SystemDelayNs =
						_parameters.SystemPar.RadarPar.RxBw[ibw].BwDelayNs;
            }

            for (int j = 0; j < _parameters.ArrayDim.MAXBEAMS; j++) {
                isOK = int.TryParse(dataGridViewSequence.Rows[0].Cells[j + 1].Value.ToString(),
                                    out _parameters.SystemPar.RadarPar.BeamSequence[j].NumberOfReps);
                for (int i = 0; i < _parameters.ArrayDim.MAXDIRECTIONS; i++) {
                    if (dataGridViewSequence.Rows[i + 1].Cells[j + 1].Value != null) {
						if (_parameters.SystemPar.RadarPar.BeamSequence[j].NumberOfReps > 0) {

							isOK = int.TryParse(dataGridViewSequence.Rows[i + 1].Cells[j + 1].Value.ToString(), out iValue);
							if (isOK) {
								_parameters.SystemPar.RadarPar.BeamSequence[j].DirectionIndex = i;
								_parameters.SystemPar.RadarPar.BeamSequence[j].ParameterIndex = iValue - 1;
								break;	// just taking first value in column
							}
						}
						else {
							_parameters.SystemPar.RadarPar.BeamSequence[j].DirectionIndex = 0;
							_parameters.SystemPar.RadarPar.BeamSequence[j].ParameterIndex = 0;
						}

                    }
                }
            }
        }

        /// <summary>
        /// Call this when change SysDelay on FmCw page
        /// </summary>
        private void FillSystemPageSysDelay0() {
            dataGridViewRxBw.Rows[0].Cells[1].Value = _parameters.SystemPar.RadarPar.RxBw[0].BwDelayNs.ToString("F0");
        }

        private void FillSystemPagePostBlank() {
            textBoxPostBlank.Text = _parameters.SystemPar.RadarPar.PBConstants.PBPostBlank.ToString();
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void FillSystemPage() {

            if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwDop) {
                radioButtonFmCwType.Checked = true;
            }
            else if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA) {
                radioButtonFMCWSAType.Checked = true;
            }
            else if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.PulsedTx) {
                radioButtonPulsedType.Checked = true;
            }

            if (_parameters.SystemPar.RadarPar.RadarType != PopParameters.TypeOfRadar.PulsedTx) {
                // POPREV: for FMCW certain pulses are fixed (rev 3.13)
                //_parameters.SystemPar.RadarPar.PBConstants.PBPreTR = 0;
                //_parameters.SystemPar.RadarPar.PBConstants.PBPostTR = 0;
                //_parameters.SystemPar.RadarPar.PBConstants.PBPreBlank = 0;
                textBoxPreTR.Enabled = false;
                textBoxPreBlank.Enabled = false;
                textBoxPostTR.Enabled = false;
                textBoxPostBlank.Enabled = false;  // is set from FMCW page
                textBoxSync.Enabled = false;
            }
            else {
                textBoxPreTR.Enabled = true;
                textBoxPreBlank.Enabled = true;
                textBoxPostTR.Enabled = true;
                textBoxPostBlank.Enabled = true; 
                textBoxSync.Enabled = true;
            }

            int nrx = _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
            numericUpDownNRx.Value = (decimal)nrx;
            int nrxDim = _parameters.SystemPar.RadarPar.ProcPar.RxID.Length;
            if (nrx > nrxDim) {
                throw new ApplicationException("RxID dimension less than NRx.");
            }
            if (nrxDim > 0) {
                comboBoxRx1.Text = _parameters.SystemPar.RadarPar.ProcPar.RxID[0].RxIDName;
                int i = _parameters.SystemPar.RadarPar.ProcPar.RxID[0].iRx;
                if (i != 0) {
                    // debug check: make sure deserialize reads array elements in proper order
                    throw new ApplicationException("Parameter file RxID first index i not 0 in FillSystemPage.");
                }
            }
            if (nrxDim > 1) {
                comboBoxRx2.Text = _parameters.SystemPar.RadarPar.ProcPar.RxID[1].RxIDName;
            }
            if (nrxDim > 2) {
                comboBoxRx3.Text = _parameters.SystemPar.RadarPar.ProcPar.RxID[2].RxIDName;
            }
            buttonRefreshRx_Click(null, null);
            
            numericUpDownDirections.Value = _parameters.ArrayDim.MAXDIRECTIONS;
            numericUpDownBeamSeq.Value = _parameters.ArrayDim.MAXBEAMS;
            textBoxStationName.Text = _parameters.SystemPar.StationName;
            textBoxRadarName.Text = _parameters.SystemPar.RadarPar.RadarName;
            textBoxLatitude.Text = _parameters.SystemPar.Latitude.ToString("F2");
            if (_parameters.SystemPar.Latitude < 0.0) {
                textBoxLatitude.Text = (-_parameters.SystemPar.Latitude).ToString("F2");
                comboBoxLatitude.Text = "South";
            }
            else {
                comboBoxLatitude.Text = "North";
            }
            textBoxLongitude.Text = _parameters.SystemPar.Longitude.ToString("F2");
            if (_parameters.SystemPar.Longitude < 0.0) {
                textBoxLongitude.Text = (-_parameters.SystemPar.Longitude).ToString("F2");
                comboBoxLongitude.Text = "West";
            }
            else {
                comboBoxLongitude.Text = "East";
            }
            textBoxUTCorrection.Text = ((double)(_parameters.SystemPar.MinutesToUT / 60.0)).ToString("F2");
            textBoxAltitude.Text = _parameters.SystemPar.Altitude.ToString();
            textBoxSiteID.Text = _parameters.SystemPar.RadarPar.RadarID.ToString();
            textBoxTxFreq.Text = _parameters.SystemPar.RadarPar.TxFreqMHz.ToString("F2");
            textBoxAntSpacingM.Text = _parameters.SystemPar.RadarPar.AntSpacingM.ToString("F3");
            textBoxAsubH.Text = _parameters.SystemPar.RadarPar.ASubH.ToString("F3");
            textBoxMaxDutyCycle.Text = ((int)(_parameters.SystemPar.RadarPar.MaxTxDutyCycle * 100.0 + 0.5)).ToString();
            textBoxMaxTx.Text = _parameters.SystemPar.RadarPar.MaxTxLengthUsec.ToString();
            textBoxMinIpp.Text = _parameters.SystemPar.RadarPar.MinIppUsec.ToString();

            textBoxPbxClock.Text = _parameters.SystemPar.RadarPar.PBConstants.PBClock.ToString();
            textBoxPreTR.Text = _parameters.SystemPar.RadarPar.PBConstants.PBPreTR.ToString();
            textBoxPostTR.Text = _parameters.SystemPar.RadarPar.PBConstants.PBPostTR.ToString();
            textBoxPreBlank.Text = _parameters.SystemPar.RadarPar.PBConstants.PBPreBlank.ToString();
            textBoxPostBlank.Text = _parameters.SystemPar.RadarPar.PBConstants.PBPostBlank.ToString();
            textBoxSync.Text = _parameters.SystemPar.RadarPar.PBConstants.PBSynch.ToString();

            // USB Power meter
            checkBoxPowerMeterEnable.Checked = _parameters.SystemPar.RadarPar.PowMeterPar.Enabled;
            textBoxPMIntervalSec.Text = _parameters.SystemPar.RadarPar.PowMeterPar.WriteIntervalSec.ToString();
            textBoxPMOffsetdB.Text = _parameters.SystemPar.RadarPar.PowMeterPar.OffsetDB.ToString("F2");

            // fill in the beam directions
            for (int i = 0; i < _parameters.ArrayDim.MAXDIRECTIONS; i++) {
                string label = _parameters.SystemPar.RadarPar.BeamDirections[i].Label;
                dataGridViewDirections.Rows[i].Cells[0].Value = label;
                if ((label != String.Empty) && !((label.Trim().StartsWith("_")) && (label.Trim().EndsWith("_")))) {
                    dataGridViewDirections.Rows[i].Cells[1].Value = _parameters.SystemPar.RadarPar.BeamDirections[i].Azimuth.ToString("F0");
                    dataGridViewDirections.Rows[i].Cells[2].Value = _parameters.SystemPar.RadarPar.BeamDirections[i].Elevation.ToString("F2");
                    dataGridViewDirections.Rows[i].Cells[3].Value = _parameters.SystemPar.RadarPar.BeamDirections[i].SwitchCode.ToString("F0");
                }
                else {
                    dataGridViewDirections.Rows[i].Cells[1].Value = String.Empty;
                    dataGridViewDirections.Rows[i].Cells[2].Value = String.Empty;
                    dataGridViewDirections.Rows[i].Cells[3].Value = String.Empty;
                }
            }

            // fill in RxBw Table (not for FMCW - except display sysdelay in RxBw[0].BwDelayNs)
            for (int i = 0; i < _parameters.ArrayDim.MAXBW; i++) {
                dataGridViewRxBw.Rows[i].Cells[0].Value = _parameters.SystemPar.RadarPar.RxBw[i].BwPwNs.ToString("F0");
                dataGridViewRxBw.Rows[i].Cells[1].Value = _parameters.SystemPar.RadarPar.RxBw[i].BwDelayNs.ToString("F0");
                if (_parameters.SystemPar.RadarPar.RadarType != PopParameters.TypeOfRadar.PulsedTx) {
                    dataGridViewRxBw.Rows[i].Cells[0].ReadOnly = true;
                    dataGridViewRxBw.Rows[i].Cells[1].ReadOnly = true;
                    dataGridViewRxBw.Rows[i].Cells[0].Value = "";
                    if (i != 0) {
                        dataGridViewRxBw.Rows[i].Cells[1].Value = "";
                    }
                }
                else {
                    dataGridViewRxBw.Rows[i].Cells[0].ReadOnly = false;
                    dataGridViewRxBw.Rows[i].Cells[1].ReadOnly = false;
                }
           }
        }


        private void buttonRefreshRx_Click(object sender, EventArgs e) {
            //int nDaq;
            string[] deviceNames = null;
            try {
                //_nDaq = daqBoard.NumDevices;
                if (Communicator != null) {
                    if ((_parameters == null) || (_parameters.Debug.NoHardware)) {
                        if (_parameters.Debug.NoHardware) {
                            _nDaq = 0;
                            deviceNames = new string[1];
                            deviceNames[0] = "";
                        }
                    }
                    else {
                        Communicator.RequestNumDaqDevices(out _nDaq, out deviceNames);
                    }
                }
                else {
                    _nDaq = 0;
                    deviceNames = new string[1];
                    deviceNames[0] = "";
                }
            }
            catch (Exception exc) {
                MessageBoxEx.Show(exc.Message, 3000);
                MessageBeep((int)MessageBoxIcon.Error);
                _nDaq = 0;
            }

            comboBoxRx1.Items.Clear();
            comboBoxRx2.Items.Clear();
            comboBoxRx3.Items.Clear();
            RxSampleOrder.Clear();
            if (_nDaq < 3) {
                comboBoxRx3.Text = "";
            }
            if (_nDaq < 2) {
                comboBoxRx2.Text = "";
            }
            if (_nDaq < 1) {
                comboBoxRx1.Text = "";
            }

            
            if (_nDaq != 0) {
                foreach (string name in deviceNames) {
                    /*
                    // device name includes serial number between braces;
                    //  make the serial # the RxID
                    int i1 = name.IndexOf('{');
                    int i2 = name.IndexOf('}');
                    string id;
                    try {
                        id = name.Substring(i1 + 1, i2 - i1 - 1);
                    }
                    catch {
                        // probably not a real DAQ device.
                        // Display full name:
                        id = name;
                    }
                     * */
                    // as of POPN4 name returned is just serial #
                    string id = name;
                    comboBoxRx1.Items.Add(id);
                    RxSampleOrder.Add(id);
                    if (_nDaq > 1) {
                        comboBoxRx2.Items.Add(id);
                        //RxSampleOrder.Add(id);
                    }
                    if (_nDaq > 2) {
                        comboBoxRx3.Items.Add(id);
                        //RxSampleOrder.Add(id);
                    }
                }
            }
            /*
            if (comboBoxRx1.Items.Count == 0) {
                comboBoxRx1.Items.Add("-");
            }
            if (nDaq > 1) {
                if (comboBoxRx2.Items.Count == 0) {
                    comboBoxRx2.Items.Add("-");
                }
            }
            if (nDaq > 2) {
                if (comboBoxRx3.Items.Count == 0) {
                    comboBoxRx3.Items.Add("-");
                }
            }
             * */
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private bool SaveSystemPage(bool showErrorMsg) {
            bool isOK;
            double value;

            dataGridViewDirections.EndEdit();
            dataGridViewRxBw.EndEdit();

            // RadarType
            checkBoxTxOn_CheckedChanged(null, null);
            // station and radar names
            _parameters.SystemPar.StationName = textBoxStationName.Text;
            _parameters.SystemPar.RadarPar.RadarName = textBoxRadarName.Text;
            // Latitude
            isOK = double.TryParse(textBoxLatitude.Text, out _parameters.SystemPar.Latitude);
            if (comboBoxLatitude.Text.ToUpper()[0] == 'S') {
                _parameters.SystemPar.Latitude = -_parameters.SystemPar.Latitude;
            }
            // Longitude
            isOK = double.TryParse(textBoxLongitude.Text, out _parameters.SystemPar.Longitude);
            if (comboBoxLongitude.Text.ToUpper()[0] == 'W') {
                _parameters.SystemPar.Longitude = -_parameters.SystemPar.Longitude;
            }
            // UT correction
            double hoursToUT;
            isOK = double.TryParse(textBoxUTCorrection.Text, out hoursToUT);
            _parameters.SystemPar.MinutesToUT = (int)Math.Floor(hoursToUT * 60.0 + 0.5);
            // Altitude
            isOK = double.TryParse(textBoxAltitude.Text, out value);
            _parameters.SystemPar.Altitude = (int)Math.Floor(value + 0.5);
            // SiteID
            isOK = int.TryParse(textBoxSiteID.Text, out _parameters.SystemPar.RadarPar.RadarID);
            // TxFreq
            isOK = double.TryParse(textBoxTxFreq.Text, out _parameters.SystemPar.RadarPar.TxFreqMHz);
            // Antenna spacing
            isOK = double.TryParse(textBoxAntSpacingM.Text, out _parameters.SystemPar.RadarPar.AntSpacingM);
            // AsubH
            // _parameters.SystemPar.RadarPar.ASubH = 2.635;  // old value small antenna
            _parameters.SystemPar.RadarPar.ASubH = 1.68;    // improved value large antenna
            //_parameters.SystemPar.RadarPar.ASubH = 3.3;     // improved value small antenna
            isOK = double.TryParse(textBoxAsubH.Text, out _parameters.SystemPar.RadarPar.ASubH);
            // MaxDutyCycle
            double maxDutyPercent;
            isOK = double.TryParse(textBoxMaxDutyCycle.Text, out maxDutyPercent);
            _parameters.SystemPar.RadarPar.MaxTxDutyCycle = maxDutyPercent / 100.0;
            // MaxTxLength
            isOK = double.TryParse(textBoxMaxTx.Text, out value);
            _parameters.SystemPar.RadarPar.MaxTxLengthUsec = (int)Math.Floor(value + 0.5);
            // minIPP
            isOK = double.TryParse(textBoxMinIpp.Text, out _parameters.SystemPar.RadarPar.MinIppUsec);

            // PBX parameters
            isOK = int.TryParse(textBoxPbxClock.Text, out _parameters.SystemPar.RadarPar.PBConstants.PBClock);
            if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.PulsedTx) {
                // these values are fixed in FMCW
                // except post-blank which is set in FMCW page
                isOK = int.TryParse(textBoxPostBlank.Text, out _parameters.SystemPar.RadarPar.PBConstants.PBPostBlank);
                isOK = int.TryParse(textBoxPostTR.Text, out _parameters.SystemPar.RadarPar.PBConstants.PBPostTR);
                isOK = int.TryParse(textBoxPreBlank.Text, out _parameters.SystemPar.RadarPar.PBConstants.PBPreBlank);
                isOK = int.TryParse(textBoxPreTR.Text, out _parameters.SystemPar.RadarPar.PBConstants.PBPreTR);
                isOK = int.TryParse(textBoxSync.Text, out _parameters.SystemPar.RadarPar.PBConstants.PBSynch);
            }

            // USB Power meter
            _parameters.SystemPar.RadarPar.PowMeterPar.Enabled = checkBoxPowerMeterEnable.Checked;
            isOK = double.TryParse(textBoxPMOffsetdB.Text, out _parameters.SystemPar.RadarPar.PowMeterPar.OffsetDB);
            isOK = int.TryParse(textBoxPMIntervalSec.Text, out _parameters.SystemPar.RadarPar.PowMeterPar.WriteIntervalSec);

            // Directions
            isOK = int.TryParse(numericUpDownDirections.Text, out _parameters.ArrayDim.MAXDIRECTIONS);
            for (int i = 0; i < _parameters.ArrayDim.MAXDIRECTIONS; i++) {
                _parameters.SystemPar.RadarPar.BeamDirections[i].Label = (string)dataGridViewDirections.Rows[i].Cells[0].Value;
                isOK = double.TryParse((string)dataGridViewDirections.Rows[i].Cells[1].Value, out _parameters.SystemPar.RadarPar.BeamDirections[i].Azimuth);
                isOK = double.TryParse((string)dataGridViewDirections.Rows[i].Cells[2].Value, out _parameters.SystemPar.RadarPar.BeamDirections[i].Elevation);
                isOK = int.TryParse((string)dataGridViewDirections.Rows[i].Cells[3].Value, out _parameters.SystemPar.RadarPar.BeamDirections[i].SwitchCode);
            }
            // RxBw
            if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.PulsedTx) {
                // these values are fixed in FMCW or set on FMCW page
                for (int i = 0; i < _parameters.ArrayDim.MAXBW; i++) {
                    isOK = double.TryParse((string)dataGridViewRxBw.Rows[i].Cells[0].Value, out value);
                    _parameters.SystemPar.RadarPar.RxBw[i].BwPwNs = (int)Math.Floor(value + 0.5);
                    isOK = double.TryParse((string)dataGridViewRxBw.Rows[i].Cells[1].Value, out value);
                    _parameters.SystemPar.RadarPar.RxBw[i].BwDelayNs = (int)Math.Floor(value + 0.5);
                }
            }

            if (radioButtonPulsedType.Checked) {
                _parameters.SystemPar.RadarPar.RadarType = PopParameters.TypeOfRadar.PulsedTx;
            }
            else if (radioButtonFMCWSAType.Checked) {
                _parameters.SystemPar.RadarPar.RadarType = PopParameters.TypeOfRadar.FmCwSA;
            }
            else {
                _parameters.SystemPar.RadarPar.RadarType = PopParameters.TypeOfRadar.FmCwDop;
            }

            // multiple receivers
            int nrx = (int)numericUpDownNRx.Value;
            _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx = nrx;
            int nrxDim = _parameters.SystemPar.RadarPar.ProcPar.RxID.Length;
            if (nrxDim < nrx) {
                throw new ApplicationException("NRxDim less than NRx in SaveSystemPage.");
            }
            if (nrxDim > 0) {
                _parameters.SystemPar.RadarPar.ProcPar.RxID[0].RxIDName = comboBoxRx1.Text;
                _parameters.SystemPar.RadarPar.ProcPar.RxID[0].iSampleOrder = RxSampleOrder.IndexOf(comboBoxRx1.Text);
                int i = _parameters.SystemPar.RadarPar.ProcPar.RxID[0].iRx;
                if (i != 0) {
                    // debug check: make sure deserialize reads array elements in proper order
                    throw new ApplicationException("Parameter file RxID first index i not 0 in SaveSystemPage.");
                }
            }
            if (nrxDim > 1) {
                _parameters.SystemPar.RadarPar.ProcPar.RxID[1].RxIDName = comboBoxRx2.Text;
                _parameters.SystemPar.RadarPar.ProcPar.RxID[1].iSampleOrder = RxSampleOrder.IndexOf(comboBoxRx2.Text);
            }
            if (nrxDim > 2) {
                _parameters.SystemPar.RadarPar.ProcPar.RxID[2].RxIDName = comboBoxRx3.Text;
                _parameters.SystemPar.RadarPar.ProcPar.RxID[2].iSampleOrder = RxSampleOrder.IndexOf(comboBoxRx3.Text);
            }

            // because sysdelay affects FMCW offset freq, we need to be sure to update that page.
            // Removed in 3.13 because sysdelay can no longer be set from this page if in FMCW mode
            //FillFmCwPage();
            //SaveFmCwPage();

            if ((_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA ) &&
                (_parameters.SystemPar.RadarPar.AntSpacingM <= 0.0) && showErrorMsg) {
                    DialogResult rr = MessageBox.Show("Antenna Spacing NOT set\n(Typical value = 0.946)", "System Page Setup Error");
                    textBoxAntSpacingM.Text = "0.946";
                    _parameters.SystemPar.RadarPar.AntSpacingM = 0.946;
                    return false;
            }

            // check validity of RxID's
            if (!RxIDsOK() && showErrorMsg) {
                DialogResult rr = MessageBox.Show("Multiple Rx ID's do not match attached hardware\non System Page." +
                                                "\nClick OK to leave anyway.", "System Page Setup Error",
                                MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
                if (rr == System.Windows.Forms.DialogResult.OK) {
                    return true;
                }
                else {
                    return false;
                }
            }

            return true;

        }

        private bool RxIDsOK() {
            if (_parameters.SystemPar.RadarPar.ProcPar.NumberOfRx == 1) {
                return true;
            }
            if (_parameters.Debug.NoHardware) {
                return true;
            }
           int nrxDim = _parameters.SystemPar.RadarPar.ProcPar.RxID.Length;
            int nrx = _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
            if (nrxDim < nrx) {
                return false;
            }
            if (comboBoxRx1.Items.Count < 1) {
                return false;
            }
            if (nrx > 0) {
                string name = comboBoxRx1.Text;
                if (!RxIDNameIsValid(name)) {
                    return false;
                }
            }
            if (nrx > 1) {
                string name = comboBoxRx2.Text;
                if (!RxIDNameIsValid(name)) {
                    return false;
                }
                if (comboBoxRx2.Text == comboBoxRx1.Text) {
                    return false;
                }
            }
            if (nrx > 2) {
                string name = comboBoxRx3.Text;
                if (!RxIDNameIsValid(name)) {
                    return false;
                }
                if ((comboBoxRx3.Text == comboBoxRx1.Text) || (comboBoxRx3.Text == comboBoxRx2.Text)) {
                    return false;
                }
            }
            return true;
        }

        private bool RxIDNameIsValid(string name) {
            int idx = comboBoxRx1.Items.IndexOf(name);
            if (idx < 0) {
                return false;
            }
            bool match = false;
            foreach (PopParameters.RxIDParameters rxid in _parameters.SystemPar.RadarPar.ProcPar.RxID) {
                if (name == rxid.RxIDName) {
                    match = true;
                    break;
                }
            }
            return match;
        }

        private double _sweepBeyondSamplesUs;


        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void FillFmCwPage() {

            checkBoxFmCwPostBlankAuto.Checked = _parameters.SystemPar.RadarPar.FmCwParSet[0].PostBlankIsAuto;

            _sweepBeyondSamplesUs = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepBeyondSamplesUs;
            if (_sweepBeyondSamplesUs <= 0.0) {
                _sweepBeyondSamplesUs = 10.0;
            }

            radioButtonFreqOffset.Checked = _parameters.SystemPar.RadarPar.FmCwParSet[0].IsRadioButtonFreqOffset;
            radioButtonGateOffset.Checked = !_parameters.SystemPar.RadarPar.FmCwParSet[0].IsRadioButtonFreqOffset;
           // radioButtonFreqOffset_CheckedChanged(null, null);

            // TODO: (DONE) if gateoffset==0 and sweepOffset>0, calculate gate offset from sweep offset
            //      because assuming 0 gate offset is actually missing gate offset from earlier rev (<3.13)
            if (radioButtonFreqOffset.Checked) {
                textBoxFmCwOffsetHz.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepOffsetHz.ToString();
            }
            else {
                textBoxFmCwOffsetGate.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].GateOffset.ToString("F2");
                // POPREV: 3.25.4 filling TxSweepOffset when gate offset not in par file 20130716
                if ((_parameters.SystemPar.RadarPar.FmCwParSet[0].GateOffset == 0) &&
                    (_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepOffsetHz > 0.0)) {
                        double offsetHz = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepOffsetHz;
                        int npts = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
                        double spacing = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleSpacingNs;
                        double sysDelayNs = _parameters.SystemPar.RadarPar.RxBw[0].BwDelayNs;
                        double sweepRateHzUsec = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepRateHzUSec;
                        textBoxFmCwOffsetHz.Text = offsetHz.ToString();
                        double gateOffset = AD9959EvalBd.OffsetGate(offsetHz, npts, spacing, sysDelayNs, sweepRateHzUsec);
                        textBoxFmCwOffsetGate.Text = gateOffset.ToString("f2");
                }
            }
            
            // fill in attached DAQ devices
            buttonCheckDAQ_Click(null, null);

            //labelFmCwSysDelayNs.Text = _parameters.SystemPar.RadarPar.RxBw[0].BwDelayNs.ToString();
            textBoxFmCwSysDelayNs.Text = _parameters.SystemPar.RadarPar.RxBw[0].BwDelayNs.ToString();

            _savedPostBlankNs = _parameters.SystemPar.RadarPar.PBConstants.PBPostBlank;
            double pbUsec = _savedPostBlankNs / 1000.0;
            textBoxFmCwPostBlankUs.Text = pbUsec.ToString();
            if (checkBoxFmCwPostBlankAuto.Checked) {
                textBoxFmCwPostBlankUs.Enabled = false;
                textBoxFmCwPostBlankUs.BackColor = Color.OldLace;
            }
            else {
                textBoxFmCwPostBlankUs.Enabled = true;
            }

			// AD9959 group
			checkBoxEnableDDS.Checked = _parameters.SystemPar.RadarPar.FmCwParSet[0].AD9959Enabled;
			textBoxDDSRefClockMHz.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDSRefClockMHz.ToString();
			int index = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDSMultiplier - 3;
			if (index < 0) {
				index = 0;
			}
			comboBoxDDSMultiplier.SelectedIndex = index;

            textBoxDDS3FreqHz.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS3FreqStartHz.ToString();
            textBoxDDS4FreqHz.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS4FreqStartHz.ToString();
            numericUpDownDDS3Phase.Value = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS3PhaseDeg;
            numericUpDownDDS4Phase.Value = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS4PhaseDeg;

            // TxSweep group
            textBoxFmCwIppUSec.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].IppMicroSec.ToString();
			textBoxFmCwSweepCenterMHz.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepCenterFreqMHz.ToString();
			double rampRate = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepRateHzUSec;
			
            textBoxFmCwSweepRate.Text = rampRate.ToString("f3");
			textBoxFmCwTimeStepClocks.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepTimeStepClocks.ToString();
            //textBoxFmCwTimeStepUSec.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepTimeIntervalUSec.ToString();

            // Sample parameters group
            textBoxSweepNPts.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts.ToString();
            textBoxSweepNSpec.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNSpec.ToString();
            textBoxSweepSampleDelay.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDelayNs.ToString();
            textBoxSweepSpacing.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleSpacingNs.ToString();
            if (_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleOverlap) {
                //checkBoxSampleOverlap.Checked = true;
            }
            else {
                //checkBoxSampleOverlap.Checked = false;
            }
            comboBoxSampleWindow.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleWindow.ToString();
			if (_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter) {
				checkBoxDcFilSamples.Checked = true;
			}
			else {
				checkBoxDcFilSamples.Checked = false;
			}
            if (_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter2) {
                checkBoxDcFilSamples2.Checked = true;
            }
            else {
                checkBoxDcFilSamples2.Checked = false;
            }

            // CrossCorrelation group
            textBoxXCMaxLag.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag.ToString();
            textBoxXCLineFitPts.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLineFitPts.ToString();
            textBoxXCFilterFraction.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrFilterFraction.ToString("F2");
            checkBoxXCFFT.Checked = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrUseFFT;
            textBoxPolyFitOrder.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrPolyFitOrder.ToString();
            textBoxLagsToFit.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToCurveFit.ToString();
            textBoxLagsToInterp.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToInterpolate.ToString();
            checkBoxXCorrAdjustBase.Checked = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrAdjustBase;

            textBoxXCNptMult.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrNptsMultiplier.ToString();

            // Doppler parameters group
            textBoxDopNPts.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts.ToString();
            textBoxDopNSpec.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec.ToString();
            if (_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerOverlap) {
                checkBoxDopOverlap.Checked = true;
            }
            else {
                checkBoxDopOverlap.Checked = false;
            }
            comboBoxDopWindow.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow.ToString();
            //textBoxRangeOffset.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].RangeOffsetM.ToString();
            int heightsCalculated = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts / 2 + 1;
            labelHtsCalculated.Text = heightsCalculated.ToString();
			if (_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerDcFilter) {
				checkBoxDcFilDoppler.Checked = true;
			}
			else {
				checkBoxDcFilDoppler.Checked = false;
			}

            /*
			if (_parameters.SystemPar.RadarPar.FmCwParSet[0].InputSampleUnitsIsRaw) {
				radioButtonRawSamples.Checked = true;
			}
			else {
				radioButtonVoltSamples.Checked = true;
			}
            */

            labelNRx.Text = _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx.ToString();

            checkBoxSelectHts.Checked = _parameters.SystemPar.RadarPar.FmCwParSet[0].SelectGatesToKeep;
            numericUpDownGateFirst.Value = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst;
            numericUpDownGateLast.Value = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast;
            // if not selecting all existing gates, check the user select hts box.
            if ((numericUpDownGateFirst.Value == 0) & (numericUpDownGateLast.Value == heightsCalculated - 1)) {
                checkBoxSelectHts.Checked = false;
            }
            else {
                checkBoxSelectHts.Checked = true;
            }
            checkBoxSelectHts_CheckedChanged(null, null);

            ////

            comboBoxFilterFile.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile;

            if (_parameters.SystemPar.RadarPar.FmCwParSet[0].ApplyFilterCorrection) {
				checkBoxApplyFilterCorr.Checked = true;
			}
			else {
				checkBoxApplyFilterCorr.Checked = false;
			}

            // set initially to unchecked so that we call check_changed method
            radioButtonFreqResp.Checked = false;
            radioButtonCoeff.Checked = false;
            if (_parameters.SystemPar.RadarPar.FmCwParSet[0].UseFilterCoeffs) {
                radioButtonCoeff.Checked = true;
            }
            else if (_parameters.SystemPar.RadarPar.FmCwParSet[0].UseFreqResp) {
                radioButtonFreqResp.Checked = true;
            }
            else {
                radioButtonFreqResp.Checked = true;
            }

            ////

			RecalculateRangeRes();
			RecalculateLastSample();
        }


        private void buttonCheckDAQ_Click(object sender, EventArgs e) {
            //int nDaq = 0;
            //_communicator.SendCommand(PopCommands.Stop);
            string[] deviceNames = null;
            try {
                //_nDaq = daqBoard.NumDevices;
                if (Communicator != null) {
                    if ((_parameters == null) || (_parameters.Debug.NoHardware)) {
                        _nDaq = 0;
                        deviceNames = new string[1];
                        deviceNames[0] = "";
                    }
                    else {
                        Communicator.RequestNumDaqDevices(out _nDaq, out deviceNames);
                    }
                }
                else {
                    _nDaq = 0;
                    deviceNames = new string[1];
                    deviceNames[0] = "";
                }
            }
            catch (Exception exc) {
                //MessageBox.Show(exc.Message);
                _nDaq = 0;
            }
            
            /*
            try {
                DAQDevice _daqBoard = DAQDevice.GetAttachedDAQ();

                //DaqBoard3000USB daqBoard = new DaqBoard3000USB();
                _nDaq = _daqBoard.NumDevices;
                _daqBoard.Close();
                _daqBoard.Dispose();
                _daqBoard = null;
            }
            catch (Exception exc) {
                //MessageBox.Show(exc.Message);
                _nDaq = 0;
            }
            */
            
            labelNumDaq.Text = _nDaq.ToString();

        }

        ///////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Method to validate that string in TextBox evalutates as an integer;
        /// This method is called when focus is lost,
        ///     if CausesValidation property of textbox is set
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void textBox_ValidatingInteger(object sender, CancelEventArgs e) {
            TextBox tb = (TextBox)sender;
            int value = Int32.MaxValue;
            bool isOK = Int32.TryParse(tb.Text, out value);
            if (!isOK) {
                MessageBox.Show(string.Format("Non-integer field in {0}", tb.Name));
                e.Cancel = true;
            }
            else {
                e.Cancel = false;
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private bool SaveFmCwPage(bool showErrorMsg) {
            bool isOK;
			bool isFmCw;
            bool pageOK = true;
            List<bool> isAOK = new List<bool>();
            if (isAOK.Contains(false)) {
                
            }

			if ((_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwDop) ||
                (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA)) {
				isFmCw = true;
			}
			else {
				isFmCw = false;
			}

            _parameters.SystemPar.RadarPar.FmCwParSet[0].PostBlankIsAuto = checkBoxFmCwPostBlankAuto.Checked;

            _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepBeyondSamplesUs = _sweepBeyondSamplesUs;

            _parameters.SystemPar.RadarPar.FmCwParSet[0].IsRadioButtonFreqOffset = radioButtonFreqOffset.Checked;

            // if setting sysDelay on this page, save it
            double value;
            isOK = double.TryParse(textBoxFmCwSysDelayNs.Text, out value);
            _parameters.SystemPar.RadarPar.RxBw[0].BwDelayNs = (int)Math.Floor(value + 0.5);
            // and update value displayed on system page
            FillSystemPageSysDelay0();

            // save Postblank (text display is in Usec, store in nsec)
            double.TryParse(textBoxFmCwPostBlankUs.Text, out value);
            value = value * 1000.0;
            _parameters.SystemPar.RadarPar.PBConstants.PBPostBlank = (int)Math.Floor(value + 0.5);
            FillSystemPagePostBlank();

			// AD9959 Board group
			_parameters.SystemPar.RadarPar.FmCwParSet[0].AD9959Enabled = checkBoxEnableDDS.Checked;
			isOK = double.TryParse(textBoxDDSRefClockMHz.Text,
								out _parameters.SystemPar.RadarPar.FmCwParSet[0].DDSRefClockMHz);
			int index = comboBoxDDSMultiplier.SelectedIndex;
			int multiplier = index + 3;
			if (multiplier < 4) {
				multiplier = 1;
			}
			_parameters.SystemPar.RadarPar.FmCwParSet[0].DDSMultiplier = multiplier;

            _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS3FreqStartHz = 0.0;
            _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS4FreqStartHz = 0.0;
            isOK = double.TryParse(textBoxDDS3FreqHz.Text,
                                out _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS3FreqStartHz);
            isOK = double.TryParse(textBoxDDS4FreqHz.Text,
                                out _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS4FreqStartHz);
            _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS3FreqEndHz = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS3FreqStartHz;
            _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS4FreqEndHz = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS4FreqStartHz;
            _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS3PhaseDeg = (int)numericUpDownDDS3Phase.Value;
            _parameters.SystemPar.RadarPar.FmCwParSet[0].DDS4PhaseDeg = (int)numericUpDownDDS4Phase.Value;

            // TxSweep group
            isOK = double.TryParse(textBoxFmCwIppUSec.Text.ToString(),
                                out _parameters.SystemPar.RadarPar.FmCwParSet[0].IppMicroSec);
			isOK = double.TryParse(textBoxFmCwSweepCenterMHz.Text,
								out _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepCenterFreqMHz);
            isOK = double.TryParse(labelDDSSweepRate.Text.ToString(),
                                out _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepRateHzUSec);
            isOK = double.TryParse(textBoxFmCwOffsetHz.Text.ToString(),
                                out _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepOffsetHz);
            isOK = double.TryParse(textBoxFmCwOffsetGate.Text.ToString(),
                                out _parameters.SystemPar.RadarPar.FmCwParSet[0].GateOffset);
            isOK = int.TryParse(textBoxFmCwTimeStepClocks.Text,
								out _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepTimeStepClocks);

            // Sample parameters group
            isOK = int.TryParse(textBoxSweepNPts.Text.ToString(),
                                out _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts);
            isOK = int.TryParse(textBoxSweepNSpec.Text.ToString(),
                                out _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNSpec);
            isOK = int.TryParse(textBoxSweepSampleDelay.Text.ToString(),
                                out _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDelayNs);
            isOK = int.TryParse(textBoxSweepSpacing.Text.ToString(),
                                out _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleSpacingNs);
            _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleOverlap = false;
            /*
            if (checkBoxSampleOverlap.Checked) {
                _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleOverlap = true;
            }
            else {
                _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleOverlap = false;
            }
             * */
            foreach (string name in Enum.GetNames(typeof(PopParameters.WindowType))) {
                if (comboBoxSampleWindow.Text == name) {
                    _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleWindow =
                            (PopParameters.WindowType)Enum.Parse(typeof(PopParameters.WindowType), name);
                    break;
                }
                // should never get here:
                _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleWindow = PopParameters.WindowType.Rectangular;
            }

            // CrossCorrelation group
            isOK = int.TryParse(textBoxXCMaxLag.Text.ToString(),
                    out _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag);
            isOK = int.TryParse(textBoxPolyFitOrder.Text.ToString(),
                    out _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrPolyFitOrder);
            isOK = int.TryParse(textBoxXCLineFitPts.Text.ToString(),
                    out _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLineFitPts);
            isOK = double.TryParse(textBoxXCFilterFraction.Text.ToString(),
                    out _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrFilterFraction);
            _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrUseFFT = checkBoxXCFFT.Checked;
            _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrAdjustBase = checkBoxXCorrAdjustBase.Checked;

            int lagsToFit;
            isOK = int.TryParse(textBoxLagsToFit.Text.ToString(),
                    out lagsToFit);
            // lags to fit must be odd; zero will mean do all lags
            if (lagsToFit < 0) {
                lagsToFit = 0;
            }
            if (lagsToFit > 0 && (lagsToFit % 2) == 0) {
                lagsToFit++;
            }
            int totalLags = 1 + 2 * _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrMaxLag;
            lagsToFit = Math.Min(totalLags, lagsToFit);
            _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToCurveFit = lagsToFit;

            int lagsToInterp;
            isOK = int.TryParse(textBoxLagsToInterp.Text.ToString(),
                    out lagsToInterp);
            // lags to interpolate must be odd or zero;
            if (lagsToInterp < 0) {
                lagsToInterp = 0;
            }
            if (lagsToInterp > 0 && (lagsToInterp % 2) == 0) {
                lagsToInterp++;
            }
            _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrLagsToInterpolate = lagsToInterp;

            //isOK = int.TryParse(textBoxDopNPts.Text.ToString(),
            //        out _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts);
            isOK = int.TryParse(textBoxXCNptMult.Text, out _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrNptsMultiplier);
            if (_parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrNptsMultiplier < 1) {
                _parameters.SystemPar.RadarPar.FmCwParSet[0].XCorrNptsMultiplier = 1;
            }

            // Doppler parameters group
            isOK = int.TryParse(textBoxDopNPts.Text.ToString(),
                    out _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts);
            isOK = int.TryParse(textBoxDopNSpec.Text.ToString(),
                    out _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec);
            _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerOverlap = checkBoxDopOverlap.Checked;
            foreach (string name in Enum.GetNames(typeof(PopParameters.WindowType))) {
                if (comboBoxDopWindow.Text == name) {
                    _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow =
                            (PopParameters.WindowType)Enum.Parse(typeof(PopParameters.WindowType), name);
                    break;
                }
                // should never get here:
                _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow = PopParameters.WindowType.Rectangular;
            }


			if (checkBoxDcFilDoppler.Checked) {
				_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerDcFilter = true;
			}
			else {
				_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerDcFilter = false;
			}
			if (checkBoxDcFilSamples.Checked) {
				_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter = true;
			}
			else {
				_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter = false;
			}
            if (checkBoxDcFilSamples2.Checked) {
                _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter2 = true;
            }
            else {
                _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDcFilter2 = false;
            }
			
            /*
			isOK = int.TryParse(textBoxRangeOffset.Text.ToString(),
                    out _parameters.SystemPar.RadarPar.FmCwParSet[0].RangeOffsetM);
             * */

            int firstGate, lastGate;
            CheckSelectedGateRange();
            _parameters.SystemPar.RadarPar.FmCwParSet[0].SelectGatesToKeep = checkBoxSelectHts.Checked;
            if (!_parameters.ReplayPar.Enabled) {
                firstGate = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst =
                        (int)numericUpDownGateFirst.Value;
                lastGate = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast =
                        (int)numericUpDownGateLast.Value;

            }
            else {
                firstGate = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst =
                        (int)numericUpDownGateFirst.Value;
                lastGate = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast =
                        (int)numericUpDownGateLast.Value;
            }

			// Baseband filter group
			if (checkBoxApplyFilterCorr.Checked) {
				_parameters.SystemPar.RadarPar.FmCwParSet[0].ApplyFilterCorrection = true;
			}
			else {
				_parameters.SystemPar.RadarPar.FmCwParSet[0].ApplyFilterCorrection = false;
			}
            _parameters.SystemPar.RadarPar.FmCwParSet[0].UseFreqResp = radioButtonFreqResp.Checked;
            _parameters.SystemPar.RadarPar.FmCwParSet[0].UseFilterCoeffs = radioButtonCoeff.Checked;
			_parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile = comboBoxFilterFile.Text;

			// Input samples
            // POPREV: rev 3.11, this parameter is no longer user changeable; units == raw
            _parameters.SystemPar.RadarPar.FmCwParSet[0].InputSampleUnitsIsRaw = true;
            /*
            if (radioButtonRawSamples.Checked) {
				_parameters.SystemPar.RadarPar.FmCwParSet[0].InputSampleUnitsIsRaw = true;
			}
			else {
				_parameters.SystemPar.RadarPar.FmCwParSet[0].InputSampleUnitsIsRaw = false;
			}
            */

			_parameters.SystemPar.RadarPar.FmCwParSet[0].InputSampleVoltMax = 10.0;

			//
			// if the radar is in FMCW mode, then
			//	transfer some FMCW parameters to standard radar parameter set
			//
			if (isFmCw) {
				if (_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerWindow == PopParameters.WindowType.Rectangular) {
					_parameters.SystemPar.RadarPar.ProcPar.IsWindowing = false;
				}
				else {
					_parameters.SystemPar.RadarPar.ProcPar.IsWindowing = true;
				}
				_parameters.SystemPar.RadarPar.BeamParSet[0].NHts = lastGate - firstGate + 1;
				labelNHts.Text = _parameters.SystemPar.RadarPar.BeamParSet[0].NHts.ToString();
				_parameters.SystemPar.RadarPar.BeamParSet[0].IppMicroSec =
						_parameters.SystemPar.RadarPar.FmCwParSet[0].IppMicroSec;
				_parameters.SystemPar.RadarPar.BeamParSet[0].NPts =
						_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts;
				_parameters.SystemPar.RadarPar.BeamParSet[0].NSpec =
						_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec;

				// save other, fixed parameters:
				//_parameters.SystemPar.RadarPar.ProcPar.NumberOfRx = 1;
				_parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[0] = 0;	// rass is off
				for (int i = 1; i < 6; i++) {
					_parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[i] = -1;
				}
                // POPREV 4.15 start saving timestamp millisec value in first extra instrument reading (type = 0x5453)
                int numInstruments = 1;
                _parameters.SystemPar.RadarPar.NumOtherInstruments = numInstruments;
                _parameters.SystemPar.RadarPar.OtherInstrumentCodes = null;
                _parameters.SystemPar.RadarPar.OtherInstrumentCodes = new int[numInstruments];
                _parameters.SystemPar.RadarPar.OtherInstrumentCodes[0] = 0x5453;

                _parameters.SystemPar.RadarPar.ProcPar.Dop0 = 1;
				_parameters.SystemPar.RadarPar.ProcPar.Dop1 = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts;
				_parameters.SystemPar.RadarPar.ProcPar.Dop2 = 1;
				_parameters.SystemPar.RadarPar.ProcPar.Dop3 = 0;
				_parameters.SystemPar.RadarPar.ProcPar.IsIcraAvg = false;

				_parameters.SystemPar.RadarPar.ProcPar.IsDcFiltering = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerDcFilter;
				_parameters.SystemPar.RadarPar.BeamParSet[0].NCI = 1;
				_parameters.SystemPar.RadarPar.BeamParSet[0].NCode = 0;

				// compute effective pulse parameters
				// range spacing = 1/(SweepRate*Nsamples*SampleSpacing)
				double sweepRate = 1.0e6 * _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepRateHzUSec;
				//double sweepRate = 1.0e6 * sweepFreq / sweepTime;
				int nSamples = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
				double spacing = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleSpacingNs * 1.0e-9;
				int rangeSpacingNs = (int)(1.0e9 / (sweepRate * nSamples * spacing) + 0.5);
				_parameters.SystemPar.RadarPar.BeamParSet[0].PulseWidthNs = rangeSpacingNs;
				_parameters.SystemPar.RadarPar.BeamParSet[0].SpacingNs = rangeSpacingNs;

				_parameters.SystemPar.RadarPar.BeamParSet[0].SampleDelayNs = _virtualRangeDelayNs;

				// we may have modified parameters displayed on dwell page,
				//	so refill it.
				FillDwellPage();

                if (!_parameters.Debug.NoHardware) {

                    if (!_parameters.Debug.NoPbx) {

                        // if using PulseBlaster card,
                        //  DDS sync clock is PB clock and must be 100 Mhz
                        if (_parameters.SystemPar.RadarPar.FmCwParSet[0].AD9959Enabled) {
                            // but for now let's assume if DDS not enabled, we know what we are doing
                            double ddsClock = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDSRefClockMHz;
                            int ddsMult = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDSMultiplier;
                            double ddsSync = ddsClock * ddsMult / 4.0;
                            if (ddsSync != 100.0) {
                                if (showErrorMsg && UsingPulseBlaster()) {
                                    MessageBox.Show("DDS Sync clock must be 100 MHz for PulseBlaster.", "FMCW Page Error", MessageBoxButtons.OK);
                                    pageOK = false;
                                }
                            }
                            if (!_parameters.SystemPar.RadarPar.FmCwParSet[0].AD9959Enabled) {
                                if (showErrorMsg && UsingPulseBlaster()) {
                                    MessageBox.Show("Must ENABLE DDS with 100 MHz Sync clock for PulseBlaster.", "FMCW Page Error", MessageBoxButtons.OK);
                                    //pageOK = false;
                                }
                            }
                        }
                        else {
                            // message only
                            MessageBox.Show("Must ENABLE DDS with 100 MHz Sync clock\n for PulseBlaster without internal clock.", "FMCW Page Error", MessageBoxButtons.OK);
                        }
                    }

                }

                if (_parameters.SystemPar.RadarPar.FmCwParSet[0].ApplyFilterCorrection) {
                    string appPath = Application.StartupPath;
                    string fullPath = Path.Combine(appPath, comboBoxFilterFile.Text);
                    if (!File.Exists(fullPath) && showErrorMsg) {
                        DialogResult rr = MessageBox.Show("Filter file does not exist.\n\n 'OK' = Continue anyway.\n'Cancel' = Stop and fix",
                            "FMCW Page Error",
                            MessageBoxButtons.OKCancel);
                        if (rr == DialogResult.OK) {
                            pageOK = true;
                        }
                        else {
                            pageOK = false;
                        }
                    }
                }

                if (!_parameters.Debug.NoHardware) {
                    int nrx = _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx;
                    buttonCheckDAQ_Click(null, null);
                    if ((!checkBoxReplay.Checked) && (_nDaq != nrx) && showErrorMsg) {
                        DialogResult rr = MessageBox.Show("NRX must match number of DAQ devices attached.\n\tContinue (OK)?\n\tFix (Cancel)?",
                                        "FMCW Page Error", MessageBoxButtons.OKCancel);
                        if (rr == System.Windows.Forms.DialogResult.Cancel) {
                            pageOK = false;
                        }
                    }

                }

                int npts = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts;
                if (!DACarter.Utilities.Tools.IsPowerOf2(npts)) {
                    if (showErrorMsg) {
                        MessageBoxEx.Show("Number of Samples is NOT power of 2", 1000);
                    }
                }

                npts = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts;
                if (!DACarter.Utilities.Tools.IsPowerOf2(npts)) {
                    if (showErrorMsg) {
                        MessageBoxEx.Show("Number of Doppler points is NOT power of 2", 1000);
                    }
                }

            }  // end if(isFmCw)

            return pageOK;
        }

        // TODO: do not create PulseBlaster here;
        //  instead query main program
        private bool UsingPulseBlaster() {
            bool isPBlaster = false;
            if (_pulseBox == null) {
                try {
                    _pulseBox = PulseGeneratorDevice.GetNewPulseGenDevice();
                }
                catch {
                    _pulseBox = null;
                }
            }
            if (_pulseBox != null) {
                if (_pulseBox is PulseBlaster) {
                    isPBlaster = true;
                }
                _pulseBox.Close();
                _pulseBox = null;
            }
            return isPBlaster;
        }

		private void FillOutputPage() {
			if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 0) {
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileWriteEnabled) {
					checkBoxEnableDiskWrite_1.Checked = true;
				}
				else {
					checkBoxEnableDiskWrite_1.Checked = false;
				}

                if ((_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 2) &&
                    (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[2].IncludeXCorr)) {
                    //checkBoxSpectra_1.Checked = false;
                    checkBoxXCorr_1.Checked = true;
                }
                else {
                    if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeSpectra) {
                        checkBoxSpectra_1.Checked = true;
                    }
                    else {
                        checkBoxXCorr_1.Checked = false;
                        checkBoxSpectra_1.Checked = false;
                    }
                }

                //checkBoxXCorr_1.Checked = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeXCorr;
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeMoments) {
					checkBoxMoments_1.Checked = true;
				}
				else {
					checkBoxMoments_1.Checked = false;
				}
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeSingleTS) {
					checkBox1TS_1.Checked = true;
				}
				else {
					checkBox1TS_1.Checked = false;
				}
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeFullTS) {
					checkBoxFullTS_1.Checked = true;
				}
				else {
					checkBoxFullTS_1.Checked = false;
				}
                if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteSingleTSTextFile) {
                    checkBox1TSText_1.Checked = true;
                }
                else {
                    checkBox1TSText_1.Checked = false;
                }
                if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteFullTSTextFile) {
                    checkBoxFullTSText_1.Checked = true;
                }
                else {
                    checkBoxFullTSText_1.Checked = false;
                }
                if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteRawTSTextFile) {
                    checkBoxRawTSText_1.Checked = true;
                }
                else {
                    checkBoxRawTSText_1.Checked = false;
                }
                if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteRawTSFile) {
					checkBoxRawTS_1.Checked = true;
				}
				else {
					checkBoxRawTS_1.Checked = false;
				}
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteHourlyFiles) {
					radioButtonHourFiles_1.Checked = true;
				}
				else {
					radioButtonDayFiles_1.Checked = true;
				}
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteModeOverwrite) {
					radioButtonOverwrite_1.Checked = true;
				}
				else {
					radioButtonAppend_1.Checked = true;
				}
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].UseLapxmFileName) {
					radioButtonLapxmName_1.Checked = true;
				}
				else {
					radioButtonPopName_1.Checked = true;
				}
				string suffix = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileNameSuffix;
				if ((suffix == null) || (suffix == String.Empty)) {
					suffix = "a";
					_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileNameSuffix = suffix;
				}
				textBoxSuffix_1.Text = suffix;
				string site = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileNameSite;
				if ((site == null) || (site == String.Empty)) {
					site = "xxx";
					_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileNameSite = site;
				}
				textBoxSite_1.Text = site;
				textBoxOutFolder_1.Text = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileFolder;
				DisplayFileName(0);
                textBoxLogFolder.Text = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].LogFileFolder;
			}
			if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 1) {
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileWriteEnabled) {
					checkBoxEnableDiskWrite_2.Checked = true;
				}
				else {
					checkBoxEnableDiskWrite_2.Checked = false;
				}

                if ((_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 2) &&
                    (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[4].IncludeXCorr)) {
                    //checkBoxSpectra_1.Checked = false;
                    checkBoxXCorr_2.Checked = true;
                }
                else {
                    if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeSpectra) {
                        checkBoxSpectra_2.Checked = true;
                    }
                    else {
                        checkBoxXCorr_2.Checked = false;
                        checkBoxSpectra_2.Checked = false;
                    }
                }


                //checkBoxXCorr_2.Checked = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeXCorr;
                if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeMoments) {
					checkBoxMoments_2.Checked = true;
				}
				else {
					checkBoxMoments_2.Checked = false;
				}
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeSingleTS) {
					checkBox1TS_2.Checked = true;
				}
				else {
					checkBox1TS_2.Checked = false;
				}
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeFullTS) {
					checkBoxFullTS_2.Checked = true;
				}
				else {
					checkBoxFullTS_2.Checked = false;
				}
                if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteSingleTSTextFile) {
                    checkBox1TSText_2.Checked = true;
                }
                else {
                    checkBox1TSText_2.Checked = false;
                }
                if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteFullTSTextFile) {
                    checkBoxFullTSText_2.Checked = true;
                }
                else {
                    checkBoxFullTSText_2.Checked = false;
                }
                if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteRawTSTextFile) {
                    checkBoxRawTSText_2.Checked = true;
                }
                else {
                    checkBoxRawTSText_2.Checked = false;
                }
                if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteRawTSFile) {
					checkBoxRawTS_2.Checked = true;
				}
				else {
					checkBoxRawTS_2.Checked = false;
				}
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteHourlyFiles) {
					radioButtonHourFiles_2.Checked = true;
				}
				else {
					radioButtonDayFiles_2.Checked = true;
				}
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteModeOverwrite) {
					radioButtonOverwrite_2.Checked = true;
				}
				else {
					radioButtonAppend_2.Checked = true;
				}
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].UseLapxmFileName) {
					radioButtonLapxmName_2.Checked = true;
				}
				else {
					radioButtonPopName_2.Checked = true;
				}
				string suffix = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileNameSuffix;
				if ((suffix == null) || (suffix == String.Empty)) {
					suffix = "a";
					_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileNameSuffix = suffix;
				}
				textBoxSuffix_2.Text = suffix;
				string site = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileNameSite;
				if ((site == null) || (site == String.Empty)) {
					site = "xxx";
					_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileNameSite = site;
				}
				textBoxSite_2.Text = site;
				textBoxOutFolder_2.Text = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileFolder;
				DisplayFileName(1);
			}
		}

		private bool SaveOutputPage(bool showErrorMsg) {
            bool pageOk = true;
            bool overwriteOn1 = false;
            bool overwriteOn2 = false;

            _parameters.SystemPar.RadarPar.ProcPar.IsWritingPopFile = false;
            if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 0) {
				// make sure at most 1 time series type is selected
				if (checkBox1TS_1.Checked) {
					checkBoxFullTS_1.Checked = false;
				}
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileWriteEnabled =
					checkBoxEnableDiskWrite_1.Checked;
                if (checkBoxEnableDiskWrite_1.Checked) {
                    _parameters.SystemPar.RadarPar.ProcPar.IsWritingPopFile = true;
                }
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeSpectra =
                    checkBoxSpectra_1.Checked;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeXCorr =
                    checkBoxXCorr_1.Checked;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeMoments =
					checkBoxMoments_1.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeSingleTS =
					checkBox1TS_1.Checked;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeFullTS =
                    checkBoxFullTS_1.Checked;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteSingleTSTextFile =
                    checkBox1TSText_1.Checked;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteFullTSTextFile =
                    checkBoxFullTSText_1.Checked;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteRawTSTextFile =
                    checkBoxRawTSText_1.Checked;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteRawTSFile =
					checkBoxRawTS_1.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteHourlyFiles =
					radioButtonHourFiles_1.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteModeOverwrite =
					radioButtonOverwrite_1.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].UseLapxmFileName =
					radioButtonLapxmName_1.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileNameSuffix =
					textBoxSuffix_1.Text;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileNameSite =
					textBoxSite_1.Text;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileFolder =
                    textBoxOutFolder_1.Text;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].LogFileFolder =
                    textBoxLogFolder.Text;

                // create additional PopFile elements for cross and auto correlations
                if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 2) {
                    if ((_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeXCorr)) {
                        bool writeEnabled = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileWriteEnabled;

                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[2] = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0];
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[3] = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0];

                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeSpectra = true;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeXCorr = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeACorr = false;

                        // set 2 does cross-corr
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[2].FileWriteEnabled = writeEnabled;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[2].IncludeSpectra = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[2].IncludeXCorr = true;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[2].IncludeACorr = false;
                        // set 3 does auto-corr
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[3].FileWriteEnabled = writeEnabled;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[3].IncludeSpectra = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[3].IncludeXCorr = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[3].IncludeACorr = true;
                    }
                    else {
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[2].FileWriteEnabled = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[2].IncludeSpectra = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[2].IncludeXCorr = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[2].IncludeACorr = false;

                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[3].FileWriteEnabled = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[3].IncludeSpectra = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[3].IncludeXCorr = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[3].IncludeACorr = false;
                    }
                }

            }
			if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 1) {
				// make sure at most 1 time series type is selected
				if (checkBox1TS_2.Checked) {
					checkBoxFullTS_2.Checked = false;
				}
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileWriteEnabled =
					checkBoxEnableDiskWrite_2.Checked;
                if (checkBoxEnableDiskWrite_2.Checked) {
                    _parameters.SystemPar.RadarPar.ProcPar.IsWritingPopFile = true;
                }
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeSpectra =
					checkBoxSpectra_2.Checked;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeXCorr =
                    checkBoxXCorr_2.Checked;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeMoments =
					checkBoxMoments_2.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeSingleTS =
					checkBox1TS_2.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeFullTS =
					checkBoxFullTS_2.Checked;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteSingleTSTextFile =
                    checkBox1TSText_2.Checked;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteFullTSTextFile =
                    checkBoxFullTSText_2.Checked;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteRawTSTextFile =
                    checkBoxRawTSText_2.Checked;
                _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteRawTSFile =
					checkBoxRawTS_2.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteHourlyFiles =
					radioButtonHourFiles_2.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteModeOverwrite =
					radioButtonOverwrite_2.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].UseLapxmFileName =
					radioButtonLapxmName_2.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileNameSuffix =
					textBoxSuffix_2.Text;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileNameSite =
					textBoxSite_2.Text;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileFolder =
					textBoxOutFolder_2.Text;

                // create additional PopFile elements for cross and auto correlations
                if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 2) {
                    if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeXCorr) {
                        bool writeEnabled = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileWriteEnabled;

                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[4] = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0];
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[5] = _parameters.SystemPar.RadarPar.ProcPar.PopFiles[0];

                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeSpectra = true;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeXCorr = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeACorr = false;

                        // set 4 does cross-corr
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[4].FileWriteEnabled = writeEnabled;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[4].IncludeSpectra = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[4].IncludeXCorr = true;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[4].IncludeACorr = false;
                        // set 5 does auto-corr
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[5].FileWriteEnabled = writeEnabled;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[5].IncludeSpectra = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[5].IncludeXCorr = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[5].IncludeACorr = true;
                    }
                    else {
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[4].FileWriteEnabled = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[4].IncludeSpectra = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[4].IncludeXCorr = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[4].IncludeACorr = false;

                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[5].FileWriteEnabled = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[5].IncludeSpectra = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[5].IncludeXCorr = false;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[5].IncludeACorr = false;
                    }
                }
            }

            if ((_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileWriteEnabled) && 
                (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteModeOverwrite == true)) {
                    overwriteOn1 = true;
            }
            else if ((_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 1) && 
                (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileWriteEnabled) &&
                (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteModeOverwrite == true)) {
                overwriteOn2 = true;
            }
            if ((overwriteOn1 || overwriteOn2) && showErrorMsg){
                DialogResult rr = MessageBox.Show("Disk record overwrite is ON!\n\tContinue (OK)?\n\tChange (Cancel)?",
                                "Output Page Error", MessageBoxButtons.OKCancel);
                if (rr == System.Windows.Forms.DialogResult.Cancel) {
                    if (overwriteOn1) {
              			radioButtonAppend_1.Checked = true;
         				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].WriteModeOverwrite =
        					radioButtonOverwrite_1.Checked;
                    }
                    if (overwriteOn2) {
                        radioButtonAppend_2.Checked = true;
                        _parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].WriteModeOverwrite =
                            radioButtonOverwrite_2.Checked;
                    }
                    pageOk = false;
                }
               
            }

            return pageOk;
		}

		private void FillMeltingLayerPage() {
			checkBoxMLEnable.Checked = _parameters.MeltingLayerPar.Enable;
			textBoxMLTimeInterval.Text = _parameters.MeltingLayerPar.CalculateEveryMinute.ToString();
			textBoxMLMinHeight.Text = _parameters.MeltingLayerPar.MinHeightM.ToString();
			textBoxMLMaxHeight.Text = _parameters.MeltingLayerPar.MaxHeightM.ToString();
			checkBoxMLUseDataRegion.Checked = _parameters.MeltingLayerPar.UseDataRegionOnly;
			textBoxMLMinDataHt.Text = _parameters.MeltingLayerPar.MinDataHtM.ToString();
			textBoxMLMaxDataHt.Text = _parameters.MeltingLayerPar.MaxDataHtM.ToString();
			textBoxMLMinSnrDvvPairs.Text = _parameters.MeltingLayerPar.MinSnrDvvPairs.ToString();
			textBoxMLMinSnrRain.Text = _parameters.MeltingLayerPar.MinSnrRain.ToString();
			textBoxMLDeltaSnrBb.Text = _parameters.MeltingLayerPar.DeltaSnrBb.ToString();
			textBoxMLDeltaDvvBb.Text = _parameters.MeltingLayerPar.DeltaDvvBb.ToString();
			textBoxMLMinSnrBb.Text = _parameters.MeltingLayerPar.MinSnrBb.ToString();
			textBoxMLDvvOnlyMaxHt.Text = _parameters.MeltingLayerPar.DvvBbOnlyMaxHeightM.ToString();
			textBoxMLDvvOnlyMinSnr.Text = _parameters.MeltingLayerPar.DvvBbOnlyMinSnr.ToString();
			textBoxMLGateSpacingRes.Text = _parameters.MeltingLayerPar.GateSpacingResolution.ToString();
			textBoxMLBrightBandPercent.Text = _parameters.MeltingLayerPar.BrightBandPercent.ToString();
			textBoxMLAcceptHtRange.Text = _parameters.MeltingLayerPar.AcceptHeightRangeM.ToString();
			textBoxMLAcceptPercent.Text = _parameters.MeltingLayerPar.AcceptPercent.ToString();
			textBoxMLMaxRainAboveBb.Text = _parameters.MeltingLayerPar.QcMaxRainAboveBb.ToString();
			textBoxMLOutputFolder.Text = _parameters.MeltingLayerPar.OutputFileFolder;
			checkBoxMLHourly.Checked = _parameters.MeltingLayerPar.WriteHourlyFiles;
			checkBoxMLWriteLogFile.Checked = _parameters.MeltingLayerPar.WriteLogFile;
			textBoxMLLogFolder.Text = _parameters.MeltingLayerPar.LogFileFolder;
			checkBoxMLHeader.Checked = _parameters.MeltingLayerPar.IncludeHeader;
			checkBoxMLUseRangeCorrectedMinSnr.Checked = _parameters.MeltingLayerPar.UseRangeCorrectedMinSnr;
			textBoxMLRangeCorrectedMinSnr.Text = _parameters.MeltingLayerPar.RangeCorrectedMinSnrOffset.ToString("0.0");
			checkBoxMLSkipNarrowWidths.Checked = _parameters.MeltingLayerPar.SkipNarrowWidths;
			textBoxMLNarrowWidth.Text = _parameters.MeltingLayerPar.NarrowWidthMS.ToString("0.00");
            checkBoxMLUseUpperNoiseHts.Checked = _parameters.MeltingLayerPar.ModifyNoiseLevels;
            textBoxMLNoiseGateLoIndex.Text = _parameters.MeltingLayerPar.NoiseGateLoIndex.ToString();
            textBoxMLNoiseGateHiIndex.Text = _parameters.MeltingLayerPar.NoiseGateHiIndex.ToString();
		}

		private void SaveMeltingLayerPage() {
			bool isOK;
			_parameters.MeltingLayerPar.Enable = checkBoxMLEnable.Checked;
			isOK = int.TryParse(textBoxMLTimeInterval.Text.ToString(),
								out _parameters.MeltingLayerPar.CalculateEveryMinute);

			isOK = int.TryParse(textBoxMLMinHeight.Text.ToString(),
								out _parameters.MeltingLayerPar.MinHeightM);
			isOK = int.TryParse(textBoxMLMaxHeight.Text.ToString(),
								out _parameters.MeltingLayerPar.MaxHeightM);
			_parameters.MeltingLayerPar.UseDataRegionOnly = checkBoxMLUseDataRegion.Checked;
			isOK = int.TryParse(textBoxMLMinDataHt.Text.ToString(),
								out _parameters.MeltingLayerPar.MinDataHtM);
			isOK = int.TryParse(textBoxMLMaxDataHt.Text.ToString(),
								out _parameters.MeltingLayerPar.MaxDataHtM);
			isOK = int.TryParse(textBoxMLMinSnrDvvPairs.Text.ToString(),
								out _parameters.MeltingLayerPar.MinSnrDvvPairs);
			isOK = int.TryParse(textBoxMLMinSnrRain.Text.ToString(),
								out _parameters.MeltingLayerPar.MinSnrRain);
			isOK = double.TryParse(textBoxMLDeltaSnrBb.Text.ToString(),
								out _parameters.MeltingLayerPar.DeltaSnrBb);
			isOK = double.TryParse(textBoxMLDeltaDvvBb.Text.ToString(),
								out _parameters.MeltingLayerPar.DeltaDvvBb);
			isOK = double.TryParse(textBoxMLMinSnrBb.Text.ToString(),
								out _parameters.MeltingLayerPar.MinSnrBb);
			isOK = int.TryParse(textBoxMLDvvOnlyMaxHt.Text.ToString(),
								out _parameters.MeltingLayerPar.DvvBbOnlyMaxHeightM);
			isOK = double.TryParse(textBoxMLDvvOnlyMinSnr.Text.ToString(),
								out _parameters.MeltingLayerPar.DvvBbOnlyMinSnr);
			isOK = int.TryParse(textBoxMLGateSpacingRes.Text.ToString(),
								out _parameters.MeltingLayerPar.GateSpacingResolution);
			isOK = int.TryParse(textBoxMLBrightBandPercent.Text.ToString(),
								out _parameters.MeltingLayerPar.BrightBandPercent);
			isOK = int.TryParse(textBoxMLAcceptHtRange.Text.ToString(),
								out _parameters.MeltingLayerPar.AcceptHeightRangeM);
			isOK = int.TryParse(textBoxMLAcceptPercent.Text.ToString(),
								out _parameters.MeltingLayerPar.AcceptPercent);
			isOK = int.TryParse(textBoxMLMaxRainAboveBb.Text.ToString(),
								out _parameters.MeltingLayerPar.QcMaxRainAboveBb);
			_parameters.MeltingLayerPar.OutputFileFolder = textBoxMLOutputFolder.Text;
			_parameters.MeltingLayerPar.LogFileFolder = textBoxMLLogFolder.Text;
			_parameters.MeltingLayerPar.WriteHourlyFiles = checkBoxMLHourly.Checked;
			_parameters.MeltingLayerPar.IncludeHeader = checkBoxMLHeader.Checked;
			_parameters.MeltingLayerPar.UseRangeCorrectedMinSnr = checkBoxMLUseRangeCorrectedMinSnr.Checked;
			isOK = double.TryParse(textBoxMLRangeCorrectedMinSnr.Text,
								out _parameters.MeltingLayerPar.RangeCorrectedMinSnrOffset);
			_parameters.MeltingLayerPar.UseRangeCorrectedMinSnr = checkBoxMLUseRangeCorrectedMinSnr.Checked;
			_parameters.MeltingLayerPar.WriteLogFile = checkBoxMLWriteLogFile.Checked;
			_parameters.MeltingLayerPar.SkipNarrowWidths = checkBoxMLSkipNarrowWidths.Checked;
			isOK = double.TryParse(textBoxMLNarrowWidth.Text.ToString(),
								out _parameters.MeltingLayerPar.NarrowWidthMS);
            _parameters.MeltingLayerPar.ModifyNoiseLevels = checkBoxMLUseUpperNoiseHts.Checked;
            isOK = int.TryParse(textBoxMLNoiseGateLoIndex.Text,
                                out _parameters.MeltingLayerPar.NoiseGateLoIndex);
            isOK = int.TryParse(textBoxMLNoiseGateHiIndex.Text,
                        out _parameters.MeltingLayerPar.NoiseGateHiIndex);
        }

		private void FillProcessingPage() {

			checkBoxReplay.Checked = _parameters.ReplayPar.Enabled;
            if (_parameters.ReplayPar.ProcessRawSamples) {
                radioButtonReplaySamples.Checked = true;
            }
			else if (_parameters.ReplayPar.ProcessTimeSeries) {
				radioButtonReplayTS.Checked = true;
			}
			else if (_parameters.ReplayPar.ProcessSpectra) {
				radioButtonReplaySpec.Checked = true;
			}
            else if (_parameters.ReplayPar.ProcessMoments) {
                radioButtonReplayMoments.Checked = true;
            }
            else if (_parameters.ReplayPar.ProcessXCorr) {
                radioButtonReplayXCorr.Checked = true;
            }
			else {
				radioButtonReplayNoRecalc.Checked = true;
			}

            checkBoxUseFmCwNSpec.Checked = _parameters.ReplayPar.UseFMCWNSpecOnReplay;
            if (_parameters.ReplayPar.NumberRecordsAtOnce < 1) {
                _parameters.ReplayPar.NumberRecordsAtOnce = 1;
            }
            textBoxTSRecAtOnce.Text = _parameters.ReplayPar.NumberRecordsAtOnce.ToString();

			textBoxReplayFilePath.Text = _parameters.ReplayPar.InputFile;
			if (File.Exists(labelReplayFile.Text)) {
				labelReplayFile.Text = Path.GetFileName(textBoxReplayFilePath.Text);
			}
			labelReplayFile.Text = Path.GetFileName(textBoxReplayFilePath.Text);

            TimeSpan start = _parameters.ReplayPar.StartTime;
            TimeSpan end = _parameters.ReplayPar.EndTime;
            int startDay = _parameters.ReplayPar.StartDay;
            int endDay = _parameters.ReplayPar.EndDay;
            if (end == TimeSpan.Zero) {
                end = new TimeSpan(24, 0, 0);
            }
            textBoxReplayStartTime.Text = start.Hours.ToString("00") + ":" + start.Minutes.ToString("00");
            textBoxReplayEndTime.Text = ((int)(end.TotalHours)).ToString("00") + ":" + end.Minutes.ToString("00");

            // startDay or endDay == 0 or blank means use only first day in data file
            if (startDay != 0) {
                textBoxReplayStartDay.Text = startDay.ToString();
            }
            else {
                textBoxReplayStartDay.Text = "";
            }
            if (endDay != 0) {
                textBoxReplayEndDay.Text = endDay.ToString();
            }
            else {
                textBoxReplayEndDay.Text = "";
            }

            checkBoxAutoCorr1Rx.Checked = _parameters.SystemPar.RadarPar.ProcPar.DoAutoCorr1Rx;

            // Ground Clutter

            checkBoxClutterRemoval.Checked = _parameters.SystemPar.RadarPar.ProcPar.RemoveClutter;
			textBoxClutterHtKm.Text = _parameters.SystemPar.RadarPar.ProcPar.MaxClutterHtKm.ToString("0.000");
            if (_parameters.SystemPar.RadarPar.ProcPar.KeepOriginalSpectra) {
                radioButtonKeepGCInSpec.Checked = true;
            }
            else {
                radioButtonDeleteGCFromSpec.Checked = true;
            }
            //radioButtonKeepGCInSpec.Checked = _parameters.SystemPar.RadarPar.ProcPar.KeepOriginalSpectra;
            int gcMethod = _parameters.SystemPar.RadarPar.ProcPar.GCMethod;
            if (gcMethod < 0 || gcMethod > 1) {
                gcMethod = 0;
            }
            textBoxGCMethod.Text = gcMethod.ToString();

            checkBoxGCDopRestrict.Checked = _parameters.SystemPar.RadarPar.ProcPar.GCRestrictExtent;
            checkBoxGCRestrictIfDC.Checked = _parameters.SystemPar.RadarPar.ProcPar.GCRestrictIfDcInPrev;
            textBoxGCPercentLess.Text = _parameters.SystemPar.RadarPar.ProcPar.GCPercentLess.ToString("F0");
            textBoxGCTimesBigger.Text = _parameters.SystemPar.RadarPar.ProcPar.GCTimesBigger.ToString("F1");
            textBoxGCSigThldDB.Text = _parameters.SystemPar.RadarPar.ProcPar.GCMinSigThldDB.ToString("F1");

			// consensus

			checkBoxCnsEnable.Checked = _parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsEnable;
            checkBoxCnsVertCorr.Checked = _parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsIsVertCorrection;
            textBoxCnsFileFolder.Text = _parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsFilePath;
            if (_parameters.ReplayPar.Enabled) {
                comboBoxCnsBeamMode.Visible = true;
                if (_parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].ReplayBeamMode == 3) {
                    comboBoxCnsBeamMode.SelectedIndex = 0;
                }
                else if (_parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].ReplayBeamMode == 5) {
                    comboBoxCnsBeamMode.SelectedIndex = 1;
                }
                else {
                    comboBoxCnsBeamMode.SelectedIndex = 0;
                }
            }
            else {
                comboBoxCnsBeamMode.Visible = false;
            }

            // fill wavelets box
            checkBoxClutterWavelet.Checked = _parameters.SystemPar.RadarPar.ProcPar.DoClutterWavelet;
            checkBoxDespikeWavelet.Checked = _parameters.SystemPar.RadarPar.ProcPar.DoDespikeWavelet;
            checkBoxHarmonicWavelet.Checked = _parameters.SystemPar.RadarPar.ProcPar.DoHarmonicWavelet;
            textBoxClutterWvltThld.Text = _parameters.SystemPar.RadarPar.ProcPar.WaveletClutterThldMed.ToString("F1");
            textBoxClutterWvltCutoff.Text = _parameters.SystemPar.RadarPar.ProcPar.WaveletClutterCutoffMps.ToString("F1");
            textBoxClutterWvltMaxHt.Text = _parameters.SystemPar.RadarPar.ProcPar.WaveletClutterMaxHt.ToString("F1");
            textBoxDespikeWvltThld.Text = _parameters.SystemPar.RadarPar.ProcPar.WaveletDespikeThldMed.ToString("F1");

			// fill restricted moments region

			checkBoxEnableRestrictedMoments.Checked = _parameters.SignalPeakSearchRange.Enabled;
			if ((checkBoxEnableRestrictedMoments.Checked)) {
				textBoxMomentLimitHigh.Enabled = true;
				textBoxMomentLimitLow.Enabled = true;
			}
			else {
				textBoxMomentLimitHigh.Enabled = false;
				textBoxMomentLimitLow.Enabled = false;
			}
			//double nyquist = _parameters.GetBeamParNyquist(0);
            textBoxMomentLimitHigh.Text = _parameters.SignalPeakSearchRange.VelHighMS.ToString("F2");
            textBoxMomentLimitLow.Text = _parameters.SignalPeakSearchRange.VelLowMS.ToString("F2");

			if (_parameters.ExcludeMomentIntervals.Enabled) {
				labelExcludeRegionsWarning.Visible = true;
			}
			else {
				labelExcludeRegionsWarning.Visible = false;
			}

            // Debug options
            checkBoxDebugNoHardware.Checked = _parameters.Debug.NoHardware;
            checkBoxSaveFilteredTS.Checked = _parameters.Debug.SaveFilteredTS;
            checkBoxDebugNoPbx.Checked = _parameters.Debug.NoPbx;
            checkBoxDebugFile.Checked = _parameters.Debug.DebugToFile;
            checkBoxDebugUseAlloc.Checked = _parameters.Debug.UseAllocator;
            checkBoxAllocTSOnly.Checked = _parameters.SystemPar.RadarPar.ProcPar.AllocTSOnly;
            checkBoxDoParallelTasks.Checked = _parameters.Debug.DoParallelTasks;

            if (_parameters.SystemPar.RadarPar.ProcPar.NSpecAtATime < 1) {
                _parameters.SystemPar.RadarPar.ProcPar.NSpecAtATime = 1;
            }
            textBoxMemAllocSize.Text = _parameters.SystemPar.RadarPar.ProcPar.NSpecAtATime.ToString();

		}

		private void SaveProcessingPage() {

			_parameters.ReplayPar.Enabled = checkBoxReplay.Checked;

            _parameters.ReplayPar.ProcessRawSamples = radioButtonReplaySamples.Checked;
            _parameters.ReplayPar.ProcessTimeSeries = radioButtonReplayTS.Checked;
            _parameters.ReplayPar.ProcessSpectra = radioButtonReplaySpec.Checked;
            _parameters.ReplayPar.ProcessMoments = radioButtonReplayMoments.Checked;
            _parameters.ReplayPar.ProcessXCorr = radioButtonReplayXCorr.Checked;

            _parameters.ReplayPar.UseFMCWNSpecOnReplay = checkBoxUseFmCwNSpec.Checked;
            if (!_parameters.ReplayPar.ProcessRawSamples && !_parameters.ReplayPar.ProcessTimeSeries) {
                // FMCW NSpec only used for TS processing
                _parameters.ReplayPar.UseFMCWNSpecOnReplay = false;
            }
            Int32.TryParse(textBoxTSRecAtOnce.Text, out _parameters.ReplayPar.NumberRecordsAtOnce);
            if (_parameters.ReplayPar.NumberRecordsAtOnce < 1) {
                _parameters.ReplayPar.NumberRecordsAtOnce = 1;
            }

            _parameters.Debug.DebugToFile = checkBoxDebugFile.Checked;
            _parameters.Debug.NoHardware = checkBoxDebugNoHardware.Checked;
            _parameters.Debug.NoPbx = checkBoxDebugNoPbx.Checked;
            _parameters.Debug.UseAllocator = checkBoxDebugUseAlloc.Checked;
            _parameters.SystemPar.RadarPar.ProcPar.AllocTSOnly = checkBoxAllocTSOnly.Checked;
            _parameters.Debug.SaveFilteredTS = checkBoxSaveFilteredTS.Checked;
            _parameters.Debug.DoParallelTasks = checkBoxDoParallelTasks.Checked;

            Int32.TryParse(textBoxMemAllocSize.Text, out _parameters.SystemPar.RadarPar.ProcPar.NSpecAtATime) ;

            /*
			if (radioButtonReplaySamples.Checked) {
				_parameters.ReplayPar.ProcessTimeSeries = true;
			}
			else {
				_parameters.ReplayPar.ProcessTimeSeries = false;
			}
			if (radioButtonReplaySpec.Checked) {
				_parameters.ReplayPar.ProcessSpectra = true;
			}
			else {
				_parameters.ReplayPar.ProcessSpectra = false;
			}
            */

			_parameters.ReplayPar.TimeDelayMs = 0;
			_parameters.ReplayPar.InputFile = textBoxReplayFilePath.Text;

            int startDay = 0;
            int endDay = 0;
            Int32.TryParse(textBoxReplayStartDay.Text, out startDay);
            Int32.TryParse(textBoxReplayEndDay.Text, out endDay);
            _parameters.ReplayPar.EndDay = endDay;
            _parameters.ReplayPar.StartDay = startDay;

            if (textBoxReplayStartTime.Text.Contains(":")) {
				_parameters.ReplayPar.StartTime = TimeSpan.Parse(textBoxReplayStartTime.Text);
			}
			else {
				_parameters.ReplayPar.StartTime = TimeSpan.Zero;
			}
			if (textBoxReplayEndTime.Text.Contains(":")) {
				try {
					_parameters.ReplayPar.EndTime = TimeSpan.Parse(textBoxReplayEndTime.Text);
				}
				catch (Exception e) {
					//MessageBox.Show(e.Message, "Time Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					_parameters.ReplayPar.EndTime = new TimeSpan(24, 0, 0);
				}
			}
			else {
				_parameters.ReplayPar.EndTime = TimeSpan.Zero ;
			}

            _parameters.SystemPar.RadarPar.ProcPar.DoAutoCorr1Rx = checkBoxAutoCorr1Rx.Checked;

			Double.TryParse(textBoxClutterHtKm.Text, out _parameters.SystemPar.RadarPar.ProcPar.MaxClutterHtKm);
			_parameters.SystemPar.RadarPar.ProcPar.RemoveClutter = checkBoxClutterRemoval.Checked;
            _parameters.SystemPar.RadarPar.ProcPar.KeepOriginalSpectra = radioButtonKeepGCInSpec.Checked;
            _parameters.SystemPar.RadarPar.ProcPar.GCRestrictExtent = checkBoxGCDopRestrict.Checked;
            _parameters.SystemPar.RadarPar.ProcPar.GCRestrictIfDcInPrev = checkBoxGCRestrictIfDC.Checked;
            Double.TryParse(textBoxGCTimesBigger.Text, out _parameters.SystemPar.RadarPar.ProcPar.GCTimesBigger);
            Double.TryParse(textBoxGCPercentLess.Text, out _parameters.SystemPar.RadarPar.ProcPar.GCPercentLess);
            Double.TryParse(textBoxGCSigThldDB.Text, out _parameters.SystemPar.RadarPar.ProcPar.GCMinSigThldDB);
            int gcMethod;
            Int32.TryParse(textBoxGCMethod.Text, out gcMethod);
            if (gcMethod < 0 || gcMethod > 1) {
                gcMethod = 0;
            }
            _parameters.SystemPar.RadarPar.ProcPar.GCMethod = gcMethod;
			
			_parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsEnable = checkBoxCnsEnable.Checked;
            _parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsIsVertCorrection = checkBoxCnsVertCorr.Checked;
            _parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsFilePath = textBoxCnsFileFolder.Text;
            if (comboBoxCnsBeamMode.SelectedIndex == 0) {
                _parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].ReplayBeamMode = 3;
            }
            else if (comboBoxCnsBeamMode.SelectedIndex == 1) {
                _parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].ReplayBeamMode = 5;
            }

            // save wavelet parameters
            _parameters.SystemPar.RadarPar.ProcPar.DoClutterWavelet = checkBoxClutterWavelet.Checked;
            _parameters.SystemPar.RadarPar.ProcPar.DoDespikeWavelet = checkBoxDespikeWavelet.Checked;
            _parameters.SystemPar.RadarPar.ProcPar.DoHarmonicWavelet = checkBoxHarmonicWavelet.Checked;
            Double.TryParse(textBoxClutterWvltThld.Text, out _parameters.SystemPar.RadarPar.ProcPar.WaveletClutterThldMed);
            Double.TryParse(textBoxClutterWvltCutoff.Text, out _parameters.SystemPar.RadarPar.ProcPar.WaveletClutterCutoffMps);
            Double.TryParse(textBoxClutterWvltMaxHt.Text, out _parameters.SystemPar.RadarPar.ProcPar.WaveletClutterMaxHt);
            Double.TryParse(textBoxDespikeWvltThld.Text, out _parameters.SystemPar.RadarPar.ProcPar.WaveletDespikeThldMed);

			// save restricted moments groupbox;
            _parameters.SignalPeakSearchRange.Enabled = checkBoxEnableRestrictedMoments.Checked;
            Double.TryParse(textBoxMomentLimitLow.Text, out _parameters.SignalPeakSearchRange.VelLowMS);
            Double.TryParse(textBoxMomentLimitHigh.Text, out _parameters.SignalPeakSearchRange.VelHighMS);

            /*
			if (!_parameters.ReplayPar.Enabled) {

				_parameters.ExcludeMomentIntervals.Enabled = checkBoxEnableRestrictedMoments.Checked;
				// put restricted intervals in first beam mode set
				if ((_parameters.ExcludeMomentIntervals.AllModesExcludeIntervals == null) ||
					(_parameters.ExcludeMomentIntervals.AllModesExcludeIntervals.Length != 1)) {
					_parameters.ExcludeMomentIntervals.AllModesExcludeIntervals = new PopParameters.ModeExcludeIntervals[1];
				}
				_parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[0].i = 0;
				_parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[0].Label = "Any Mode";
				_parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[0].Mode.Azimuth = -99;
				_parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[0].Mode.Elevation = -99;
				_parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[0].Mode.NCode = -99;
				_parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[0].Mode.PulseWidthNs = -99;

				if ((_parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[0].MomentExcludeIntervals == null) ||
					(_parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[0].MomentExcludeIntervals.Length != 2)) {

					_parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[0].MomentExcludeIntervals =
											new PopParameters.MomentExcludeInterval[2];
				}

				PopParameters.MomentExcludeInterval[] intervals = _parameters.ExcludeMomentIntervals.AllModesExcludeIntervals[0].MomentExcludeIntervals;
				// allow all heights
				intervals[0].HtLowM = -999999;
				intervals[0].HtHighM = 999999;
				intervals[1].HtLowM = -999999;
				intervals[1].HtHighM = 999999;
				// set lower excluded region
				intervals[0].VelLowMS = -999.0;
				Double.TryParse(textBoxMomentLimitLow.Text, out intervals[0].VelHighMS);
				// set higher excluded region
				intervals[1].VelHighMS = 999.0;
				Double.TryParse(textBoxMomentLimitHigh.Text, out intervals[1].VelLowMS);
				intervals[0].i = 0;
				intervals[1].i = 1;
				// check for case where both textboxes left blank.
				// Both values are set to 0 so everything is excluded;
				//	probably not what we wanted.
				if (intervals[0].VelHighMS == 0.0 && intervals[1].VelLowMS == 0.0) {
					intervals[0].VelHighMS = -999.0;
					intervals[1].VelLowMS = 999.0;
				}

			}
            */

		}

		private void checkBoxEnableRestrictedMoments_CheckedChanged(object sender, EventArgs e) {

			if (checkBoxEnableRestrictedMoments.Checked) {
				textBoxMomentLimitHigh.Enabled = true;
				textBoxMomentLimitLow.Enabled = true;
			}
			else {
				textBoxMomentLimitHigh.Enabled = false;
				textBoxMomentLimitLow.Enabled = false;
			}
		}



        //
        #endregion Private Tab Page Methods

        #region Private Windows Component Event Handlers
        //
        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void PopNSetup_Load(object sender, EventArgs e) {

            toolTip1.SetToolTip(textBoxReplayStartDay, "Set to 0 (or blank) to use actual first record of file for start day/year");
            toolTip1.SetToolTip(textBoxReplayEndDay, "Set to 0 to make end day same as start day");


            if (_parameters == null) {
                _parameters = new PopParameters();
                this.SetupParameters = _parameters;
            }
            _backupParameters = _parameters.DeepCopy();
            _savedParameters = _parameters.DeepCopy();
			if ((_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwDop) ||
                (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCwSA)) {
				tabControl1.SelectedTab = tabPageFmCw;
			}
            else if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.PulsedTx) {
                tabControl1.SelectedTab = tabPageProcessing;
            }
			// enable user setting frequency offset
			//radioButtonFreqOffset.Checked = true;
            timerCheckChanges.Interval = 5000;
            timerCheckChanges.Enabled = true;


        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void checkBoxTxOn_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxTxOn.Checked) {
                checkBoxTxOn.Text = "TX Pulse (ON)";
            }
            else {
                checkBoxTxOn.Text = "TX Pulse (OFF)";
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void numericUpDownBmPars_ValueChanged(object sender, EventArgs e) {
            _parameters.ArrayDim.MAXBEAMPAR = (int)numericUpDownBmPars.Value;
            PopParameters newPar = _parameters.DeepCopy();
            this.SetupParameters = newPar;
        }

        private void numericUpDownDirections_ValueChanged(object sender, EventArgs e) {
            _parameters.ArrayDim.MAXDIRECTIONS = (int)numericUpDownDirections.Value;
            PopParameters newPar = _parameters.DeepCopy();
            this.SetupParameters = newPar;
        }

        private void numericUpDownBeamSeq_ValueChanged(object sender, EventArgs e) {
            _parameters.ArrayDim.MAXBEAMS = (int)numericUpDownBeamSeq.Value;
            PopParameters newPar = _parameters.DeepCopy();
            this.SetupParameters = newPar;
        }

        private void numericUpDownNRx_ValueChanged(object sender, EventArgs e) {
            int nrx = (int)numericUpDownNRx.Value;
            _parameters.ArrayDim.MAXRXID = nrx;
            _parameters.SystemPar.RadarPar.ProcPar.NumberOfRx = nrx;
            PopParameters newPar = _parameters.DeepCopy();
            this.SetupParameters = newPar;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void radioButtonPulsedType_CheckedChanged(object sender, EventArgs e) {
            if (radioButtonPulsedType.Checked) {
                _parameters.SystemPar.RadarPar.RadarType = PopParameters.TypeOfRadar.PulsedTx;
            }
            else if (radioButtonFMCWSAType.Checked) {
                _parameters.SystemPar.RadarPar.RadarType = PopParameters.TypeOfRadar.FmCwSA;
            }
            else {
                _parameters.SystemPar.RadarPar.RadarType = PopParameters.TypeOfRadar.FmCwDop;
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called when TabPage gets focus and needs to be displayed
        /// </summary>
        private void tabPageDwell_Enter(object sender, EventArgs e) {
            FillDwellPage();
        }
        private void tabPageSystem_Enter(object sender, EventArgs e) {
            FillSystemPage();
        }
        private void tabPageFmCw_Enter(object sender, EventArgs e) {
            FillFmCwPage();
        }
		private void tabPageOutput_Enter(object sender, EventArgs e) {
			FillOutputPage();
		}
		private void tabPageMeltingLayer_Enter(object sender, EventArgs e) {
			FillMeltingLayerPage();
		}
		private void tabPageProcessing_Enter(object sender, EventArgs e) {
			FillProcessingPage();
		}





        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called when leaving a tab page or any time when we need to save contents
        /// </summary>
        private void tabPageDwell_Leave(object sender, EventArgs e) {
            SaveDwellPage();
        }
        private void tabPageSystem_Leave(object sender, EventArgs e) {
            SaveSystemPage(true);
        }
        private void tabPageFmCw_Leave(object sender, EventArgs e) {
            // When we click "Save" button, focus first moves to button from tabPage,
            //  so we come here.
            // But if we do anything here, we never get to button_click method.
            // So check to see if Save button click is pending
            //MessageBoxEx.Show("Leave Tab " + _saveButtonClicked.ToString(), 500);

            if (!_saveButtonClicked) {
                SaveFmCwPage(true);
                _saveButtonClicked = false;
                //TextFile.WriteLineToFile("Dave.txt", "TabPage_Leave SavePage");
            }
            else {
                //TextFile.WriteLineToFile("Dave.txt", "TabPage_Leave");
            }
        }
		private void tabPageOutput_Leave(object sender, EventArgs e) {
            SaveOutputPage(true);
		}
		private void tabPageMeltingLayer_Leave(object sender, EventArgs e) {
			SaveMeltingLayerPage();
		}
		private void tabPageProcessing_Leave(object sender, EventArgs e) {
			SaveProcessingPage();
		}


        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Event for clicking Cancel Changes button.
        /// Fill all pages with original parameters.
        /// </summary>
        private void buttonCancel_Click(object sender, EventArgs e) {
            //
            // _savedParameters and _backupParameters are used for
            // detecting changes and undoing changes;
            // see: buttonSave, buttonSaveAs, buttonCancel, timerCheckChanges
            //
            buttonCancel.Enabled = false;
            if (buttonCancel.Text.ToLower().Contains("undo")) {
                SaveAllTabPages(false);
                PopParameters tempPar = new PopParameters();
                tempPar = _parameters.DeepCopy();
                this.SetupParameters = _savedParameters;
                _backupParameters = tempPar.DeepCopy();
                buttonCancel.Text = "Redo Changes";
            }
            else {
                this.SetupParameters = _backupParameters.DeepCopy();
                buttonCancel.Text = "Undo All Changes";
            }
            /*
            PopParameters tempPar = new PopParameters();
            SaveAllTabPages();
            tempPar = _parameters.DeepCopy();
            this.SetupParameters = _backupParameters.DeepCopy();
            _backupParameters = tempPar.DeepCopy();
            //_backupParameters = _parameters.DeepCopy();
            //FillParameterScreens();
             * */
            buttonCancel.Enabled = true;
            MessageBeep((int)MessageBoxIcon.Asterisk);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridViewBeamPar_CellValidating(object sender, DataGridViewCellValidatingEventArgs e) {
            int row = e.RowIndex;
            int column = e.ColumnIndex;
            string fValue = e.FormattedValue.ToString();
            //DataGridView grid = (DataGridView)sender;
            //object o = grid.Rows[row].Cells[column].Value;
            if (fValue.Trim() == string.Empty) {
                // don't worry about empty cells
                return;
            }
            int x = 0;
            if (!int.TryParse(fValue, out x)) {
                //dataGridViewBeamPar.Rows[row].ErrorText = "Value must be an integer. Press ESC to continue anyway.";
                MessageBox.Show("Value must be an integer.\nRe-enter the parameter or press ESC to cancel the edit.", "Input Format Error");
                e.Cancel = true;
            }
            //string num = grid.Rows[row].Cells[column].Value.ToString();
            //SaveDwellPage();
            //FillDwellPage();
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Validate entries into the Beam Sequence Table
        /// </summary>
        private void dataGridViewSequence_CellValidating(object sender, DataGridViewCellValidatingEventArgs e) {
            int row = e.RowIndex;
            int column = e.ColumnIndex;
            string fValue = e.FormattedValue.ToString();	// the value to validate
            if ((column == 0)) {
                // this is the label column
                return;
            }
            if (fValue.Trim() == string.Empty) {
                // don't worry about empty cells
                return;
            }
            int iValue;
            if (!int.TryParse(fValue, out iValue)) {
                //dataGridViewSequence.Rows[row].Cells[column].ErrorText = "Value must be an integer. Press ESC to continue anyway.";
                MessageBox.Show("Value must be an integer.\nRe-enter the parameter or press ESC to cancel the edit.", "Input Format Error");
                e.Cancel = true;
                return;
            }
            if (row == 0) {
                // this is an NREP entry
            }
            else {
                // this is a beam parameter index entry
                int ipar = iValue - 1;
                if (iValue > _parameters.ArrayDim.MAXBEAMPAR) {
                    MessageBox.Show("Beam Parameter Index must not be greater than " + _parameters.ArrayDim.MAXBEAMPAR.ToString(), "Parameter Error");
                    e.Cancel = true;
                    return;
                }
                if (CellIsEmpty(0, column)) {
                    // nRep cell is empty, fill in default value
                    dataGridViewSequence.Rows[0].Cells[column].Value = "1";
                }
                // clear other cells
                for (int j = 0; j < _parameters.ArrayDim.MAXDIRECTIONS; j++) {
                    if (j + 1 != row) {
                        dataGridViewSequence.Rows[j + 1].Cells[column].Value = "";
                    }
                }
            }
        }
        ///

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        private void buttonNew_Click(object sender, EventArgs e) {
            PopParameters newParams = new PopParameters();
            this.SetupParameters = newParams;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Save the currently display parameter settings
        /// </summary>
        private void buttonSaveAs_Click(object sender, EventArgs e) {

            // The following is now called from buttonSaveAs_MouseEnter
            // see tabPageFmCw_Leave method etc.
            /*
            bool ok = SaveAllTabPages(true);
            if (!ok) {
                return;
            }
            */

            _saveButtonClicked = false;     // see tabPageFmCw_Leave method etc.
            //saveFileDialog1.InitialDirectory = Path.GetDirectoryName(_parameters.Source);
            //saveFileDialog1.FileName = Path.GetFileName(_parameters.Source);
            saveFileDialog1.InitialDirectory = Path.GetDirectoryName(textBoxFileName.Text);
            saveFileDialog1.FileName = Path.GetFileName(textBoxFileName.Text);
            DialogResult rr = saveFileDialog1.ShowDialog();
            if (rr == DialogResult.OK) {
                string ext = Path.GetExtension(saveFileDialog1.FileName);
                if (ext.ToLower() == ".parx") {
                    _parameters.Source = saveFileDialog1.FileName;
					textBoxFileName.Text = _parameters.Source;
					textBoxFileName.SelectionStart = textBoxFileName.TextLength;
					_parameters.WriteToFile(saveFileDialog1.FileName);
                    //
                    // _savedParameters and _backupParameters are used for
                    // detecting changes and undoing changes;
                    // see: buttonSave, buttonSaveAs, buttonCancel_Click, timerCheckChanges
                    //
                    _savedParameters = _parameters.DeepCopy();
                    _backupParameters = _parameters.DeepCopy();
                }
                else {
                    MessageBox.Show("Don't know how to save " + ext + " files yet.", "File NOT saved");
                }
            }
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void buttonSave_Click(object sender, EventArgs e) {

            // The following is now called from buttonSave_MouseEnter
            // see tabPageFmCw_Leave method etc.

            //TextFile.WriteLineToFile("Dave.txt", "SaveButton_Click");
            bool ok = SaveAllTabPages(true);
            if (!ok) {
                return;
            }

            //TextFile.WriteLineToFile("Dave.txt", "SaveButton_Click save to par file");
            bool timerIsEnabled = timerCheckChanges.Enabled;
            timerCheckChanges.Enabled = false;
            SaveToParFile();
            _savedParameters = _parameters.DeepCopy();
            _backupParameters = _parameters.DeepCopy();

            timerCheckChanges_Tick(null, null);
            timerCheckChanges.Enabled = timerIsEnabled;
            _saveButtonClicked = false;     // see tabPageFmCw_Leave method etc.
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// NOTE: when exiting, must set Living=false;
        /// </summary>
        private void PopNSetup_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                // the user probably expects everything visible to be used by POPN
                // So first save what is on the visible TabPage, then save the others.
                // Need to do this if change to one page affects another.
                PopNSetup3 setup = sender as POPN.PopNSetup3;
                if (setup != null) {
                    if (setup.tabPageFmCw.Visible) {
                        SaveFmCwPage(true);
                    }
                    if (setup.tabPageDwell.Visible) {
                        SaveDwellPage();
                    }
                    if (setup.tabPageSystem.Visible) {
                        SaveSystemPage(true);
                    }
                    if (setup.tabPageOutput.Visible) {
                        SaveOutputPage(true);
                    }
                    if (setup.tabPageMeltingLayer.Visible) {
                        SaveMeltingLayerPage();
                    }
                    if (setup.tabPageProcessing.Visible) {
                        SaveProcessingPage();
                    }
                }
                bool ok = SaveAllTabPages(true);
                if (!ok) {
                    e.Cancel = true;
                    IsLiving = true;
                    return;
                }
				PopParameters fileParameters = PopParameters.ReadFromFile(textBoxFileName.Text);
				// compare the parameters in the file to the parameters on the screen
                if (fileParameters.Equals(_parameters)) {
                    IsLiving = false;
                    return;
                }
                else {
                    DialogResult rr;
                    SaveChangesBox messageBox = new SaveChangesBox();
                    rr = messageBox.ShowDialog(this);
                    /*
                    rr = MessageBox.Show("You have not saved your parameter changes.\n"+
                                        "Do you want to save changes?",
                                        "Parameters not saved", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
                    */
                    if (rr == DialogResult.Cancel) {
						// cancel closing form
                        e.Cancel = true;
                        IsLiving = true;
                    }
					else if (rr == DialogResult.OK) {
						// save then exit
						buttonSave_Click(null, null);
                        IsLiving = false;
					}
                    else if (rr == DialogResult.Ignore) {
                        IsLiving = false;
                    }
                    messageBox.Dispose();
                    return;
                }
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        private void textBoxSweepNPts_TextChanged(object sender, EventArgs e) {
            int npts;
            bool OK = int.TryParse(textBoxSweepNPts.Text, out npts);
            if (OK) {
                int htsCalculated = (npts / 2 + 1);
                labelHtsCalculated.Text = htsCalculated.ToString();
                if (!checkBoxSelectHts.Checked) {
                    numericUpDownGateFirst.Value = 0;
                    numericUpDownGateLast.Value = htsCalculated - 1;
                }
                labelNHts.Text = (numericUpDownGateLast.Value - numericUpDownGateFirst.Value + 1).ToString() ;
            }
			RecalculateRangeRes();
			RecalculateLastSample();
            //CalculateSweepRegisterValues();
            UpdateDDSValues();
        }

		/*
		private void textBoxFmCwTimeStepUSec_TextChanged(object sender, EventArgs e) {
			RecalculateRangeRes();
		}
		*/

		private void textBoxFmCwSweepCenterMHz_TextChanged(object sender, EventArgs e) {
            UpdateDDSValues();
            //CalculateSweepRegisterValues();
		}

		private void textBoxFmCwSweepRate_TextChanged(object sender, EventArgs e) {
			RecalculateRangeRes();
			double sweep = 0.0;
			double sweepDDS = 0.0;
			double.TryParse(textBoxFmCwSweepRate.Text, out sweep);
			double.TryParse(labelDDSSweepRate.Text, out sweepDDS);
			if (sweep != sweepDDS) {
				_rampIsOriginal = true;
				//buttonUseDDSValues.Text = "Use DDS Values";
			}
		}

		private void textBoxFmCwOffsetGate_TextChanged(object sender, EventArgs e) {
			if (radioButtonGateOffset.Checked) {
				RecalculateRangeRes();
			}
		}

		private void textBoxFmCwOffsetHz_TextChanged(object sender, EventArgs e) {
			if (radioButtonFreqOffset.Checked) {
				if (true) {
					
				}
				RecalculateRangeRes();
			}
		}

		private void textBoxSweepSpacing_TextChanged(object sender, EventArgs e) {
			RecalculateRangeRes();
			RecalculateLastSample();
            UpdateDDSValues();
            //CalculateSweepRegisterValues();
        }

		private void textBoxSweepSampleDelay_TextChanged(object sender, EventArgs e) {
			RecalculateLastSample();
            UpdateDDSValues();
            //CalculateSweepRegisterValues();
        }

		private void textBoxFmCwTimeStepClocks_TextChanged(object sender, EventArgs e) {
			RecalculateRangeRes();
		}


		private void RecalculateRangeRes() {
			bool isOK;
			int npts, spacing;
            double sweepRateHzUsec;
			int sysDelayNs;

			isOK = int.TryParse(textBoxSweepNPts.Text.ToString(), out npts);
			isOK = int.TryParse(textBoxSweepSpacing.Text.ToString(), out spacing);
            isOK = double.TryParse(textBoxFmCwSweepRate.Text.ToString(), out sweepRateHzUsec);
			// FMCW uses only sysdelay #0 from main page
			//sysDelayNs = _parameters.SystemPar.RadarPar.RxBw[0].BwDelayNs;
            double value;
            isOK = double.TryParse(textBoxFmCwSysDelayNs.Text, out value);
            sysDelayNs = (int)Math.Floor(value + 0.5);

			double sysDelayCorrectionM = sysDelayNs * PopParameters.MperNs;

			double sampleSpacingHz = 1.0e9 / (npts * spacing);
			double rangeSpacingNs = 1.0e3 * sampleSpacingHz / (sweepRateHzUsec);
			labelGateSpacingNs.Text = rangeSpacingNs.ToString("f2");

			double rangeResM = PopParameters.MperNs * rangeSpacingNs;
			labelRangeResM.Text = rangeResM.ToString("f3");

			int firstGate = (int)numericUpDownGateFirst.Value;
			int lastGate = (int)numericUpDownGateLast.Value;

			//range = NewRangeM(0, rangeResM, rangeCorrections);

			double freqOffset;
			double gateOffset;
			//isOK = double.TryParse(textBoxFmCwOffsetHz.Text, out freqOffset);
			if (radioButtonGateOffset.Checked) {
				double.TryParse(textBoxFmCwOffsetGate.Text, out gateOffset);
				//double offset0 = OffsetFreq(gateOffset, sampleSpacingHz, rangeResM, sysDelayCorrectionM);
                freqOffset = AD9959EvalBd.OffsetFreqHz(gateOffset, npts, spacing, sysDelayNs, sweepRateHzUsec);
                textBoxFmCwOffsetHz.Text = freqOffset.ToString("f3");
                // actual (DDS) gate offset:
                //double gateOffset0 = OffsetGate(freqOffset, sampleSpacingHz, rangeResM, sysDelayCorrectionM);
                double gateOffset1 = AD9959EvalBd.OffsetGate(freqOffset, npts, spacing, sysDelayNs, sweepRateHzUsec);
            }
			else {
                double.TryParse(textBoxFmCwOffsetHz.Text, out freqOffset);
                //double gateOffset0 = OffsetGate(freqOffset, sampleSpacingHz, rangeResM, sysDelayCorrectionM);
                gateOffset = AD9959EvalBd.OffsetGate(freqOffset, npts, spacing, sysDelayNs, sweepRateHzUsec);
				textBoxFmCwOffsetGate.Text = gateOffset.ToString("f2");
			}

			// check: this is gate offset from save params:
			double jgate = _parameters.GetGateOffset();

			//double offsetToUse;
            double rangeCorrections = rangeResM * freqOffset / sampleSpacingHz + sysDelayCorrectionM;
			double firstGateM;
			double lastGateM;

            // POPREV:  Changed to (gateoffset < 0) rev 3.14
            // if (freqOffset < 0.0) {
            if (gateOffset < 0.0) {
                _lastNegOffset = freqOffset;
                // for negative offsets, we will rearrange FFT pts
                // so that zero range is first point
                _maxGate = (int)Math.Abs(gateOffset);
                checkBoxSelectHts.Checked = true;
                if (_lastOffset > 0.0) {
                    numericUpDownGateFirst.Value = 0;
                }
                if (lastGate >= _maxGate) {
                    lastGate = _maxGate;
                    numericUpDownGateLast.Value = lastGate;
                    _usesMaxGate = true;
                }
                else {
                    _usesMaxGate = false;
                }
                // original ranges before flipping
                firstGateM = NewRangeM(0, rangeResM, rangeCorrections);
                lastGateM = NewRangeM(_maxGate, rangeResM, rangeCorrections);
                // now account for flipping of entire FFT region
                int nGates = _maxGate + 1;
                lastGateM = firstGateM;
                firstGateM = lastGateM - (nGates - 1) * rangeResM;
                // now account for selected hts
                firstGateM = firstGateM + firstGate * rangeResM;
                lastGateM = lastGateM - (_maxGate - lastGate) * rangeResM;
                _lastOffset = freqOffset;
            }
            else {
                _lastPosOffset = freqOffset;
                firstGateM = NewRangeM(firstGate, rangeResM, rangeCorrections);
                lastGateM = NewRangeM(lastGate, rangeResM, rangeCorrections);
                _lastOffset = freqOffset;
            }

			//rangeCorrections = rangeResM * offset / sampleSpacingHz + sysDelayCorrectionM;
			//firstGateM = NewRangeM(firstGate, rangeResM, rangeCorrections);
			//lastGateM = NewRangeM(lastGate, rangeResM, rangeCorrections);
			/*
			if (offset < 0.0) {
				int nGates = lastGate - firstGate + 1;
				lastGateM = firstGateM;
				firstGateM = lastGateM - (nGates - 1) * rangeResM;
			}
			*/
			labelFirstGateKm.Text = (firstGateM / 1000.0).ToString("f3");
			labelLastGateKm.Text = (lastGateM / 1000.0).ToString("f3");

			// pulsed Doppler sample delay to get same first gate range
			_virtualRangeDelayNs = (int)Math.Floor(firstGateM / PopParameters.MperNs + sysDelayNs + 0.5);

			CheckGateSpacing();

            UpdateDDSValues();
            //CalculateSweepRegisterValues();

		}

		private void CheckGateSpacing() {
			double gateSpacingNs;
			int nHeights;
			int.TryParse(labelNHts.Text, out nHeights);
			double.TryParse(labelGateSpacingNs.Text, out gateSpacingNs);
			int iSpacingNs = (int)(gateSpacingNs + 0.5);
			double diff = Math.Abs(gateSpacingNs - (double)iSpacingNs);
			diff = diff * nHeights;
			if (diff > gateSpacingNs / 2.0) {
				// rounding sample spacing to integer ns in pop files
				// will cause greater than half gate error
				labelGateSpacingNs.BackColor = Color.Yellow;
			}
			else {
				labelGateSpacingNs.BackColor = Color.FloralWhite;
			}
		}

        //
        // Dependencies on FMCW form
        //
        // DDS registers and created frequencies depend upon:
        //  RefClock
        //  RefClockMultiplier
        //  CenterFrequency
        //  SweepRate
        //  DeltaTPeriods
        //  GateOffset (FreqOffset)
        //  NSamples
        //  SampleDelay
        //  SampleSpacing
        //  SystemDelay
        //  SweepBeyondSamples
        //
        // Updating DDS affects:
        //  DDS registers and frequencies
        //  Freq offset parameter
        //  Gate offset parameter
        //  SweepRate parameter
        //
        // LastSample and PostBlank depend upon:
        //  NSamples
        //  SampleDelay
        //  SampleSpacing
        //  SweepBeyondSamples
        //
        // Nyquist depends upon:
        //  Ipp
        //
        // If offset is negative affected fields are:
        //  First, last gate
        //  Gates to keep
        //
        // RangeResolution depends upon:
        //  NSamples
        //  SampleSpacing
        //  SweepRate
        //  SysDelay (if offset freq fixed)
        //
        //  SystemDelay affects:
        //   if gateOffset is fixed:
        //      FreqOffset (DDS calculated value)
        //   if FreqOffset is fixed:
        //      GateOffset
        //      First and Last Gate
        //
        // So "derived parameters" on FmCwTabPage depend upon
        //  NSamples
        //  SampleSpacing
        //  SampleDelay
        //  SysDelay (if offset freq fixed)
        //  SweepRate
        //  Offset
        //  IPP (nyq and sample time)
        //  Doppler Npts (sample time)
        //  SweepBeyondSamples (PostBlank)

        private void UpdateDDSValues() {

            AD9959EvalBd.DDSInputValues ddsIn;
            double.TryParse(textBoxFmCwSweepCenterMHz.Text, out ddsIn.CenterFreqMHz);
            int.TryParse(textBoxFmCwTimeStepClocks.Text, out ddsIn.DeltaTPeriods);
            ddsIn.IsSpecifyingGateOffset = true;
            ddsIn.FreqOffsetHz = 0.0;  // if IsSpecifyingGateOffset this is not an input
            double.TryParse(textBoxFmCwOffsetGate.Text, out ddsIn.GateOffset);
            int.TryParse(textBoxSweepNPts.Text, out ddsIn.NSamples);
            double.TryParse(textBoxDDSRefClockMHz.Text, out ddsIn.RefClockMHz);
            int.TryParse(comboBoxDDSMultiplier.Text, out ddsIn.RefClockMultiplier);
            double.TryParse(textBoxSweepSampleDelay.Text, out ddsIn.SampleDelayNs);
            double.TryParse(textBoxSweepSpacing.Text, out ddsIn.SampleSpacingNs);
            double.TryParse(textBoxFmCwSweepRate.Text, out ddsIn.SweepRateHzUsec);
            double.TryParse(textBoxFmCwSysDelayNs.Text, out ddsIn.SystemDelayNs);
            ddsIn.SweepBeyondSamplesNs = _sweepBeyondSamplesUs * 1000.0;

            AD9959EvalBd.DDSCalculatedValues ddsOut;
            AD9959EvalBd.CalculateDDSValues(ddsIn, out ddsOut);

            labelDDSSweepRate.Text = ddsOut.SweepRateHzUsec.ToString("F3");
            labelDDSRampDurationUsec.Text = ddsOut.SweepDurationUsec.ToString("F3");
            labelDDSDeltaFreqHz.Text = ddsOut.DeltaFreqHz.ToString("F3");
            labelDDSDeltaTimeNs.Text = ddsOut.DeltaTNsec.ToString("F1");
            labelDDSDeltaFreqReg.Text = ddsOut.DeltaFreqRegValue.ToString();
            labelDDSDeltaTimeReg.Text = ddsOut.DeltaTRegValue.ToString();
            labelDDSFreqOffsetHz.Text = (ddsOut.OffsetFreqMHz * 1.0e6).ToString("F3");
            if (ddsIn.IsSpecifyingGateOffset) {
                textBoxFmCwOffsetHz.Text = (ddsOut.OffsetFreqMHz * 1.0e6).ToString("F3");
            }
            else {
                textBoxFmCwOffsetGate.Text = ddsOut.OffsetGate.ToString("F5");
            }
            labelDDSStartFreqHz1.Text = (ddsOut.StartFreq1MHz * 1.0e6).ToString("F2");
            labelDDSStartFreqHz2.Text = (ddsOut.StartFreq2MHz * 1.0e6).ToString("F2");
            labelDDSEndFreqHz1.Text = (ddsOut.EndFreq1MHz * 1.0e6).ToString("F2");
            labelDDSEndFreqHz2.Text = (ddsOut.EndFreq2MHz * 1.0e6).ToString("F2");
            labelDDSStartFreqReg1.Text = ddsOut.StartFreq1RegValue.ToString();
            labelDDSStartFreqReg2.Text = ddsOut.StartFreq2RegValue.ToString();
            labelDDSEndFreqReg1.Text = ddsOut.EndFreq1RegValue.ToString();
            labelDDSEndFreqReg2.Text = ddsOut.EndFreq2RegValue.ToString();
        }

        /*
        private void CalculateSweepRegisterValues() {
            AD9959EvalBd dds;
            CalculateSweepRegisterValues(out dds);
        }
        */

        /*
        private void CalculateSweepRegisterValues(out AD9959EvalBd DDS) {

            double sweepRate, offset, syncClockPeriodNs;
            int timeStepClocks;
            double lastUs;
            double timeStepUs;
            double centerFreqMHz;
            double sysClockMHz;
            double ippUs;

            double refClockMHz;
            int ddsMultiplier;

            //SaveFmCwPage();

            double.TryParse(textBoxFmCwIppUSec.Text, out ippUs);
            double.TryParse(textBoxFmCwSweepCenterMHz.Text, out centerFreqMHz);
            double.TryParse(textBoxFmCwSweepRate.Text, out sweepRate);
            double.TryParse(textBoxFmCwOffsetHz.Text, out offset);
            int.TryParse(textBoxFmCwTimeStepClocks.Text, out timeStepClocks);
            double.TryParse(labelDDSSyncClockPeriodNsec.Text, out syncClockPeriodNs);
            double.TryParse(labelDDSSysClockMHz.Text, out sysClockMHz);
            timeStepUs = timeStepClocks * syncClockPeriodNs / 1000.0;

            double.TryParse(textBoxDDSRefClockMHz.Text, out refClockMHz);
            int.TryParse(comboBoxDDSMultiplier.Text, out ddsMultiplier);

            RecalculateLastSample();
            double.TryParse(labelLastSampleUs.Text, out lastUs);

            // create AD9959 board object without connecting to hardware
            DDS = new AD9959EvalBd(refClockMHz,
                                                ddsMultiplier,
                                                ippUs,
                                                false);
            double preTRUs = 0.0;
            double txUs = 1.0;
            double delayNs;
            int nSamples;
            double spacingNs;
            double sweepBeyondSamplesUs = _sweepBeyondSamplesUs;

            // TODO: put use DDS
            double.TryParse(textBoxSweepSampleDelay.Text, out delayNs);
            double.TryParse(textBoxSweepSpacing.Text, out spacingNs);
            int.TryParse(textBoxSweepNPts.Text, out nSamples);
            // total length of sweep:
            double sweepTimeUs = preTRUs + txUs + delayNs / 1000.0 + (nSamples - 1) * spacingNs / 1000.0 + sweepBeyondSamplesUs;
            // set time where we want 60 MHz to be at middle of samples:
            double midSweepTimeUs = preTRUs + txUs + delayNs / 1000.0 + (nSamples - 1) * spacingNs / 2000.0;
            DDS.SetFreqSweepParameters(0, centerFreqMHz, sweepRate, 0.0, timeStepClocks, sweepTimeUs, midSweepTimeUs);
            DDS.SetFreqSweepParameters(1, centerFreqMHz, sweepRate, offset, timeStepClocks, sweepTimeUs, midSweepTimeUs);

            // read back actual register values to be used by DDS
            double actualStartFreq0 = DDS.SweepStartFreq0MHz;
            double actualEndFreq0 = DDS.SweepEndFreq0MHz;
            double actualDeltaFreq0 = DDS.SweepDeltaFreq0MHz;
            double actualDeltaTime0 = DDS.SweepDeltaTime0Usec;
            double actualStartFreq1 = DDS.SweepStartFreq1MHz;
            double actualEndFreq1 = DDS.SweepEndFreq1MHz;
            double actualDeltaFreq1 = DDS.SweepDeltaFreq1MHz;
            double actualDeltaTime1 = DDS.SweepDeltaTime1Usec;

            // compute derived sweep values
            double actualOffset = (actualStartFreq1 - actualStartFreq0) * 1.0e6;	// Hz
            double actualRampRate0 = 1.0e6 * actualDeltaFreq0 / actualDeltaTime0;  // Hz/usec
            double actualRampRate1 = 1.0e6 * actualDeltaFreq1 / actualDeltaTime1;
            double actualRampTime = (actualEndFreq0 - actualStartFreq0) / (actualDeltaFreq0 / actualDeltaTime0);  // usec

            // display actual values
            labelDDSSweepRate.Text = actualRampRate0.ToString("f4");
            //labelDDSSweepRate1.Text = actualRampRate1.ToString("f4");
            labelDDSFreqOffsetHz.Text = actualOffset.ToString("f3");
            labelDDSStartFreqMHz1.Text = actualStartFreq0.ToString("f3");
            labelDDSStartFreqMHz2.Text = actualStartFreq1.ToString("f3");
            labelDDSEndFreqMHz1.Text = actualEndFreq0.ToString("f3");
            labelDDSEndFreqMHz2.Text = actualEndFreq1.ToString("f3");
            labelDDSDeltaFreqHz.Text = (actualDeltaFreq0 * 1.0e6).ToString("F3");
            labelDDSDeltaFreqHz.Text = (actualDeltaFreq1 * 1.0e6).ToString("F3");
            labelDDSDeltaTimeUsec.Text = actualDeltaTime0.ToString("F3");
            labelDDSDeltaTimeUsec.Text = actualDeltaTime1.ToString("F3");
            labelDDSRampDurationUsec.Text = actualRampTime.ToString("F1");

            return;

            //////////////////////////////////////////////////
        }
        */

        private void RecalculateLastSample() {
            int nSamples, delayNs, spacingNs;
            int.TryParse(textBoxSweepNPts.Text, out nSamples);
            int.TryParse(textBoxSweepSampleDelay.Text, out delayNs);
            int.TryParse(textBoxSweepSpacing.Text, out spacingNs);
            int lastNs = delayNs + (nSamples - 1) * spacingNs;
            double lastUs = lastNs / 1000.0;
            labelLastSampleUs.Text = lastUs.ToString("f0");
            double postBlank = lastNs / 1000.0 + _sweepBeyondSamplesUs;  
            if (checkBoxFmCwPostBlankAuto.Checked) {
                textBoxFmCwPostBlankUs.Text = postBlank.ToString("f3");
            }
        }
        

        private void RecalculateNyquist() {
            double hzms = (149.896 / _parameters.SystemPar.RadarPar.TxFreqMHz);
            double ipp;
            bool isOK = double.TryParse(textBoxFmCwIppUSec.Text, out ipp);
            double nyq = 0.0;
            if (isOK && (ipp != 0.0)) {
                nyq = (float)(0.5e6 * hzms / ipp);    // nyquist freq in m/s 
            }
            labelNyquistMS.Text = nyq.ToString("F2");

        }
        


		private double NewRangeM(double igate, double rangeResM, double rangeCorrections) {
			double range;
			range = rangeResM * igate - rangeCorrections;
			return range;
		}

        /*
		public double OffsetFreq(double iGate0, double delfR, double delR, double sysDelay) {
			double offset = delfR * (iGate0 - sysDelay / delR);
			return offset;
		}

		public double OffsetGate(double offset, double delfR, double delR, double sysDelay) {
			//offset = delfR * (iGate0 - 0.5 - sysDelay / delR);
			double gate = offset / delfR + sysDelay / delR;
			//return (int)(gate + 0.5);
			return gate;
		}
         * */


		//////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        private void checkBoxSelectHts_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxSelectHts.Checked) {
                numericUpDownGateFirst.Enabled = true;
                numericUpDownGateLast.Enabled = true;
            }
            else {
                numericUpDownGateFirst.Enabled = false;
                numericUpDownGateLast.Enabled = false;
                textBoxSweepNPts_TextChanged(null, null);
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        private void buttonUpdateFmCw_Click(object sender, EventArgs e) {
            labelNHts.Text = (numericUpDownGateLast.Value - numericUpDownGateFirst.Value + 1).ToString();
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        private void numericUpDownGateFirst_ValueChanged(object sender, EventArgs e) {
            CheckSelectedGateRange();
            labelNHts.Text = (numericUpDownGateLast.Value - numericUpDownGateFirst.Value + 1).ToString();
			RecalculateRangeRes();
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        private void numericUpDownGateLast_ValueChanged(object sender, EventArgs e) {
            CheckSelectedGateRange();
            labelNHts.Text = (numericUpDownGateLast.Value - numericUpDownGateFirst.Value + 1).ToString();
			RecalculateRangeRes();
		}

		private void checkBoxEnableDiskWrite_1_CheckedChanged(object sender, EventArgs e) {
			if (checkBoxEnableDiskWrite_1.Checked) {
                checkBoxSpectra_1.Enabled = true;
                checkBoxXCorr_1.Enabled = true;
                checkBoxMoments_1.Enabled = true;
				checkBoxFullTS_1.Enabled = true;
				checkBoxRawTS_1.Enabled = true;
				checkBox1TS_1.Enabled = true;
                checkBox1TSText_1.Enabled = true;
                checkBoxFullTSText_1.Enabled = true;
                checkBoxRawTSText_1.Enabled = true;
                radioButtonAppend_1.Enabled = true;
				radioButtonOverwrite_1.Enabled = true;
				radioButtonDayFiles_1.Enabled = true;
				radioButtonHourFiles_1.Enabled = true;
				radioButtonPopName_1.Enabled = true;
				radioButtonLapxmName_1.Enabled = true;
				textBoxOutFolder_1.Enabled = true;
				textBoxSite_1.Enabled = true;
				textBoxSuffix_1.Enabled = true;
			}
			else {
				checkBoxSpectra_1.Enabled = false;
                checkBoxXCorr_1.Enabled = false;
                checkBoxMoments_1.Enabled = false;
				checkBoxFullTS_1.Enabled = false;
				checkBoxRawTS_1.Enabled = false;
				checkBox1TS_1.Enabled = false;
                checkBox1TSText_1.Enabled = false;
                checkBoxFullTSText_1.Enabled = false;
                checkBoxRawTSText_1.Enabled = false;
                radioButtonAppend_1.Enabled = false;
				radioButtonOverwrite_1.Enabled = false;
				radioButtonDayFiles_1.Enabled = false;
				radioButtonHourFiles_1.Enabled = false;
				radioButtonPopName_1.Enabled = false;
				radioButtonLapxmName_1.Enabled = false;
				textBoxOutFolder_1.Enabled = false;
				textBoxSite_1.Enabled = false;
				textBoxSuffix_1.Enabled = false;
			}
		}

		private void checkBoxEnableDiskWrite_2_CheckedChanged(object sender, EventArgs e) {
			if (checkBoxEnableDiskWrite_2.Checked) {
                checkBoxXCorr_2.Enabled = true;
                checkBoxSpectra_2.Enabled = true;
				checkBoxMoments_2.Enabled = true;
				checkBoxFullTS_2.Enabled = true;
				checkBoxRawTS_2.Enabled = true;
				checkBox1TS_2.Enabled = true;
                checkBox1TSText_2.Enabled = true;
                checkBoxFullTSText_2.Enabled = true;
                checkBoxRawTSText_2.Enabled = true;
                radioButtonAppend_2.Enabled = true;
				radioButtonOverwrite_2.Enabled = true;
				radioButtonDayFiles_2.Enabled = true;
				radioButtonHourFiles_2.Enabled = true;
				radioButtonPopName_2.Enabled = true;
				radioButtonLapxmName_2.Enabled = true;
				textBoxOutFolder_2.Enabled = true;
				textBoxSite_2.Enabled = true;
				textBoxSuffix_2.Enabled = true;
			}
			else {
				checkBoxSpectra_2.Enabled = false;
                checkBoxXCorr_2.Enabled = false;
                checkBoxMoments_2.Enabled = false;
				checkBoxFullTS_2.Enabled = false;
				checkBoxRawTS_2.Enabled = false;
				checkBox1TS_2.Enabled = false;
                checkBox1TSText_2.Enabled = false;
                checkBoxFullTSText_2.Enabled = false;
                checkBoxRawTSText_2.Enabled = false;
                radioButtonAppend_2.Enabled = false;
				radioButtonOverwrite_2.Enabled = false;
				radioButtonDayFiles_2.Enabled = false;
				radioButtonHourFiles_2.Enabled = false;
				radioButtonPopName_2.Enabled = false;
				radioButtonLapxmName_2.Enabled = false;
				textBoxOutFolder_2.Enabled = false;
				textBoxSite_2.Enabled = false;
				textBoxSuffix_2.Enabled = false;
			}
		}

		private void checkBox1TS_1_CheckedChanged(object sender, EventArgs e) {
			if (checkBox1TS_1.Checked) {
				checkBoxFullTS_1.Checked = false;
                checkBoxRawTS_1.Checked = false;
			}
		}

		private void checkBoxFullTS_1_CheckedChanged(object sender, EventArgs e) {
			if (checkBoxFullTS_1.Checked) {
				checkBox1TS_1.Checked = false;
                checkBoxRawTS_1.Checked = false;
            }
		}

		private void checkBox1TS_2_CheckedChanged(object sender, EventArgs e) {
			if (checkBox1TS_2.Checked) {
				checkBoxFullTS_2.Checked = false;
                checkBoxRawTS_2.Checked = false;
            }
		}

		private void checkBoxFullTS_2_CheckedChanged(object sender, EventArgs e) {
			if (checkBoxFullTS_2.Checked) {
				checkBox1TS_2.Checked = false;
                checkBoxRawTS_2.Checked = false;
            }
		}

		private void buttonBrowseOutputFolder_1_Click(object sender, EventArgs e) {
			if (Directory.Exists(textBoxOutFolder_1.Text)) {
				folderBrowserDialog1.SelectedPath = textBoxOutFolder_1.Text;
			}
			DialogResult rr = folderBrowserDialog1.ShowDialog();
			if (rr == DialogResult.OK) {
				textBoxOutFolder_1.Text = folderBrowserDialog1.SelectedPath;
			}
		}

        private void buttonBrowseLogFolder_Click(object sender, EventArgs e) {
            if (Directory.Exists(textBoxLogFolder.Text)) {
                folderBrowserDialog1.SelectedPath = textBoxLogFolder.Text;
            }
            DialogResult rr = folderBrowserDialog1.ShowDialog();
            if (rr == DialogResult.OK) {
                textBoxLogFolder.Text = folderBrowserDialog1.SelectedPath;
            }
        }
        
        private void buttonBrowseOutputFolder_2_Click(object sender, EventArgs e) {
			if (Directory.Exists(textBoxOutFolder_2.Text)) {
				folderBrowserDialog1.SelectedPath = textBoxOutFolder_2.Text;
			}
			DialogResult rr = folderBrowserDialog1.ShowDialog();
			if (rr == DialogResult.OK) {
				textBoxOutFolder_2.Text = folderBrowserDialog1.SelectedPath;
			}
		}

		private void radioButtonPopName_1_CheckedChanged(object sender, EventArgs e) {
			DisplayFileName(0);
		}

		private void radioButtonDayFiles_1_CheckedChanged(object sender, EventArgs e) {
			DisplayFileName(0);
		}

		private void radioButtonPopName_2_CheckedChanged(object sender, EventArgs e) {
			DisplayFileName(1);
		}

		private void radioButtonDayFiles_2_CheckedChanged(object sender, EventArgs e) {
			DisplayFileName(1);
		}

		private void checkBoxSpectra_1_CheckedChanged(object sender, EventArgs e) {
			DisplayFileName(0);
            if (checkBoxSpectra_1.Checked) {
                // if writing spectra, cannot write raw ts or xcorr
                checkBoxRawTS_1.Checked = false;
                checkBoxXCorr_1.Checked = false;
            }
        }

        private void checkBoxXCorr_1_CheckedChanged(object sender, EventArgs e) {
            DisplayFileName(0);
            if (checkBoxXCorr_1.Checked) {
                checkBoxRawTS_1.Checked = false;
                // xcorr takes the place of spectra in file, so turn off spectral write
                checkBoxSpectra_1.Checked = false;
            }

        }

        private void checkBoxMoments_1_CheckedChanged(object sender, EventArgs e) {
			DisplayFileName(0);
            if (checkBoxMoments_1.Checked) {
                checkBoxRawTS_1.Checked = false;
            }
        }

		private void checkBoxSpectra_2_CheckedChanged(object sender, EventArgs e) {
			DisplayFileName(1);
            if (checkBoxSpectra_2.Checked) {
                checkBoxRawTS_2.Checked = false;
                checkBoxXCorr_2.Checked = false;
            }
        }

        private void checkBoxXCorr_2_CheckedChanged(object sender, EventArgs e) {
            DisplayFileName(1);
            if (checkBoxXCorr_2.Checked) {
                checkBoxRawTS_2.Checked = false;
                checkBoxSpectra_2.Checked = false;
            }
        }

        private void checkBoxMoments_2_CheckedChanged(object sender, EventArgs e) {
			DisplayFileName(1);
            if (checkBoxMoments_2.Checked) {
                checkBoxRawTS_2.Checked = false;
            }
        }

        private void checkBoxRawTS_1_CheckedChanged(object sender, EventArgs e) {
            DisplayFileName(0);
            if (checkBoxRawTS_1.Checked) {
                // raw ts files contain no other data
                checkBoxMoments_1.Checked = false;
                checkBoxSpectra_1.Checked = false;
                checkBox1TS_1.Checked = false;
                checkBoxFullTS_1.Checked = false;
                checkBoxXCorr_1.Checked = false;
            }
        }

        private void checkBoxRawTS_2_CheckedChanged(object sender, EventArgs e) {
            DisplayFileName(1);
            if (checkBoxRawTS_2.Checked) {
                checkBoxMoments_2.Checked = false;
                checkBoxSpectra_2.Checked = false;
                checkBox1TS_2.Checked = false;
                checkBoxFullTS_2.Checked = false;
                checkBoxXCorr_2.Checked = false;
            }
        }

        private void textBoxSite_1_TextChanged(object sender, EventArgs e) {
            if (textBoxSite_1.Text.Length == 3) {
                DisplayFileName(0);
            }
        }

        private void textBoxSite_2_TextChanged(object sender, EventArgs e) {
            if (textBoxSite_2.Text.Length == 3) {
                DisplayFileName(1);
            }
        }

        private void textBoxSuffix_1_TextChanged(object sender, EventArgs e) {
            if (textBoxSuffix_1.Text.Length == 1) {
                DisplayFileName(0);
            }
        }

        private void textBoxSuffix_2_TextChanged(object sender, EventArgs e) {
            if (textBoxSuffix_2.Text.Length == 1) {
                DisplayFileName(1);
            }
        }



        //
        #endregion Private Windows Component Event Handlers

        #region Private Methods
        //


        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private bool CellIsEmpty(int row, int column) {
            bool isEmpty = false;
            if (dataGridViewSequence.Rows[0].Cells[column].Value != null) {
                if ((dataGridViewSequence.Rows[0].Cells[column].Value.ToString().Trim() == string.Empty)) {
                    isEmpty = true;
                }
            }
            else {
                isEmpty = true;
            }
            return isEmpty;
        }

        /// <summary>
        /// 
        /// </summary>
        private void SaveToParFile() {
            SaveAllTabPages(true);
            string outputFilePathName = _parameters.Source;

            string ext = Path.GetExtension(outputFilePathName);
            if (ext.ToLower() == ".parx") {
				textBoxFileName.Text = outputFilePathName;
				textBoxFileName.SelectionStart = textBoxFileName.TextLength;
				_parameters.WriteToFile(outputFilePathName);
                MessageBoxEx.Show("Parameters saved to file\n" + outputFilePathName, "Saved to File", 1000);
                MessageBeep((int)MessageBoxIcon.Asterisk);
            }
            else {
                MessageBox.Show("Don't know how to save " + ext + " files yet.", "File NOT saved");
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void CheckSelectedGateRange() {
            if (_parameters.ReplayPar.Enabled) {
                // on replay, let user be responsible for hts selected
                return;
            }
            if (checkBoxSelectHts.Checked) {
                // if user is selecting gates to keep
                // make sure the first and last gates are within
                //  the range of actual gates
                int firstGate = (int)numericUpDownGateFirst.Value;
                int lastGate = (int)numericUpDownGateLast.Value;
                int nsamples;
                //npts = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts / 2 + 1;
                bool isOK = int.TryParse(textBoxSweepNPts.Text, out nsamples);
                if (!isOK) {
                    nsamples = 1;
                }
                int ngates = nsamples/2 + 1;
                if ((firstGate > ngates - 1) || (firstGate > lastGate) || (firstGate < 0)) {
                    numericUpDownGateFirst.Value = 0;
                }
                firstGate = (int)numericUpDownGateFirst.Value;
                if ((lastGate > ngates - 1) || (lastGate < 0) || (lastGate < firstGate)) {
                    numericUpDownGateLast.Value = ngates - 1;
                }
            }

        }

		private void DisplayFileName(int index) {

			//SaveOutputPage();

			DateTime dt = DateTime.Now;
			string year = String.Format("{0:D2}", dt.Year % 100);
			string day = String.Format("{0:D3}", dt.DayOfYear);
			string hour = String.Format("{0:D2}", dt.Hour);

			if (index == 0) {
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 0) {
					string ext;
                    ext = ".mom";
					if (checkBoxMoments_1.Checked && !checkBoxSpectra_1.Checked && !checkBoxXCorr_1.Checked) {
						ext = ".mom";
					}
                    else if (checkBoxSpectra_1.Checked) {
                        ext = ".spc";
                    }
                    else if (checkBoxXCorr_1.Checked) {
                        ext = ".spc";
                    }
                    else if (checkBoxRawTS_1.Checked) {
                        ext = ".raw.ts";
                    }
					string suffix = textBoxSuffix_1.Text;
					if (suffix == null) {
						textBoxSuffix_1.Text = suffix = "a";
						//FillOutputPage();
					}
					else {
						if (suffix.Length > 1) {
							suffix = suffix.Substring(0, 1);
						}
						if (suffix == String.Empty) {
							textBoxSuffix_1.Text = suffix = "a";
							//FillOutputPage();
						}
					}
					string site = textBoxSite_1.Text;
					if (site == null) {
						site = "xxx";
						//FillOutputPage();
					}
					else {
						if (site.Length > 3) {
							site = site.Substring(0, 3);
						}
						if (site.Length < 3) {
							site = "xxx";
							//FillOutputPage();
						}
					}
					textBoxSite_1.Text = site;
					bool lapxm = radioButtonLapxmName_1.Checked;
					String fileName;
					if (lapxm) {
						fileName = "D" + site + year + day;
						if (radioButtonHourFiles_1.Checked) {
							fileName += hour;
						}
					}
					else {
						fileName = "D" + year + day;
						if (radioButtonHourFiles_1.Checked) {
							char letter = 'c';
							int code = letter + dt.Hour;
							letter = (char)code;
							suffix = letter.ToString();
						}
					}
                    string presuffix = "";
                    if (checkBoxXCorr_1.Checked) {
                        presuffix = "ac";
                    }

					fileName += (presuffix + suffix + ext);
					labelOutFile_1.Text = fileName;

				}
				
			}
			else if (index == 1) {
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 1) {
                    string ext;
                    ext = ".mom";
                    if (checkBoxMoments_2.Checked && !checkBoxSpectra_2.Checked && !checkBoxXCorr_2.Checked) {
                        ext = ".mom";
                    }
                    else if (checkBoxSpectra_2.Checked) {
                        ext = ".spc";
                    }
                    else if (checkBoxXCorr_2.Checked) {
                        ext = ".cor";
                    }
                    else if (checkBoxRawTS_2.Checked) {
                        ext = ".raw.ts";
                    }
                    string suffix = textBoxSuffix_2.Text;
					if (suffix == null) {
						suffix = "a";
						//FillOutputPage();
					}
					else {
						if (suffix.Length > 1) {
							suffix = suffix.Substring(0, 1);
						}
						if (suffix == String.Empty) {
							suffix = "a";
							//FillOutputPage();
						}
					}
					textBoxSuffix_2.Text = suffix;
					string site = textBoxSite_2.Text;
					if (site == null) {
						site = "xxx";
						//FillOutputPage();
					}
					else {
						if (site.Length > 3) {
							site = site.Substring(0, 3);
						}
						if (site.Length < 3) {
							site = "xxx";
							//FillOutputPage();
						}
					}
					textBoxSite_2.Text = site;
					bool lapxm = radioButtonLapxmName_2.Checked;
					String fileName;
					if (lapxm) {
						fileName = "D" + site + year + day;
						if (radioButtonHourFiles_2.Checked) {
							fileName += hour;
						}
					}
					else {
						fileName = "D" + year + day;
						if (radioButtonHourFiles_2.Checked) {
							char letter = 'c';
							int code = letter + dt.Hour;
							letter = (char)code;
							suffix = letter.ToString();
						}
					}
					fileName += suffix + ext;
					labelOutFile_2.Text = fileName;
				}
			}
		}


		private void textBoxDDSRefClockMHz_TextChanged(object sender, EventArgs e) {
			UpdateDDSDisplay();
		}

		private void comboBoxDDSMultiplier_SelectedIndexChanged(object sender, EventArgs e) {
			UpdateDDSDisplay();
        }

		private void UpdateDDSDisplay() {
			bool isOK;
			double refClockMHz;
			isOK = double.TryParse(textBoxDDSRefClockMHz.Text, out refClockMHz);
			int index = comboBoxDDSMultiplier.SelectedIndex;
			int multiplier = index + 3;
			if (multiplier < 4) {
				multiplier = 1;
			}
			double sysClock = refClockMHz * multiplier;
			double syncClock = sysClock / 4.0;
			double syncPeriodNs = 0.0;
			if (syncClock != 0.0) {
				syncPeriodNs = 1000.0 / syncClock;
			}
			labelDDSSyncClockMHz.Text = syncClock.ToString();
			labelDDSSysClockMHz.Text = sysClock.ToString();
			labelDDSSyncClockPeriodNsec.Text = syncPeriodNs.ToString();

            UpdateDDSValues();
            //CalculateSweepRegisterValues();
        }


        private void dataGridViewBeamPar_CellContentClick(object sender, DataGridViewCellEventArgs e) {

        }

        private void textBoxFileName_TextChanged(object sender, EventArgs e) {
            string fileName = Path.GetFileName(textBoxFileName.Text);
            this.Text = "PopNSetup: " + fileName;
        }


        private void textBoxFmCwIppUSec_TextChanged(object sender, EventArgs e) {
            RecalculateNyquist();
            RecalculateDwellTime();
            UpdateDDSDisplay();
        }

        private void buttonMLBrowseOutput_Click(object sender, EventArgs e) {
            if (Directory.Exists(textBoxMLOutputFolder.Text)) {
                folderBrowserDialog1.SelectedPath = textBoxMLOutputFolder.Text;
                //openFileDialog1.InitialDirectory = Path.GetDirectoryName(textBoxMLOutputFolder.Text);
            }
            DialogResult rr = folderBrowserDialog1.ShowDialog();
            if (rr == DialogResult.OK) {
                textBoxMLOutputFolder.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void buttonMLBrowseLogFolder_Click(object sender, EventArgs e) {
            if (Directory.Exists(textBoxMLLogFolder.Text)) {
                folderBrowserDialog1.SelectedPath = textBoxMLLogFolder.Text;
                //openFileDialog1.InitialDirectory = Path.GetDirectoryName(textBoxMLLogFolder.Text);
            }
            DialogResult rr = folderBrowserDialog1.ShowDialog();
            if (rr == DialogResult.OK) {
                textBoxMLLogFolder.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void buttonBrowseReplay_Click(object sender, EventArgs e) {
            if (textBoxReplayFilePath.Text.Trim() != String.Empty) {
                try {
                    string currentFolder = Path.GetDirectoryName(textBoxReplayFilePath.Text);
                    if (Directory.Exists(currentFolder)) {
                        openFileDialog1.InitialDirectory = currentFolder;
                        openFileDialog1.FileName = Path.GetFileName(textBoxReplayFilePath.Text);
                    }
                }
                catch { };
            }
            DialogResult rr = openFileDialog1.ShowDialog();
            if (rr == DialogResult.OK) {
                textBoxReplayFilePath.Text = openFileDialog1.FileName;
            }
            labelReplayFile.Text = Path.GetFileName(textBoxReplayFilePath.Text);
        }

        private void textBoxFileName_SizeChanged(object sender, EventArgs e) {
            textBoxFileName.SelectionStart = 0;
            textBoxFileName.SelectionStart = textBoxFileName.TextLength;

        }

        private void checkBoxReplay_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxReplay.Checked) {
                labelReplayWarning.Text = "REPLAY ON!";
                labelReplayWarning.BackColor = Color.Red;
            }
            else {
                labelReplayWarning.Text = "";
                labelReplayWarning.BackColor = Color.Transparent;
            }
        }

        private void checkBoxMLUseRangeCorrectedMinSnr_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxMLUseRangeCorrectedMinSnr.Checked) {
                textBoxMLMinSnrRain.Enabled = false;
                textBoxMLMinSnrBb.Enabled = false;
                textBoxMLDvvOnlyMinSnr.Enabled = false;
            }
            else {
                textBoxMLMinSnrRain.Enabled = true;
                textBoxMLMinSnrBb.Enabled = true;
                textBoxMLDvvOnlyMinSnr.Enabled = true;
            }
        }

        /*
        private void radioButtonFreqOffset_CheckedChanged(object sender, EventArgs e) {
            // select which way to imput offset: by freq or gate
            if (radioButtonFreqOffset.Checked) {
                textBoxFmCwOffsetHz.Enabled = true;
                textBoxFmCwOffsetGate.Enabled = false;
                // process freq value
                textBoxFmCwOffsetHz_TextChanged(null, null);
            }
            else {
                textBoxFmCwOffsetHz.Enabled = false;
                textBoxFmCwOffsetGate.Enabled = true;
                // process gate value
                textBoxFmCwOffsetGate_TextChanged(null, null);
            }
        }
         * */

        /*
        private void buttonUseDDSValues_Click(object sender, EventArgs e) {
            double rampRate;
            double offset;

            if (_rampIsOriginal) {
                double.TryParse(labelDDSSweepRate0.Text, out rampRate);
                double.TryParse(textBoxFmCwSweepRate.Text, out _originalRampRate);
                double.TryParse(labelDDSFreqOffsetHz.Text, out offset);
                double.TryParse(textBoxFmCwOffsetHz.Text, out _originalOffset);
                textBoxFmCwSweepRate.Text = rampRate.ToString("F3");
                textBoxFmCwOffsetHz.Text = offset.ToString("F2");
                _rampIsOriginal = false;
                //buttonUseDDSValues.Text = "Use Original Sweep";
            }
            else {
                textBoxFmCwSweepRate.Text = _originalRampRate.ToString("F5");
                textBoxFmCwOffsetHz.Text = _originalOffset.ToString("F2");
                _rampIsOriginal = true;
                //buttonUseDDSValues.Text = "Use DDS Values";
            }
        }
         * */

        private void textBoxDopNPts_TextChanged(object sender, EventArgs e) {
            RecalculateDwellTime();
        }

        private void textBoxDopNSpec_TextChanged(object sender, EventArgs e) {
            RecalculateDwellTime();
        }

        private void RecalculateDwellTime() {
            double ippUsec = 0.0;
            int npts = 0;
            int nspec = 0;
            string label = "";
            if (double.TryParse(textBoxFmCwIppUSec.Text, out ippUsec)) {
                if (Int32.TryParse(textBoxDopNPts.Text, out npts)) {
                    if (Int32.TryParse(textBoxDopNSpec.Text, out nspec)) {
                        double dwell = ippUsec / 1.0e6 * npts * nspec;
                        label = dwell.ToString("F1");
                    }
                }
            }
            labelDwellSec.Text = label;
        }

        //
        #endregion Private Methods


        #region TEST DaqBoard
        //
        //private DaqBoard3000USB _daqBoard;
        private const int NREPS = 10;
        private int _iReps;
        //private bool _dataReady;
        private int nIpp = 1024;

/*
        /// <summary>
        /// This acquisition test simply starts a timer,
        ///     which then calls daq.Start().
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonAcquire_Click(object sender, EventArgs e) {
            
            int nSamples = _parameters.GetSamplesPerIPP(0);
            int threadID = Thread.CurrentThread.ManagedThreadId;
            if (_daqBoard == null) {
                _daqBoard = new DaqBoard3000USB();
            }
            _daqBoard.NDataSamplesPerDevice = nSamples * nIpp;
            Console.Beep();
            _iReps = 0;
            timer1.Interval = 100;
            timer1.Enabled = true;
            
            return;
        }

        /// <summary>
        /// This acquire test calls Daq.Start() from a for loop;
        ///     and uses the completion event to signal when the
        ///     next iteration can start.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        
        private void buttonAcquire2_Click(object sender, EventArgs e) {
            int threadID = Thread.CurrentThread.ManagedThreadId;
            int nSamples = _parameters.GetSamplesPerIPP(0);
            if (_daqBoard == null) {
                _daqBoard = new DaqBoard3000USB();
                //_daqBoard.AcquisitionCompleteEvent += new EventHandler(AcqCompleteHandler);
            }
            _daqBoard.NDataSamplesPerDevice = nSamples * nIpp;
            Console.Beep();
            for (int i = 0; i < NREPS; i++) {
                //_dataReady = false;
                _daqBoard.Start();
                //while (!_dataReady) {
                while (!_daqBoard.DataIsAvailable) {
                    // DoEvents is required to make UI responsive,
                    //  but acquisition is interrupted while UI updating
                    Application.DoEvents();
                    Thread.Sleep(10);
                }
                // plot last one
                if (i == NREPS - 1) {
                    Console.Beep();
                    PlotData();
                }
            }
            
            return;
        }
*/
/*
        private void PlotData() {
            int nSamples = _parameters.GetSamplesPerIPP(0);
            double[] x = new double[nSamples * 2];
            double[] y = new double[nSamples * 2];
            for (int j = 0; j < nSamples; j++) {
                y[j] = (float)_daqBoard.DataArray.GetValue(j);
                x[j] = j;
            }
            // and plot last IPP
            for (int j = 0; j < nSamples; j++) {
                int jj = j + nSamples * (nIpp - 1);
                y[j + nSamples] = (float)_daqBoard.DataArray.GetValue(jj);
                x[j + nSamples] = j + nSamples;
            }
            QuickPlotZ plot = new QuickPlotZ();
            plot.AddCurve("", x, y, Color.Blue);
            plot.Display();
        }


        private void AcqCompleteHandler(Object sender, EventArgs args) {
            if (sender is DaqBoard3000USB) {
                int threadID = Thread.CurrentThread.ManagedThreadId;
                DaqBoard3000USB daqBoard = (DaqBoard3000USB)sender;
                int count = daqBoard.DataArray.Length;
                //Console.Beep();
                if (count != daqBoard.NDataSamplesPerDevice) {
                    throw new ApplicationException("Not all samples received from DAQ");
                }
                //_dataReady = true;
            }
        }

        private void timer1_Tick(object sender, EventArgs e) {
            timer1.Stop();
            if (_iReps == 0) {
                _iReps++;
                _daqBoard.Start();
                timer1.Enabled = true;
                return;
            }
            else if (_iReps <= NREPS) {
                if (_daqBoard.DataIsAvailable) {
                    if (_iReps != NREPS) {
                        _iReps++;
                        _daqBoard.Start();
                        timer1.Enabled = true;
                        return;
                    }
                    else {
                        Console.Beep();
                        timer1.Stop();
                        PlotData();
                        return;
                    }
                }
                else {
                    // daq not done
                    timer1.Enabled = true;
                    return;
                }
            }
        }
*/

        //
        #endregion TEST DaqBoard

        #region Test Pulses
        //
        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonPBX_Click(object sender, EventArgs e) {
            /*
            SaveToParFile();
			//buttonBWToggle_Click(null, null);
            if (!_parameters.Debug.NoPbx) {
                if (_pulseBox == null) {
                    try {
                        _pulseBox = PulseGeneratorFactory.GetNewPulseGenDevice();
                    }
                    catch {
                        _pulseBox = null;
                    }
                }
                if ( _pulseBox == null || !_pulseBox.Exists()) {
                    MessageBox.Show("pbx does not exist or cannot be accessed.");
                    return;
                }
                else {
                    bool isPB = _pulseBox is PulseBlaster;
                    bool isPBX = _pulseBox is PbxControllerCard;
                    int status1 = _pulseBox.ReadStatus();
                    bool isBusy = _pulseBox.IsBusy();
                    if (isBusy) {
                        _pulseBox.StopPulses();
                    }
                    int status2 = _pulseBox.ReadStatus();

                    if (isPB || _parameters.SystemPar.RadarPar.FmCwParSet[0].AD9959Enabled) {
                        try {
                            _DDS = new AD9959EvalBd(_parameters, true);
                        }
                        catch {
                            _DDS = null;
                        }
                        if (_DDS != null) {
                            _DDS.StartAllFrequencies();
                        }
                        else if (isPB) {
                            MessageBox.Show("PulseBlaster needs DDS clock. ");
                            return;
                        }
                    }

                    _pulseBox.Reset();
                    _pulseBox.Setup(_parameters, 0);
                    int status3 = _pulseBox.ReadStatus();
                    bool isBusy3 = _pulseBox.IsBusy();
                }
            }
            else {
                //_pulseBox = new PbxControllerCard();
            }

			//MessageBox.Show("Sweep started, DDS in PC mode now. Click for manual mode.");
			//_AD9959EvalBd.SetPCMode(false);

			//buttonBWToggle_Click(null, null);
            */
        }

		private void buttonBWToggle_Click(object sender, EventArgs e) {

            // POPREV: function modified in rev 3.13 to show DDS ramp frequency register values

            /*
            AD9959EvalBd dds;
            CalculateSweepRegisterValues(out dds);

            double startF = dds.SweepStartFreq0MHz;
            double endF = dds.SweepEndFreq0MHz;
            double rampF = dds.SweepDeltaFreq0MHz;
            double rampT = dds.SweepDeltaTime0Usec;
            double newF, newT;

            string sVal = AD9959EvalBd.GetFreqBinaryString0(startF, dds.SysClockMHz, out newF);
            int regVal = AD9959EvalBd.BinString2Int0(sVal);

            sVal = AD9959EvalBd.GetFreqBinaryString0(endF, dds.SysClockMHz, out newF);
            int regValEnd = AD9959EvalBd.BinString2Int0(sVal);

            sVal = AD9959EvalBd.GetFreqBinaryString0(rampF, dds.SysClockMHz, out newF);
            int regValDeltaF = AD9959EvalBd.BinString2Int0(sVal);

            sVal = AD9959EvalBd.GetTimeBinaryString0(rampT, dds.SysClockMHz, out newT, 0);
            int regValDeltaT = AD9959EvalBd.BinString2Int0(sVal);

            sVal = AD9959EvalBd.GetFreqBinaryString0(dds.SweepStartFreq1MHz, dds.SysClockMHz, out newF);
            int regVal1 = AD9959EvalBd.BinString2Int0(sVal);

            sVal = AD9959EvalBd.GetFreqBinaryString0(dds.SweepEndFreq1MHz, dds.SysClockMHz, out newF);
            int regValEnd1 = AD9959EvalBd.BinString2Int0(sVal);

            sVal = AD9959EvalBd.GetFreqBinaryString0(dds.SweepDeltaFreq1MHz, dds.SysClockMHz, out newF);
            int regValDeltaF1 = AD9959EvalBd.BinString2Int0(sVal);

            sVal = AD9959EvalBd.GetTimeBinaryString0(dds.SweepDeltaTime0Usec, dds.SysClockMHz, out newT, 0);
            int regValDeltaT1 = AD9959EvalBd.BinString2Int0(sVal);

            MessageBox.Show("Start0: " + regVal.ToString() +
                            "\nEnd0:  " + regValEnd.ToString() +
                            "\nDeltaF0:  " + regValDeltaF.ToString() +
                            "\nDeltaT0:  " + regValDeltaT.ToString() +
                            "\n\nStart1: " + regVal1.ToString() +
                            "\nEnd1:  " + regValEnd1.ToString() +
                            "\nDeltaF1:  " + regValDeltaF1.ToString() +
                            "\nDeltaT1:  " + regValDeltaT1.ToString(),
                            "DDS Registers");
             * */

		}

		//////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonStopPbx_Click(object sender, EventArgs e) {
            /*
            if (_pulseBox == null) {
                _pulseBox = new PbxControllerCard();
            }
            _pulseBox.Reset();
			if (_DDS != null) {
				_DDS = new AD9959EvalBd(100.0, 5, 1000, true);
                _DDS.ResetDDS();
                _DDS = null;
			}
             * */
		}

        private void buttonAllowEdit_Click(object sender, EventArgs e) {

        }

        private void checkBoxApplyFilterCorr_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxApplyFilterCorr.Checked) {
                radioButtonFreqResp.Enabled = true;
                radioButtonCoeff.Enabled = true;
                comboBoxFilterFile.Enabled = true;
            }
            else {
                radioButtonFreqResp.Enabled = false;
                radioButtonCoeff.Enabled = false;
                comboBoxFilterFile.Enabled = false;
            }
        }

        private void radioButtonFreqResp_CheckedChanged(object sender, EventArgs e) {
            if (radioButtonFreqResp.Checked) {
                comboBoxFilterFile.Items.Clear();
                //string appPath = _filterFolderFullPath;
                string[] filterFileNames;
                if (!Directory.Exists(_filterFolderFullPath)) {
                    _filterFolderFullPath = Application.StartupPath;
                    _filterFolderRelPath = ".";
                }
                filterFileNames = Directory.GetFiles(_filterFolderFullPath, "*.gain");
                foreach (string fullFileName in filterFileNames) {
                    string relFileName = Path.Combine(_filterFolderRelPath, Path.GetFileName(fullFileName));
                    comboBoxFilterFile.Items.Add(relFileName);
                }
                string parFilterFile = _parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile;
                parFilterFile = Path.GetFileName(parFilterFile);
                if (!comboBoxFilterFile.Text.ToLower().EndsWith("gain")) {
                    if (comboBoxFilterFile.Items.Count == 1) {
                        comboBoxFilterFile.Text = (string)comboBoxFilterFile.Items[0];
                    }
                    else if ((comboBoxFilterFile.Items.Count > 1) && (parFilterFile != null) &&
                            (comboBoxFilterFile.Items.Contains(_parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile))) {
                        comboBoxFilterFile.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile;
                    }
                    else {
                        // make user choose new file
                        comboBoxFilterFile.Text = "--Choose *.gain file--";
                    }
                }
            }
            else {
                comboBoxFilterFile.Items.Clear();
                string[] filterFileNames;
                if (!Directory.Exists(_filterFolderFullPath)) {
                    _filterFolderFullPath = Application.StartupPath;
                    _filterFolderRelPath = ".";
                }
                string appPath = Application.StartupPath;
                filterFileNames = Directory.GetFiles(_filterFolderFullPath, "*.coeff");
                foreach (string fullFileName in filterFileNames) {
                    string relFileName = Path.Combine(_filterFolderRelPath, Path.GetFileName(fullFileName));
                    comboBoxFilterFile.Items.Add(relFileName);
                }
                string parFilterFile = _parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile;
                if (!comboBoxFilterFile.Text.ToLower().EndsWith("coeff")) {
                    if (comboBoxFilterFile.Items.Count == 1) {
                        comboBoxFilterFile.Text = (string)comboBoxFilterFile.Items[0];
                    }
                    else if ((comboBoxFilterFile.Items.Count > 1) && (parFilterFile != null) &&
                            (comboBoxFilterFile.Items.Contains(_parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile))) {
                        comboBoxFilterFile.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile;
                    }
                    else {
                        // make user choose new file
                        comboBoxFilterFile.Text = "--Choose *.coeff file--";
                    }
                }
            }
        }

        private void comboBoxFilterFile_DropDown(object sender, EventArgs e) {
            radioButtonFreqResp_CheckedChanged(null, null);
        }

        private void textBoxFmCwSysDelayNs_TextChanged(object sender, EventArgs e) {
            RecalculateRangeRes();
        }

        private void checkBoxFmCwPostBlankAuto_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxFmCwPostBlankAuto.Checked) {
                textBoxFmCwPostBlankUs.Enabled = false;
                textBoxFmCwPostBlankUs.BackColor = Color.OldLace;
                double value;
                bool isOK = double.TryParse(textBoxFmCwPostBlankUs.Text, out value);
                if (isOK) {
                    _savedPostBlankNs = (int)(value * 1000.0 + 0.5);
                }
                RecalculateLastSample();
            }
            else {
                textBoxFmCwPostBlankUs.Enabled = true;
                textBoxFmCwPostBlankUs.Text = (_savedPostBlankNs/1000.0).ToString("f3");
                textBoxFmCwPostBlankUs.BackColor = Color.White;
            }
        }

        private void textBoxDDS3FreqHz_TextChanged(object sender, EventArgs e) {
            double freq;
            double.TryParse(textBoxDDS3FreqHz.Text, out freq);
            int reg = AD9959EvalBd.FreqRegFromFreqMHz(freq / 1.0e6, 400.0);
            toolTip1.SetToolTip(textBoxDDS3FreqHz, "register: " + reg.ToString());
        }

        private void textBoxDDS4FreqHz_TextChanged(object sender, EventArgs e) {
            double freq;
            double.TryParse(textBoxDDS4FreqHz.Text, out freq);
            int reg = AD9959EvalBd.FreqRegFromFreqMHz(freq / 1.0e6, 400.0);
            toolTip1.SetToolTip(textBoxDDS4FreqHz, "register: " + reg.ToString());
        }

        private void timerCheckChanges_Tick(object sender, EventArgs e) {
            // TODO get rid of return in timerCheckChanges_Tick:
            SaveAllTabPages(false);
            if (!_parameters.Equals(_savedParameters)) {
                labelChangesNotSaved.Text = "NOT SAVED";
                if (labelChangesNotSaved.BackColor != Color.OrangeRed) {
                    labelChangesNotSaved.BackColor = Color.OrangeRed;
                    labelChangesNotSaved.ForeColor = Color.Yellow;
                }
                else {
                    labelChangesNotSaved.BackColor = Color.Yellow;
                    labelChangesNotSaved.ForeColor = Color.Black;
                }
                labelChangesNotSaved.Visible = true;
                buttonCancel.Enabled = true;
                buttonCancel.Text = "Undo All Changes";
            }
            else {
                labelChangesNotSaved.Visible = false;
                if (!_parameters.Equals(_backupParameters)) {
                    buttonCancel.Text = "Redo Changes";
                    buttonCancel.Enabled = true;
                }
                else {
                    buttonCancel.Enabled = false;
                }
            }
        }

        private void buttonBrowseFilters_Click(object sender, EventArgs e) {

            //comboBoxFilterFile.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].FilterFile;

            string filterFileName = comboBoxFilterFile.Text;
            string filterFolder = Path.GetDirectoryName(filterFileName);

            Tools.GetFullRelPath(_currentDirectory, filterFolder, out _filterFolderFullPath, out _filterFolderRelPath);

            if (Directory.Exists(_filterFolderFullPath)) {
                folderBrowserDialog1.SelectedPath = _filterFolderFullPath;
            }
            DialogResult rr = folderBrowserDialog1.ShowDialog();
            if (rr == DialogResult.OK) {
                filterFolder = folderBrowserDialog1.SelectedPath;
            }
            _filterFolderFullPath = filterFolder;
            _filterFolderRelPath = Tools.GetRelativePath(_currentDirectory, filterFolder);
        }

        /// <summary>
        /// This method called before button is actually clicked and therefore
        ///     before focus leaves the tab page;
        /// buttonSave_Enter and buttonSave_Click are called after focus leaves
        ///     the tab page, so focusChanged is called and those 2 button methods
        ///     do not end up being executed.
        /// See tabPageFmCw_Leave method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSave_MouseEnter(object sender, EventArgs e) {
            _saveButtonClicked = true;  // see tabPageFmCw_Leave method etc.
            //TextFile.WriteLineToFile("Dave.txt","Save_MouseEnter");
        }

        private void buttonSaveAs_MouseEnter(object sender, EventArgs e) {
            _saveButtonClicked = true;  // see tabPageFmCw_Leave method etc.
        }

        private void buttonSave_MouseLeave(object sender, EventArgs e) {
            _saveButtonClicked = false;
            //TextFile.WriteLineToFile("Dave.txt", "Save_MouseLeave");
        }

        private void buttonSaveAs_MouseLeave(object sender, EventArgs e) {
            _saveButtonClicked = false;
            
        }

        private void timer1_Tick(object sender, EventArgs e) {
            // TODO timer1_Tick not used
        }

        private void textBoxTxFreq_TextChanged(object sender, EventArgs e) {
            bool isOK = double.TryParse(textBoxTxFreq.Text, out _parameters.SystemPar.RadarPar.TxFreqMHz);
            RecalculateNyquist();
        }

        private void buttonCnsBrowseFolder_Click(object sender, EventArgs e) {

            string outFolder = textBoxCnsFileFolder.Text;
            if (Directory.Exists(outFolder)) {
                folderBrowserDialog1.SelectedPath = outFolder;
            }
            DialogResult rr = folderBrowserDialog1.ShowDialog();
            if (rr == DialogResult.OK) {
                outFolder = folderBrowserDialog1.SelectedPath;
            }
            textBoxCnsFileFolder.Text = outFolder;

        }

        //
        #endregion  Test Pulses

        //

       
    }
}
