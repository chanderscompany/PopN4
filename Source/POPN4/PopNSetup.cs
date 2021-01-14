using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using DACarter.NOAA.Hardware;
using DACarter.PopUtilities;
using DACarter.Utilities.Graphics;
using DACarter.Utilities;

namespace POPN {
	public partial class PopNSetup : Form {


        #region Constructor
        //
        public PopNSetup() {
            InitializeComponent();
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
        }
        //
        #endregion Constructor

        #region Private Fields
        //
        private DACarter.PopUtilities.PopParameters _parameters;
        private PopParameters _backupParameters;
        private PbxControllerCard _pulseBox;
		private AD9959EvalBd _AD9959EvalBd;
		private double _originalRampRate, _originalOffset;
		private bool _rampIsOriginal;

		private double _lastPosOffset, _lastNegOffset, _lastOffset;
		private int _lastPosFirstGate, _lastPosLastGate;
		private int _lastNegFirstGate, _lastNegLastGate;
		private bool _usesMaxGate;
		private int _maxGate;

		// when in FMCW mode, this is the 
		//	pulsed Doppler sample delay that would produce the same first gate range:
		private int _virtualRangeDelayNs = -999;
		//
        #endregion

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
                _parameters = value;
                if (value != null) {
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

			comboBoxFilterCoeff.Items.Clear();
			string appPath = Application.StartupPath;
			string[] filterFileNames = Directory.GetFiles(appPath, "*.coeff");
			foreach (string fullPath in filterFileNames) {
				comboBoxFilterCoeff.Items.Add(Path.GetFileName(fullPath));
			}


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
        private void SaveAllTabPages() {
            SaveDwellPage();
            SaveSystemPage();
			SaveProcessingPage();
			SaveFmCwPage();
			SaveOutputPage();
			SaveMeltingLayerPage();
			//_parameters.MomentExcludeIntervals = new PopParameters.MomentExcludeInterval[2];
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
            if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCw) {
                labelRadartype.Text = "FM CW Doppler Radar";
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

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void FillSystemPage() {

            numericUpDownDirections.Value = _parameters.ArrayDim.MAXDIRECTIONS;
            numericUpDownBeamSeq.Value = _parameters.ArrayDim.MAXBEAMS;
            if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCw) {
                radioButtonFmCwType.Checked = true;
            }
            else if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.PulsedTx) {
                radioButtonPulsedType.Checked = true;
            }
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
            textBoxMaxDutyCycle.Text = ((int)(_parameters.SystemPar.RadarPar.MaxTxDutyCycle * 100.0 + 0.5)).ToString();
            textBoxMaxTx.Text = _parameters.SystemPar.RadarPar.MaxTxLengthUsec.ToString();
            textBoxMinIpp.Text = _parameters.SystemPar.RadarPar.MinIppUsec.ToString();

            textBoxPbxClock.Text = _parameters.SystemPar.RadarPar.PBConstants.PBClock.ToString();
            textBoxPreTR.Text = _parameters.SystemPar.RadarPar.PBConstants.PBPreTR.ToString();
            textBoxPostTR.Text = _parameters.SystemPar.RadarPar.PBConstants.PBPostTR.ToString();
            textBoxPreBlank.Text = _parameters.SystemPar.RadarPar.PBConstants.PBPreBlank.ToString();
            textBoxPostBlank.Text = _parameters.SystemPar.RadarPar.PBConstants.PBPostBlank.ToString();
            textBoxSync.Text = _parameters.SystemPar.RadarPar.PBConstants.PBSynch.ToString();

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

            // fill in RxBw Table
            for (int i = 0; i < _parameters.ArrayDim.MAXBW; i++) {
                dataGridViewRxBw.Rows[i].Cells[0].Value = _parameters.SystemPar.RadarPar.RxBw[i].BwPwNs.ToString("F0");
                dataGridViewRxBw.Rows[i].Cells[1].Value = _parameters.SystemPar.RadarPar.RxBw[i].BwDelayNs.ToString("F0");
            }
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void SaveSystemPage() {
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
            isOK = int.TryParse(textBoxPostBlank.Text, out _parameters.SystemPar.RadarPar.PBConstants.PBPostBlank);
            isOK = int.TryParse(textBoxPostTR.Text, out _parameters.SystemPar.RadarPar.PBConstants.PBPostTR);
            isOK = int.TryParse(textBoxPreBlank.Text, out _parameters.SystemPar.RadarPar.PBConstants.PBPreBlank);
            isOK = int.TryParse(textBoxPreTR.Text, out _parameters.SystemPar.RadarPar.PBConstants.PBPreTR);
            isOK = int.TryParse(textBoxSync.Text, out _parameters.SystemPar.RadarPar.PBConstants.PBSynch);
            // Directions
            isOK = int.TryParse(numericUpDownDirections.Text, out _parameters.ArrayDim.MAXDIRECTIONS);
            for (int i = 0; i < _parameters.ArrayDim.MAXDIRECTIONS; i++) {
                _parameters.SystemPar.RadarPar.BeamDirections[i].Label = (string)dataGridViewDirections.Rows[i].Cells[0].Value;
                isOK = double.TryParse((string)dataGridViewDirections.Rows[i].Cells[1].Value, out _parameters.SystemPar.RadarPar.BeamDirections[i].Azimuth);
                isOK = double.TryParse((string)dataGridViewDirections.Rows[i].Cells[2].Value, out _parameters.SystemPar.RadarPar.BeamDirections[i].Elevation);
                isOK = int.TryParse((string)dataGridViewDirections.Rows[i].Cells[3].Value, out _parameters.SystemPar.RadarPar.BeamDirections[i].SwitchCode);
            }
            // RxBw
            for (int i = 0; i < _parameters.ArrayDim.MAXBW; i++) {
                isOK = double.TryParse((string)dataGridViewRxBw.Rows[i].Cells[0].Value, out value);
                _parameters.SystemPar.RadarPar.RxBw[i].BwPwNs = (int)(value + 0.5);
                isOK = double.TryParse((string)dataGridViewRxBw.Rows[i].Cells[1].Value, out value);
				_parameters.SystemPar.RadarPar.RxBw[i].BwDelayNs = (int)(value + 0.5);
            }

        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void FillFmCwPage() {

			labelFmCwSysDelayNs.Text = _parameters.SystemPar.RadarPar.RxBw[0].BwDelayNs.ToString();

			// AD9959 group
			checkBoxEnableDDS.Checked = _parameters.SystemPar.RadarPar.FmCwParSet[0].AD9959Enabled;
			textBoxDDSRefClockMHz.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDSRefClockMHz.ToString();
			int index = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDSMultiplier - 3;
			if (index < 0) {
				index = 0;
			}
			comboBoxDDSMultiplier.SelectedIndex = index;

            // TxSweep group
            textBoxFmCwIppUSec.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].IppMicroSec.ToString();
			textBoxFmCwSweepCenterMHz.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepCenterFreqMHz.ToString();
			double rampRate = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepRateHzUSec;
			
            textBoxFmCwSweepRate.Text = rampRate.ToString();
			textBoxFmCwTimeStepClocks.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepTimeStepClocks.ToString();
            //textBoxFmCwTimeStepUSec.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepTimeIntervalUSec.ToString();
            textBoxFmCwOffsetHz.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepOffsetHz.ToString();

            // Sample parameters group
            textBoxSweepNPts.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts.ToString();
            textBoxSweepNSpec.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNSpec.ToString();
            textBoxSweepSampleDelay.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleDelayNs.ToString();
            textBoxSweepSpacing.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleSpacingNs.ToString();
            if (_parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleOverlap) {
                checkBoxSampleOverlap.Checked = true;
            }
            else {
                checkBoxSampleOverlap.Checked = false;
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
            textBoxRangeOffset.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].RangeOffsetM.ToString();
            int heightsCalculated = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts / 2 + 1;
            labelHtsCalculated.Text = heightsCalculated.ToString();
			if (_parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerDcFilter) {
				checkBoxDcFilDoppler.Checked = true;
			}
			else {
				checkBoxDcFilDoppler.Checked = false;
			}

			if (_parameters.SystemPar.RadarPar.FmCwParSet[0].InputSampleUnitsIsRaw) {
				radioButtonRawSamples.Checked = true;
			}
			else {
				radioButtonVoltSamples.Checked = true;
			}

			comboBoxFilterCoeff.Text = _parameters.SystemPar.RadarPar.FmCwParSet[0].FilterCoeffFile;

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

			if (_parameters.SystemPar.RadarPar.FmCwParSet[0].ApplyFilterCorrection) {
				checkBoxApplyFilterCorr.Checked = true;
			}
			else {
				checkBoxApplyFilterCorr.Checked = false;
			}

			RecalculateRangeRes();
			RecalculateLastSample();
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void SaveFmCwPage() {
            bool isOK;
			bool isFmCw;

			if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCw) {
				isFmCw = true;
			}
			else {
				isFmCw = false;
			}

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

            // TxSweep group
            isOK = double.TryParse(textBoxFmCwIppUSec.Text.ToString(),
                                out _parameters.SystemPar.RadarPar.FmCwParSet[0].IppMicroSec);
			isOK = double.TryParse(textBoxFmCwSweepCenterMHz.Text,
								out _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepCenterFreqMHz);
            isOK = double.TryParse(textBoxFmCwSweepRate.Text.ToString(),
                                out _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepRateHzUSec);
            isOK = double.TryParse(textBoxFmCwOffsetHz.Text.ToString(),
                                out _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepOffsetHz);
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
            if (checkBoxSampleOverlap.Checked) {
                _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleOverlap = true;
            }
            else {
                _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleOverlap = false;
            }
            foreach (string name in Enum.GetNames(typeof(PopParameters.WindowType))) {
                if (comboBoxSampleWindow.Text == name) {
                    _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleWindow =
                            (PopParameters.WindowType)Enum.Parse(typeof(PopParameters.WindowType), name);
                    break;
                }
                // should never get here:
                _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleWindow = PopParameters.WindowType.Rectangular;
            }

            // Doppler parameters group
            isOK = int.TryParse(textBoxDopNPts.Text.ToString(),
                    out _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts);
            isOK = int.TryParse(textBoxDopNSpec.Text.ToString(),
                    out _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNSpec);
            if (checkBoxDopOverlap.Checked) {
                _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerOverlap = true;
            }
            else {
                _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerOverlap = false;
            }
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
			
			isOK = int.TryParse(textBoxRangeOffset.Text.ToString(),
                    out _parameters.SystemPar.RadarPar.FmCwParSet[0].RangeOffsetM);

            int firstGate, lastGate;
            CheckSelectedGateRange(); 
            firstGate = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateFirst = 
                        (int)numericUpDownGateFirst.Value;
            lastGate = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerKeepGateLast = 
                        (int)numericUpDownGateLast.Value;

			// Baseband filter group
			if (checkBoxApplyFilterCorr.Checked) {
				_parameters.SystemPar.RadarPar.FmCwParSet[0].ApplyFilterCorrection = true;
			}
			else {
				_parameters.SystemPar.RadarPar.FmCwParSet[0].ApplyFilterCorrection = false;
			}
			_parameters.SystemPar.RadarPar.FmCwParSet[0].FilterCoeffFile = comboBoxFilterCoeff.Text;

			// Input samples
			if (radioButtonRawSamples.Checked) {
				_parameters.SystemPar.RadarPar.FmCwParSet[0].InputSampleUnitsIsRaw = true;
			}
			else {
				_parameters.SystemPar.RadarPar.FmCwParSet[0].InputSampleUnitsIsRaw = false;
			}

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
				_parameters.SystemPar.RadarPar.ProcPar.NumberOfRx = 1;
				_parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[0] = 0;	// rass is off
				for (int i = 1; i < 6; i++) {
					_parameters.SystemPar.RadarPar.ProcPar.RassSourceParams[i] = -1;
				}
				_parameters.SystemPar.RadarPar.ProcPar.NumberOfMetInst = 0;
				_parameters.SystemPar.RadarPar.ProcPar.Dop0 = 1;
				_parameters.SystemPar.RadarPar.ProcPar.Dop1 = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerNPts;
				_parameters.SystemPar.RadarPar.ProcPar.Dop2 = 1;
				_parameters.SystemPar.RadarPar.ProcPar.Dop3 = 0;
				_parameters.SystemPar.RadarPar.ProcPar.IsIcraAvg = false;

				_parameters.SystemPar.RadarPar.ProcPar.IsDcFiltering = _parameters.SystemPar.RadarPar.FmCwParSet[0].DopplerDcFilter;
				_parameters.SystemPar.RadarPar.BeamParSet[0].NCI = 1;
				_parameters.SystemPar.RadarPar.BeamParSet[0].NCode = 1;

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

			}  // end if(isFmCw)

		}

		private void FillOutputPage() {
			if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 0) {
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileWriteEnabled) {
					checkBoxEnableDiskWrite_1.Checked = true;
				}
				else {
					checkBoxEnableDiskWrite_1.Checked = false;
				}
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeSpectra) {
					checkBoxSpectra_1.Checked = true;
				}
				else {
					checkBoxSpectra_1.Checked = false;
				}
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
			}
			if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 1) {
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileWriteEnabled) {
					checkBoxEnableDiskWrite_2.Checked = true;
				}
				else {
					checkBoxEnableDiskWrite_2.Checked = false;
				}
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeSpectra) {
					checkBoxSpectra_2.Checked = true;
				}
				else {
					checkBoxSpectra_2.Checked = false;
				}
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

		private void SaveOutputPage() {
			if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 0) {
				// make sure at most 1 time series type is selected
				if (checkBox1TS_1.Checked) {
					checkBoxFullTS_1.Checked = false;
				}
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].FileWriteEnabled =
					checkBoxEnableDiskWrite_1.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeSpectra =
					checkBoxSpectra_1.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeMoments =
					checkBoxMoments_1.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeSingleTS =
					checkBox1TS_1.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[0].IncludeFullTS =
					checkBoxFullTS_1.Checked;
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
			}
			if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 1) {
				// make sure at most 1 time series type is selected
				if (checkBox1TS_2.Checked) {
					checkBoxFullTS_2.Checked = false;
				}
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].FileWriteEnabled =
					checkBoxEnableDiskWrite_2.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeSpectra =
					checkBoxSpectra_2.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeMoments =
					checkBoxMoments_2.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeSingleTS =
					checkBox1TS_2.Checked;
				_parameters.SystemPar.RadarPar.ProcPar.PopFiles[1].IncludeFullTS =
					checkBoxFullTS_2.Checked;
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
			}
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
		}

		private void FillProcessingPage() {

			checkBoxReplay.Checked = _parameters.ReplayPar.Enabled;
			if (_parameters.ReplayPar.ProcessTimeSeries) {
				radioButtonReplayTS.Checked = true;
			}
			else {
				if (_parameters.ReplayPar.ProcessSpectra) {
					radioButtonReplaySpec.Checked = true;
				}
				else {
					radioButtonReplayNoRecalc.Checked = true;
				}
			}
			textBoxReplayFilePath.Text = _parameters.ReplayPar.InputFile;
			if (File.Exists(labelReplayFile.Text)) {
				labelReplayFile.Text = Path.GetFileName(textBoxReplayFilePath.Text);
			}
			labelReplayFile.Text = Path.GetFileName(textBoxReplayFilePath.Text);

			TimeSpan start = _parameters.ReplayPar.StartTime;
			TimeSpan end = _parameters.ReplayPar.EndTime;
			if (end == TimeSpan.Zero) {
				end = new TimeSpan(24, 0, 0);
			}
			textBoxReplayStart.Text = start.Hours.ToString("00") + ":" + start.Minutes.ToString("00");
			textBoxReplayEnd.Text = ((int)(end.TotalHours)).ToString("00") + ":" + end.Minutes.ToString("00");

			checkBoxClutterRemoval.Checked = _parameters.SystemPar.RadarPar.ProcPar.RemoveClutter;
			textBoxClutterHtKm.Text = _parameters.SystemPar.RadarPar.ProcPar.MaxClutterHtKm.ToString("0.000");

			// consensus

			checkBoxCnsEnable.Checked = _parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsEnable;

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

		}

		private void SaveProcessingPage() {
			_parameters.ReplayPar.Enabled = checkBoxReplay.Checked;
			if (radioButtonReplayTS.Checked) {
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
			_parameters.ReplayPar.TimeDelayMs = 0;
			_parameters.ReplayPar.InputFile = textBoxReplayFilePath.Text;

			if (textBoxReplayStart.Text.Contains(":")) {
				_parameters.ReplayPar.StartTime = TimeSpan.Parse(textBoxReplayStart.Text);
			}
			else {
				_parameters.ReplayPar.StartTime = TimeSpan.Zero;
			}
			if (textBoxReplayEnd.Text.Contains(":")) {
				try {
					_parameters.ReplayPar.EndTime = TimeSpan.Parse(textBoxReplayEnd.Text);
				}
				catch (Exception e) {
					//MessageBox.Show(e.Message, "Time Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					_parameters.ReplayPar.EndTime = new TimeSpan(24, 0, 0);
				}
			}
			else {
				_parameters.ReplayPar.EndTime = TimeSpan.Zero ;
			}

			Double.TryParse(textBoxClutterHtKm.Text, out _parameters.SystemPar.RadarPar.ProcPar.MaxClutterHtKm);
			_parameters.SystemPar.RadarPar.ProcPar.RemoveClutter = checkBoxClutterRemoval.Checked;

			// consensus
			_parameters.SystemPar.RadarPar.ProcPar.CnsPar[0].CnsEnable = false;

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
            if (_parameters == null) {
                _parameters = new PopParameters();
                this.SetupParameters = _parameters;
            }
            _backupParameters = _parameters.DeepCopy();
			if (_parameters.SystemPar.RadarPar.RadarType == PopParameters.TypeOfRadar.FmCw) {
				tabControl1.SelectedTab = tabPageFmCw;
			}
			// enable user setting frequency offset
			radioButtonFreqOffset.Checked = true;

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

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        private void radioButtonPulsedType_CheckedChanged(object sender, EventArgs e) {
            if (radioButtonPulsedType.Checked) {
                _parameters.SystemPar.RadarPar.RadarType = PopParameters.TypeOfRadar.PulsedTx;
            }
            else {
                _parameters.SystemPar.RadarPar.RadarType = PopParameters.TypeOfRadar.FmCw;
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
            SaveSystemPage();
        }
        private void tabPageFmCw_Leave(object sender, EventArgs e) {
            SaveFmCwPage();
        }
		private void tabPageOutput_Leave(object sender, EventArgs e) {
			SaveOutputPage();
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
            this.SetupParameters = _backupParameters.DeepCopy();
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
            SaveAllTabPages();
            saveFileDialog1.InitialDirectory = Path.GetDirectoryName(_parameters.Source);
            saveFileDialog1.FileName = Path.GetFileName(_parameters.Source);
            DialogResult rr = saveFileDialog1.ShowDialog();
            if (rr == DialogResult.OK) {
                string ext = Path.GetExtension(saveFileDialog1.FileName);
                if (ext.ToLower() == ".parx") {
                    _parameters.Source = saveFileDialog1.FileName;
					textBoxFileName.Text = _parameters.Source;
					textBoxFileName.SelectionStart = textBoxFileName.TextLength;
					_parameters.WriteToFile(saveFileDialog1.FileName);
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
			SaveToParFile();
		}

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        private void PopNSetup_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                // the user probably expects everything visible to be used by POPN
                SaveAllTabPages();
				PopParameters fileParameters = PopParameters.ReadFromFile(textBoxFileName.Text);
				// compare the parameters in the file to the parameters on the screen
                if (fileParameters.Equals(_parameters)) {
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
                    }
					else if (rr == DialogResult.OK) {
						// save then exit
						buttonSave_Click(null, null);
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
		}

		/*
		private void textBoxFmCwTimeStepUSec_TextChanged(object sender, EventArgs e) {
			RecalculateRangeRes();
		}
		*/

		private void textBoxFmCwSweepCenterMHz_TextChanged(object sender, EventArgs e) {
			CalculateSweepRegisterValues();
		}

		private void textBoxFmCwSweepRate_TextChanged(object sender, EventArgs e) {
			RecalculateRangeRes();
			double sweep = 0.0;
			double sweepDDS = 0.0;
			double.TryParse(textBoxFmCwSweepRate.Text, out sweep);
			double.TryParse(labelDDSSweepRate0.Text, out sweepDDS);
			if (sweep != sweepDDS) {
				_rampIsOriginal = true;
				buttonUseDDSValues.Text = "Use DDS Values";
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
		}

		private void textBoxSweepSampleDelay_TextChanged(object sender, EventArgs e) {
			RecalculateLastSample();
		}

		private void textBoxFmCwTimeStepClocks_TextChanged(object sender, EventArgs e) {
			RecalculateRangeRes();
		}


		private void RecalculateRangeRes() {
			bool isOK;
			int npts, spacing;
			double sweepRate;
			int sysDelayNs;

			isOK = int.TryParse(textBoxSweepNPts.Text.ToString(), out npts);
			isOK = int.TryParse(textBoxSweepSpacing.Text.ToString(), out spacing);
			isOK = double.TryParse(textBoxFmCwSweepRate.Text.ToString(), out sweepRate);
			// FMCW uses only sysdelay #0 from main page
			sysDelayNs = _parameters.SystemPar.RadarPar.RxBw[0].BwDelayNs;

			double sysDelayCorrectionM = sysDelayNs * PopParameters.MperNs;

			sweepRate = 1.0e6 * sweepRate;
			double sampleSpacingHz = 1.0e9 / (npts * spacing);
			double rangeSpacingNs = 1.0e9 * sampleSpacingHz / (sweepRate);
			labelGateSpacingNs.Text = rangeSpacingNs.ToString("f2");

			double rangeResM = PopParameters.MperNs * rangeSpacingNs;
			labelRangeResM.Text = rangeResM.ToString("f3");

			int firstGate = (int)numericUpDownGateFirst.Value;
			int lastGate = (int)numericUpDownGateLast.Value;

			//range = NewRangeM(0, rangeResM, rangeCorrections);

			double offset;
			double igate;
			//isOK = double.TryParse(textBoxFmCwOffsetHz.Text, out freqOffset);
			if (radioButtonGateOffset.Checked) {
				double.TryParse(textBoxFmCwOffsetGate.Text, out igate);
				offset = OffsetFreq(igate, sampleSpacingHz, rangeResM, sysDelayCorrectionM);
				textBoxFmCwOffsetHz.Text = offset.ToString("f2");
			}
			else {
				double.TryParse(textBoxFmCwOffsetHz.Text, out offset);
				igate = OffsetGate(offset, sampleSpacingHz, rangeResM, sysDelayCorrectionM);
				textBoxFmCwOffsetGate.Text = igate.ToString("f2");
			}

			// check: this is gate offset from save params:
			double jgate = _parameters.GetGateOffset();

			//double offsetToUse;
			double rangeCorrections = rangeResM * offset / sampleSpacingHz + sysDelayCorrectionM;
			double firstGateM;
			double lastGateM;

			if (offset < 0.0) {
				_lastNegOffset = offset;
				// for negative offsets, we will rearrange FFT pts
				// so that zero range is first point
				_maxGate = (int)Math.Abs(igate);
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
				_lastOffset = offset;
			}
			else {
				_lastPosOffset = offset;
				firstGateM = NewRangeM(firstGate, rangeResM, rangeCorrections);
				lastGateM = NewRangeM(lastGate, rangeResM, rangeCorrections);
				_lastOffset = offset;
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

			CalculateSweepRegisterValues();

		}

		private void RecalculateLastSample() {
			int nSamples, delayNs, spacingNs;
			int.TryParse(textBoxSweepNPts.Text, out nSamples);
			int.TryParse(textBoxSweepSampleDelay.Text, out delayNs);
			int.TryParse(textBoxSweepSpacing.Text, out spacingNs);
			double lastUs = (delayNs + (nSamples - 1) * spacingNs) / 1000.0;
			labelLastSampleUs.Text = lastUs.ToString("f0");
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

		private void CalculateSweepRegisterValues() {

			double sweepRate, offset, syncClockPeriodNs;
			int timeStepClocks;
			double lastUs;
			double timeStepUs;
			double centerFreqMHz;
			double sysClockMHz;

			double.TryParse(textBoxFmCwSweepCenterMHz.Text, out centerFreqMHz);
			double.TryParse(textBoxFmCwSweepRate.Text, out sweepRate);
			double.TryParse(textBoxFmCwOffsetHz.Text, out offset);
			int.TryParse(textBoxFmCwTimeStepClocks.Text, out timeStepClocks);
			double.TryParse(labelDDSSyncClockPeriodNsec.Text, out syncClockPeriodNs);
			double.TryParse(labelDDSSysClockMHz.Text, out sysClockMHz);
			timeStepUs = timeStepClocks * syncClockPeriodNs / 1000.0;

			RecalculateLastSample();
			double.TryParse(labelLastSampleUs.Text, out lastUs);
			double sweepTimeUs = lastUs + 10.0;

			// create AD9959 board object without connecting to hardware
			AD9959EvalBd DDS = new AD9959EvalBd(100.0, 5, false);
			DDS.SetFreqSweepParameters(0, centerFreqMHz, sweepRate, 0.0, timeStepClocks, sweepTimeUs);
			DDS.SetFreqSweepParameters(1, centerFreqMHz, sweepRate, offset, timeStepClocks, sweepTimeUs);

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
			labelDDSSweepRate0.Text = actualRampRate0.ToString("f4");
			labelDDSSweepRate1.Text = actualRampRate1.ToString("f4");
			labelDDSFreqOffsetHz.Text = actualOffset.ToString("f2");
			labelDDSStartFreqMHz0.Text = actualStartFreq0.ToString("f3");
			labelDDSStartFreqMHz1.Text = actualStartFreq1.ToString("f3");
			labelDDSEndFreqMHz0.Text = actualEndFreq0.ToString("f3");
			labelDDSEndFreqMHz1.Text = actualEndFreq1.ToString("f3");
			labelDDSDeltaFreqHz0.Text = (actualDeltaFreq0 * 1.0e6).ToString("F3");
			labelDDSDeltaFreqHz1.Text = (actualDeltaFreq1 * 1.0e6).ToString("F3");
			labelDDSDeltaTimeUsec0.Text = actualDeltaTime0.ToString("F3");
			labelDDSDeltaTimeUsec1.Text = actualDeltaTime1.ToString("F3");
			labelDDSRampDurationUsec.Text = actualRampTime.ToString("F1");

			return;

			//////////////////////////////////////////////////
			/*
			double startFreqMHz = centerFreqMHz - sweepRate * sweepTimeUs / 2.0e6;
			double steps = sweepTimeUs / timeStepUs;
			double rampStepFreqMHz = sweepRate * timeStepUs / 1.0e6;
			double endFreqMHz = startFreqMHz + steps * rampStepFreqMHz;

			double newStartFreqMHz, newEndFreqMHz, newDeltaFreqStepMHz;
			AD9959EvalBd.GetFreqBinaryString0(startFreqMHz, sysClockMHz, out newStartFreqMHz);
			AD9959EvalBd.GetFreqBinaryString0(endFreqMHz, sysClockMHz, out newEndFreqMHz);
			AD9959EvalBd.GetFreqBinaryString0(rampStepFreqMHz, sysClockMHz, out newDeltaFreqStepMHz);

			double nSteps, newRampTimeUs=0.0, newRampRate = 0.0;
			if (newDeltaFreqStepMHz != 0.0) {
				nSteps = (newEndFreqMHz - newStartFreqMHz) / newDeltaFreqStepMHz;
				newRampTimeUs = nSteps * timeStepUs;
				if (newRampTimeUs != 0.0) {
					newRampRate = 1.0e6 * (newEndFreqMHz - newStartFreqMHz) / newRampTimeUs;
				}
			}

			double freqOffsetHz;
			double.TryParse(textBoxFmCwOffsetHz.Text, out freqOffsetHz);

			double newStartFreqMHz2, newEndFreqMHz2, newDeltaFreqStepMHz2;
			double startFreqMHz2 = startFreqMHz + freqOffsetHz / 1.0e6;
			double endFreqMHz2 = endFreqMHz + freqOffsetHz / 1.0e6;
			AD9959EvalBd.GetFreqBinaryString0(startFreqMHz2, sysClockMHz, out newStartFreqMHz2);
			AD9959EvalBd.GetFreqBinaryString0(endFreqMHz2, sysClockMHz, out newEndFreqMHz2);
			// delta freq step is same in both sweeps
			//AD9959EvalBd.GetFreqBinaryString0(rampStepFreqMHz2, sysClockMHz, out newDeltaFreqStepMHz);

			double nSteps2, newRampTimeUs2, newRampRate2 = 0.0;
			if (newDeltaFreqStepMHz != 0.0) {
				nSteps2 = (newEndFreqMHz2 - newStartFreqMHz2) / newDeltaFreqStepMHz;
				newRampTimeUs2 = nSteps2 * timeStepUs;
				if (newRampTimeUs2 != 0.0) {
					newRampRate2 = 1.0e6 * (newEndFreqMHz2 - newStartFreqMHz2) / newRampTimeUs2;
				}
			}

			double newOffset = (newStartFreqMHz2 - newStartFreqMHz) * 1.0e6;

			labelDDSSweepRate0.Text = newRampRate.ToString("f4");
			labelDDSSweepRate1.Text = newRampRate2.ToString("f4");
			labelDDSFreqOffsetHz.Text = newOffset.ToString("f2");
			labelDDSStartFreqMHz0.Text = newStartFreqMHz.ToString("f3");
			labelDDSStartFreqMHz1.Text = newStartFreqMHz2.ToString("f3");
			labelDDSEndFreqMHz0.Text = newEndFreqMHz.ToString("f3");
			labelDDSEndFreqMHz1.Text = newEndFreqMHz2.ToString("f3");
			labelDDSDeltaFreqHz0.Text = (newDeltaFreqStepMHz * 1.0e6).ToString("F3");
			labelDDSDeltaFreqHz1.Text = (newDeltaFreqStepMHz * 1.0e6).ToString("F3");
			labelDDSDeltaTimeUsec0.Text = timeStepUs.ToString("F3");
			labelDDSDeltaTimeUsec1.Text = timeStepUs.ToString("F3");
			labelDDSRampDurationUsec.Text = newRampTimeUs.ToString("F1");
			*/
		}


		private double NewRangeM(double igate, double rangeResM, double rangeCorrections) {
			double range;
			range = rangeResM * igate - rangeCorrections;
			return range;
		}

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
				checkBoxMoments_1.Enabled = true;
				checkBoxFullTS_1.Enabled = true;
				checkBoxRawTS_1.Enabled = true;
				checkBox1TS_1.Enabled = true;
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
				checkBoxMoments_1.Enabled = false;
				checkBoxFullTS_1.Enabled = false;
				checkBoxRawTS_1.Enabled = false;
				checkBox1TS_1.Enabled = false;
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
				checkBoxSpectra_2.Enabled = true;
				checkBoxMoments_2.Enabled = true;
				checkBoxFullTS_2.Enabled = true;
				checkBoxRawTS_2.Enabled = true;
				checkBox1TS_2.Enabled = true;
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
				checkBoxMoments_2.Enabled = false;
				checkBoxFullTS_2.Enabled = false;
				checkBoxRawTS_2.Enabled = false;
				checkBox1TS_2.Enabled = false;
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
			}
		}

		private void checkBoxFullTS_1_CheckedChanged(object sender, EventArgs e) {
			if (checkBoxFullTS_1.Checked) {
				checkBox1TS_1.Checked = false;
			}
		}

		private void checkBox1TS_2_CheckedChanged(object sender, EventArgs e) {
			if (checkBox1TS_2.Checked) {
				checkBoxFullTS_2.Checked = false;
			}
		}

		private void checkBoxFullTS_2_CheckedChanged(object sender, EventArgs e) {
			if (checkBoxFullTS_2.Checked) {
				checkBox1TS_2.Checked = false;
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
		}

		private void checkBoxMoments_1_CheckedChanged(object sender, EventArgs e) {
			DisplayFileName(0);
		}

		private void checkBoxSpectra_2_CheckedChanged(object sender, EventArgs e) {
			DisplayFileName(1);
		}

		private void checkBoxMoments_2_CheckedChanged(object sender, EventArgs e) {
			DisplayFileName(1);

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
            SaveAllTabPages();
            string outputFilePathName = _parameters.Source;

            string ext = Path.GetExtension(outputFilePathName);
            if (ext.ToLower() == ".parx") {
				textBoxFileName.Text = outputFilePathName;
				textBoxFileName.SelectionStart = textBoxFileName.TextLength;
				_parameters.WriteToFile(outputFilePathName);
                MessageBoxEx.Show("Parameters saved to file\n" + outputFilePathName, "Saved to File", 1500);
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
            if (checkBoxSelectHts.Checked) {
                // if user is selecting gates to keep
                // make sure the first and last gates are within
                //  the range of actual gates
                int firstGate = (int)numericUpDownGateFirst.Value;
                int lastGate = (int)numericUpDownGateLast.Value;
                int npts = _parameters.SystemPar.RadarPar.FmCwParSet[0].TxSweepSampleNPts / 2 + 1;
                if ((firstGate > npts - 1) || (firstGate > lastGate)) {
                    numericUpDownGateFirst.Value = 0;
                }
                if (lastGate > npts - 1) {
                    numericUpDownGateLast.Value = npts - 1;
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
					if (checkBoxMoments_1.Checked && !checkBoxSpectra_1.Checked) {
						ext = ".mom";
					}
					else {
						ext = ".spc";
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
					fileName += suffix + ext;
					labelOutFile_1.Text = fileName;

				}
				
			}
			else if (index == 1) {
				if (_parameters.SystemPar.RadarPar.ProcPar.PopFiles.Length > 1) {
					string ext;
					if (checkBoxMoments_2.Checked && !checkBoxSpectra_2.Checked) {
						ext = ".mom";
					}
					else {
						ext = ".spc";
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

		}

		//
        #endregion Private Methods


        #region TEST DaqBoard
        //
        private DaqBoard3000USB _daqBoard;
        private const int NREPS = 10;
        private int _iReps;
        //private bool _dataReady;
        private int nIpp = 1024;

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
            _daqBoard.NDataSamples = nSamples * nIpp;
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
            _daqBoard.NDataSamples = nSamples * nIpp;
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
                if (count != daqBoard.NDataSamples) {
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
            SaveToParFile();
			//buttonBWToggle_Click(null, null);
			if (_pulseBox == null) {
                _pulseBox = new PbxControllerCard();
            }
            if (!_pulseBox.Exists()) {
                //throw new ApplicationException("Can't find PulseBox Card");
            }
            _pulseBox.Setup(_parameters, 0);

			if (_parameters.SystemPar.RadarPar.FmCwParSet[0].AD9959Enabled) {
				double refClock = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDSRefClockMHz;
				int clockMultiplier = _parameters.SystemPar.RadarPar.FmCwParSet[0].DDSMultiplier;
				_AD9959EvalBd = new AD9959EvalBd(refClock, clockMultiplier, true);

				_AD9959EvalBd.RunFmCwFreqSweeps(_parameters);
			}
			//MessageBox.Show("Sweep started, DDS in PC mode now. Click for manual mode.");
			//_AD9959EvalBd.SetPCMode(false);

			//buttonBWToggle_Click(null, null);
        }

		private void buttonBWToggle_Click(object sender, EventArgs e) {
			//MessageBox.Show("BW button Click");
			PbxControllerCard pbx = new PbxControllerCard(true);
			//MessageBox.Show("Create link to existing PBX in BW button click");
			/*
			if (!pbx.Exists()) {
				//throw new ApplicationException("Can't find PulseBox Card");
				return;
			}
			*/
			bool restartPulses = false;
			if (pbx.PbxIsBusy()) {
				pbx.StopPulses();
				restartPulses = true;
			}
			pbx.PbxWriteBW(1);
			MessageBox.Show("BW is set to 1", "Testing BW");
			Thread.Sleep(1000);
			pbx.PbxWriteBW(0);
			if (restartPulses) {
				pbx.StartPulses();
			}

		}

		//////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonStopPbx_Click(object sender, EventArgs e) {
            if (_pulseBox == null) {
                _pulseBox = new PbxControllerCard();
            }
            _pulseBox.StopPulses();
			if (_AD9959EvalBd != null) {
				_AD9959EvalBd = new AD9959EvalBd(100.0, 5, true);
			}
		}

		private void dataGridViewBeamPar_CellContentClick(object sender, DataGridViewCellEventArgs e) {

		}

		private void textBoxFileName_TextChanged(object sender, EventArgs e) {
			string fileName = Path.GetFileName(textBoxFileName.Text);
			this.Text = "PopNSetup: " + fileName; 
		}


		private void textBoxFmCwIppUSec_TextChanged(object sender, EventArgs e) {
			RecalculateNyquist();
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

		private void buttonUseDDSValues_Click(object sender, EventArgs e) {
			double rampRate;
			double offset;

			if (_rampIsOriginal) {
				double.TryParse(labelDDSSweepRate0.Text, out rampRate);
				double.TryParse(textBoxFmCwSweepRate.Text, out _originalRampRate);
				double.TryParse(labelDDSFreqOffsetHz.Text, out offset);
				double.TryParse(textBoxFmCwOffsetHz.Text, out _originalOffset);
				textBoxFmCwSweepRate.Text = rampRate.ToString("F2");
				textBoxFmCwOffsetHz.Text = offset.ToString("F2");
				_rampIsOriginal = false;
				buttonUseDDSValues.Text = "Use Original Sweep";
			}
			else {
				textBoxFmCwSweepRate.Text = _originalRampRate.ToString("F2");
				textBoxFmCwOffsetHz.Text = _originalOffset.ToString("F2");
				_rampIsOriginal = true;
				buttonUseDDSValues.Text = "Use DDS Values";
			}
		}




        //
        #endregion

       
    }
}
