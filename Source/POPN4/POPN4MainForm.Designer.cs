namespace POPN4 {
    partial class POPN4MainForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(POPN4MainForm));
            this.buttonGo = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.xpProgressBar1 = new Framework.Controls.XpProgressBar();
            this.checkBoxPause = new System.Windows.Forms.CheckBox();
            this.buttonKill = new System.Windows.Forms.Button();
            this.labelTimeStamp = new System.Windows.Forms.Label();
            this.comboBoxConfigFile = new System.Windows.Forms.ComboBox();
            this.buttonParameters = new System.Windows.Forms.Button();
            this.listBoxMessages = new System.Windows.Forms.ListBox();
            this.buttonStopService = new System.Windows.Forms.Button();
            this.checkBoxPlotOptions = new System.Windows.Forms.CheckBox();
            this.buttonUninstall = new System.Windows.Forms.Button();
            this.groupBoxService = new System.Windows.Forms.GroupBox();
            this.buttonStartService = new System.Windows.Forms.Button();
            this.buttonServiceStatus = new System.Windows.Forms.Button();
            this.labelServiceStatus = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.labelConfigExpand = new System.Windows.Forms.Label();
            this.buttonReconnect = new System.Windows.Forms.Button();
            this.labelCurrentParxFile = new System.Windows.Forms.Label();
            this.buttonBrowsePar = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.groupBoxPlotOptions = new System.Windows.Forms.GroupBox();
            this.checkBoxSampledTS = new System.Windows.Forms.CheckBox();
            this.checkBoxClutterWavelet = new System.Windows.Forms.CheckBox();
            this.checkBoxCrossCorr = new System.Windows.Forms.CheckBox();
            this.checkBoxDoppler = new System.Windows.Forms.CheckBox();
            this.checkBoxMoments = new System.Windows.Forms.CheckBox();
            this.buttonReplot = new System.Windows.Forms.Button();
            this.checkBoxDopplerSpec = new System.Windows.Forms.CheckBox();
            this.checkBoxDopplerAScan = new System.Windows.Forms.CheckBox();
            this.numericUpDownPlotHt = new System.Windows.Forms.NumericUpDown();
            this.checkBoxDopplerTS = new System.Windows.Forms.CheckBox();
            this.numericUpDownPlotRx = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBoxService.SuspendLayout();
            this.groupBoxPlotOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPlotHt)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPlotRx)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonGo
            // 
            this.buttonGo.BackColor = System.Drawing.Color.Lime;
            this.buttonGo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonGo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonGo.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonGo.Location = new System.Drawing.Point(39, 5);
            this.buttonGo.Name = "buttonGo";
            this.buttonGo.Size = new System.Drawing.Size(75, 23);
            this.buttonGo.TabIndex = 0;
            this.buttonGo.Text = "GO";
            this.buttonGo.UseVisualStyleBackColor = false;
            this.buttonGo.Click += new System.EventHandler(this.buttonGo_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.BackColor = System.Drawing.Color.OrangeRed;
            this.buttonStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonStop.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStop.ForeColor = System.Drawing.Color.Yellow;
            this.buttonStop.Location = new System.Drawing.Point(209, 5);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(100, 23);
            this.buttonStop.TabIndex = 1;
            this.buttonStop.Text = "Abort/Unload";
            this.buttonStop.UseVisualStyleBackColor = false;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // xpProgressBar1
            // 
            this.xpProgressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.xpProgressBar1.ColorBackGround = System.Drawing.Color.White;
            this.xpProgressBar1.ColorBarBorder = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(240)))), ((int)(((byte)(170)))));
            this.xpProgressBar1.ColorBarCenter = System.Drawing.Color.MediumSeaGreen;
            this.xpProgressBar1.ColorText = System.Drawing.Color.Black;
            this.xpProgressBar1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xpProgressBar1.Location = new System.Drawing.Point(6, 34);
            this.xpProgressBar1.Name = "xpProgressBar1";
            this.xpProgressBar1.Position = 0;
            this.xpProgressBar1.PositionMax = 100;
            this.xpProgressBar1.PositionMin = 0;
            this.xpProgressBar1.Size = new System.Drawing.Size(243, 23);
            this.xpProgressBar1.SteepDistance = ((byte)(0));
            this.xpProgressBar1.SteepWidth = ((byte)(1));
            this.xpProgressBar1.TabIndex = 10;
            this.xpProgressBar1.Text = "Please Wait...";
            this.xpProgressBar1.TextShadow = false;
            // 
            // checkBoxPause
            // 
            this.checkBoxPause.BackColor = System.Drawing.Color.Yellow;
            this.checkBoxPause.Cursor = System.Windows.Forms.Cursors.Hand;
            this.checkBoxPause.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxPause.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBoxPause.Location = new System.Drawing.Point(132, 5);
            this.checkBoxPause.Name = "checkBoxPause";
            this.checkBoxPause.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.checkBoxPause.Size = new System.Drawing.Size(59, 23);
            this.checkBoxPause.TabIndex = 11;
            this.checkBoxPause.Text = "Pause";
            this.checkBoxPause.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxPause.UseVisualStyleBackColor = false;
            this.checkBoxPause.CheckedChanged += new System.EventHandler(this.checkBoxPause_CheckedChanged);
            // 
            // buttonKill
            // 
            this.buttonKill.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonKill.BackColor = System.Drawing.Color.Fuchsia;
            this.buttonKill.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonKill.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonKill.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonKill.ForeColor = System.Drawing.Color.Black;
            this.buttonKill.Location = new System.Drawing.Point(300, 259);
            this.buttonKill.Name = "buttonKill";
            this.buttonKill.Size = new System.Drawing.Size(50, 23);
            this.buttonKill.TabIndex = 12;
            this.buttonKill.Text = "XDbg";
            this.buttonKill.UseVisualStyleBackColor = false;
            this.buttonKill.Visible = false;
            this.buttonKill.Click += new System.EventHandler(this.buttonKill_Click);
            // 
            // labelTimeStamp
            // 
            this.labelTimeStamp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelTimeStamp.BackColor = System.Drawing.Color.WhiteSmoke;
            this.labelTimeStamp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.labelTimeStamp.Location = new System.Drawing.Point(250, 34);
            this.labelTimeStamp.Name = "labelTimeStamp";
            this.labelTimeStamp.Size = new System.Drawing.Size(100, 23);
            this.labelTimeStamp.TabIndex = 13;
            this.labelTimeStamp.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // comboBoxConfigFile
            // 
            this.comboBoxConfigFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxConfigFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxConfigFile.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.comboBoxConfigFile.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBoxConfigFile.FormattingEnabled = true;
            this.comboBoxConfigFile.Location = new System.Drawing.Point(7, 62);
            this.comboBoxConfigFile.MaxDropDownItems = 25;
            this.comboBoxConfigFile.Name = "comboBoxConfigFile";
            this.comboBoxConfigFile.Size = new System.Drawing.Size(229, 21);
            this.comboBoxConfigFile.TabIndex = 14;
            this.comboBoxConfigFile.DropDown += new System.EventHandler(this.comboBoxConfigFile_DropDown);
            this.comboBoxConfigFile.DropDownClosed += new System.EventHandler(this.comboBoxConfigFile_DropDownClosed);
            // 
            // buttonParameters
            // 
            this.buttonParameters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonParameters.BackColor = System.Drawing.Color.Moccasin;
            this.buttonParameters.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonParameters.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonParameters.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonParameters.Location = new System.Drawing.Point(272, 62);
            this.buttonParameters.Name = "buttonParameters";
            this.buttonParameters.Size = new System.Drawing.Size(75, 23);
            this.buttonParameters.TabIndex = 15;
            this.buttonParameters.Text = "Parameters";
            this.buttonParameters.UseVisualStyleBackColor = false;
            this.buttonParameters.Click += new System.EventHandler(this.buttonParameters_Click);
            // 
            // listBoxMessages
            // 
            this.listBoxMessages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxMessages.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listBoxMessages.FormattingEnabled = true;
            this.listBoxMessages.HorizontalScrollbar = true;
            this.listBoxMessages.Location = new System.Drawing.Point(7, 118);
            this.listBoxMessages.Name = "listBoxMessages";
            this.listBoxMessages.Size = new System.Drawing.Size(340, 80);
            this.listBoxMessages.TabIndex = 16;
            // 
            // buttonStopService
            // 
            this.buttonStopService.BackColor = System.Drawing.Color.PeachPuff;
            this.buttonStopService.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonStopService.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonStopService.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStopService.ForeColor = System.Drawing.Color.Black;
            this.buttonStopService.Location = new System.Drawing.Point(96, 19);
            this.buttonStopService.Name = "buttonStopService";
            this.buttonStopService.Size = new System.Drawing.Size(72, 23);
            this.buttonStopService.TabIndex = 17;
            this.buttonStopService.Text = "Stop";
            this.buttonStopService.UseVisualStyleBackColor = false;
            this.buttonStopService.Click += new System.EventHandler(this.buttonStopService_Click);
            // 
            // checkBoxPlotOptions
            // 
            this.checkBoxPlotOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxPlotOptions.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxPlotOptions.BackColor = System.Drawing.Color.LemonChiffon;
            this.checkBoxPlotOptions.Checked = true;
            this.checkBoxPlotOptions.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxPlotOptions.Cursor = System.Windows.Forms.Cursors.Hand;
            this.checkBoxPlotOptions.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.checkBoxPlotOptions.Location = new System.Drawing.Point(260, 208);
            this.checkBoxPlotOptions.Name = "checkBoxPlotOptions";
            this.checkBoxPlotOptions.Size = new System.Drawing.Size(88, 23);
            this.checkBoxPlotOptions.TabIndex = 20;
            this.checkBoxPlotOptions.Text = "- More Options";
            this.checkBoxPlotOptions.UseVisualStyleBackColor = false;
            this.checkBoxPlotOptions.CheckedChanged += new System.EventHandler(this.checkBoxPlotOptions_CheckedChanged);
            // 
            // buttonUninstall
            // 
            this.buttonUninstall.BackColor = System.Drawing.Color.LightSalmon;
            this.buttonUninstall.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonUninstall.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonUninstall.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonUninstall.ForeColor = System.Drawing.Color.Black;
            this.buttonUninstall.Location = new System.Drawing.Point(181, 19);
            this.buttonUninstall.Name = "buttonUninstall";
            this.buttonUninstall.Size = new System.Drawing.Size(86, 23);
            this.buttonUninstall.TabIndex = 21;
            this.buttonUninstall.Text = "Uninstall";
            this.buttonUninstall.UseVisualStyleBackColor = false;
            this.buttonUninstall.Click += new System.EventHandler(this.buttonUninstall_Click);
            // 
            // groupBoxService
            // 
            this.groupBoxService.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBoxService.Controls.Add(this.buttonStartService);
            this.groupBoxService.Controls.Add(this.buttonStopService);
            this.groupBoxService.Controls.Add(this.buttonUninstall);
            this.groupBoxService.Location = new System.Drawing.Point(11, 237);
            this.groupBoxService.Name = "groupBoxService";
            this.groupBoxService.Size = new System.Drawing.Size(280, 48);
            this.groupBoxService.TabIndex = 22;
            this.groupBoxService.TabStop = false;
            this.groupBoxService.Text = "Service";
            // 
            // buttonStartService
            // 
            this.buttonStartService.BackColor = System.Drawing.Color.LightCyan;
            this.buttonStartService.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonStartService.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonStartService.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStartService.ForeColor = System.Drawing.Color.Black;
            this.buttonStartService.Location = new System.Drawing.Point(11, 19);
            this.buttonStartService.Name = "buttonStartService";
            this.buttonStartService.Size = new System.Drawing.Size(72, 23);
            this.buttonStartService.TabIndex = 22;
            this.buttonStartService.Text = "Start";
            this.buttonStartService.UseVisualStyleBackColor = false;
            this.buttonStartService.Click += new System.EventHandler(this.buttonStartService_Click);
            // 
            // buttonServiceStatus
            // 
            this.buttonServiceStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonServiceStatus.BackColor = System.Drawing.Color.OldLace;
            this.buttonServiceStatus.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonServiceStatus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonServiceStatus.Location = new System.Drawing.Point(91, 208);
            this.buttonServiceStatus.Name = "buttonServiceStatus";
            this.buttonServiceStatus.Size = new System.Drawing.Size(81, 23);
            this.buttonServiceStatus.TabIndex = 23;
            this.buttonServiceStatus.Text = "Unknown";
            this.buttonServiceStatus.UseVisualStyleBackColor = false;
            this.buttonServiceStatus.Click += new System.EventHandler(this.buttonServiceStatus_Click);
            // 
            // labelServiceStatus
            // 
            this.labelServiceStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelServiceStatus.Location = new System.Drawing.Point(5, 212);
            this.labelServiceStatus.Name = "labelServiceStatus";
            this.labelServiceStatus.Size = new System.Drawing.Size(100, 17);
            this.labelServiceStatus.TabIndex = 24;
            this.labelServiceStatus.Text = "   Service status:";
            // 
            // labelConfigExpand
            // 
            this.labelConfigExpand.AutoSize = true;
            this.labelConfigExpand.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.labelConfigExpand.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelConfigExpand.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelConfigExpand.ForeColor = System.Drawing.Color.Black;
            this.labelConfigExpand.Location = new System.Drawing.Point(321, 9);
            this.labelConfigExpand.Name = "labelConfigExpand";
            this.labelConfigExpand.Size = new System.Drawing.Size(18, 15);
            this.labelConfigExpand.TabIndex = 25;
            this.labelConfigExpand.Text = "...";
            this.labelConfigExpand.Click += new System.EventHandler(this.labelConfigExpand_Click);
            // 
            // buttonReconnect
            // 
            this.buttonReconnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonReconnect.BackColor = System.Drawing.Color.Khaki;
            this.buttonReconnect.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonReconnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonReconnect.Location = new System.Drawing.Point(178, 208);
            this.buttonReconnect.Name = "buttonReconnect";
            this.buttonReconnect.Size = new System.Drawing.Size(76, 23);
            this.buttonReconnect.TabIndex = 26;
            this.buttonReconnect.Text = "Reconnect";
            this.buttonReconnect.UseVisualStyleBackColor = false;
            this.buttonReconnect.Visible = false;
            this.buttonReconnect.Click += new System.EventHandler(this.buttonReconnect_Click);
            // 
            // labelCurrentParxFile
            // 
            this.labelCurrentParxFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCurrentParxFile.BackColor = System.Drawing.Color.WhiteSmoke;
            this.labelCurrentParxFile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.labelCurrentParxFile.ForeColor = System.Drawing.SystemColors.ControlText;
            this.labelCurrentParxFile.Location = new System.Drawing.Point(7, 88);
            this.labelCurrentParxFile.Name = "labelCurrentParxFile";
            this.labelCurrentParxFile.Size = new System.Drawing.Size(229, 21);
            this.labelCurrentParxFile.TabIndex = 27;
            this.labelCurrentParxFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonBrowsePar
            // 
            this.buttonBrowsePar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowsePar.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonBrowsePar.Image = ((System.Drawing.Image)(resources.GetObject("buttonBrowsePar.Image")));
            this.buttonBrowsePar.Location = new System.Drawing.Point(243, 62);
            this.buttonBrowsePar.Name = "buttonBrowsePar";
            this.buttonBrowsePar.Size = new System.Drawing.Size(23, 23);
            this.buttonBrowsePar.TabIndex = 28;
            this.buttonBrowsePar.UseVisualStyleBackColor = true;
            this.buttonBrowsePar.Click += new System.EventHandler(this.buttonBrowsePar_Click);
            // 
            // groupBoxPlotOptions
            // 
            this.groupBoxPlotOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxPlotOptions.BackColor = System.Drawing.Color.LightCyan;
            this.groupBoxPlotOptions.Controls.Add(this.checkBoxSampledTS);
            this.groupBoxPlotOptions.Controls.Add(this.checkBoxClutterWavelet);
            this.groupBoxPlotOptions.Controls.Add(this.checkBoxCrossCorr);
            this.groupBoxPlotOptions.Controls.Add(this.checkBoxDoppler);
            this.groupBoxPlotOptions.Controls.Add(this.checkBoxMoments);
            this.groupBoxPlotOptions.Controls.Add(this.buttonReplot);
            this.groupBoxPlotOptions.Controls.Add(this.checkBoxDopplerSpec);
            this.groupBoxPlotOptions.Controls.Add(this.checkBoxDopplerAScan);
            this.groupBoxPlotOptions.Controls.Add(this.numericUpDownPlotHt);
            this.groupBoxPlotOptions.Controls.Add(this.checkBoxDopplerTS);
            this.groupBoxPlotOptions.Controls.Add(this.numericUpDownPlotRx);
            this.groupBoxPlotOptions.Controls.Add(this.label1);
            this.groupBoxPlotOptions.Controls.Add(this.label2);
            this.groupBoxPlotOptions.Location = new System.Drawing.Point(11, 291);
            this.groupBoxPlotOptions.Name = "groupBoxPlotOptions";
            this.groupBoxPlotOptions.Size = new System.Drawing.Size(332, 122);
            this.groupBoxPlotOptions.TabIndex = 19;
            this.groupBoxPlotOptions.TabStop = false;
            this.groupBoxPlotOptions.Text = "Plots";
            // 
            // checkBoxSampledTS
            // 
            this.checkBoxSampledTS.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxSampledTS.AutoSize = true;
            this.checkBoxSampledTS.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxSampledTS.Cursor = System.Windows.Forms.Cursors.Hand;
            this.checkBoxSampledTS.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxSampledTS.Location = new System.Drawing.Point(21, 45);
            this.checkBoxSampledTS.Name = "checkBoxSampledTS";
            this.checkBoxSampledTS.Size = new System.Drawing.Size(122, 17);
            this.checkBoxSampledTS.TabIndex = 2;
            this.checkBoxSampledTS.Text = "Sampled Time Series";
            this.checkBoxSampledTS.UseVisualStyleBackColor = false;
            // 
            // checkBoxClutterWavelet
            // 
            this.checkBoxClutterWavelet.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxClutterWavelet.AutoSize = true;
            this.checkBoxClutterWavelet.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxClutterWavelet.Cursor = System.Windows.Forms.Cursors.Hand;
            this.checkBoxClutterWavelet.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxClutterWavelet.Location = new System.Drawing.Point(155, 59);
            this.checkBoxClutterWavelet.Name = "checkBoxClutterWavelet";
            this.checkBoxClutterWavelet.Size = new System.Drawing.Size(172, 17);
            this.checkBoxClutterWavelet.TabIndex = 12;
            this.checkBoxClutterWavelet.Text = "Clutter Wavelet Transform at Ht";
            this.checkBoxClutterWavelet.UseVisualStyleBackColor = false;
            // 
            // checkBoxCrossCorr
            // 
            this.checkBoxCrossCorr.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxCrossCorr.AutoSize = true;
            this.checkBoxCrossCorr.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxCrossCorr.Cursor = System.Windows.Forms.Cursors.Hand;
            this.checkBoxCrossCorr.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxCrossCorr.Location = new System.Drawing.Point(155, 87);
            this.checkBoxCrossCorr.Name = "checkBoxCrossCorr";
            this.checkBoxCrossCorr.Size = new System.Drawing.Size(125, 17);
            this.checkBoxCrossCorr.TabIndex = 11;
            this.checkBoxCrossCorr.Text = "CrossCorrelation at Ht";
            this.checkBoxCrossCorr.UseVisualStyleBackColor = false;
            // 
            // checkBoxDoppler
            // 
            this.checkBoxDoppler.AutoSize = true;
            this.checkBoxDoppler.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxDoppler.Cursor = System.Windows.Forms.Cursors.Hand;
            this.checkBoxDoppler.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxDoppler.Location = new System.Drawing.Point(21, 101);
            this.checkBoxDoppler.Name = "checkBoxDoppler";
            this.checkBoxDoppler.Size = new System.Drawing.Size(92, 17);
            this.checkBoxDoppler.TabIndex = 10;
            this.checkBoxDoppler.Text = "Doppler Profile";
            this.checkBoxDoppler.UseVisualStyleBackColor = false;
            // 
            // checkBoxMoments
            // 
            this.checkBoxMoments.AutoSize = true;
            this.checkBoxMoments.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxMoments.Cursor = System.Windows.Forms.Cursors.Hand;
            this.checkBoxMoments.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxMoments.Location = new System.Drawing.Point(21, 87);
            this.checkBoxMoments.Name = "checkBoxMoments";
            this.checkBoxMoments.Size = new System.Drawing.Size(78, 17);
            this.checkBoxMoments.TabIndex = 9;
            this.checkBoxMoments.Text = "SNR Profile";
            this.checkBoxMoments.UseVisualStyleBackColor = false;
            // 
            // buttonReplot
            // 
            this.buttonReplot.BackColor = System.Drawing.Color.Lavender;
            this.buttonReplot.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonReplot.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonReplot.Location = new System.Drawing.Point(250, 14);
            this.buttonReplot.Name = "buttonReplot";
            this.buttonReplot.Size = new System.Drawing.Size(62, 23);
            this.buttonReplot.TabIndex = 8;
            this.buttonReplot.Text = "Replot";
            this.buttonReplot.UseVisualStyleBackColor = false;
            this.buttonReplot.Click += new System.EventHandler(this.buttonReplot_Click);
            // 
            // checkBoxDopplerSpec
            // 
            this.checkBoxDopplerSpec.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxDopplerSpec.AutoSize = true;
            this.checkBoxDopplerSpec.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxDopplerSpec.Cursor = System.Windows.Forms.Cursors.Hand;
            this.checkBoxDopplerSpec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxDopplerSpec.Location = new System.Drawing.Point(155, 73);
            this.checkBoxDopplerSpec.Name = "checkBoxDopplerSpec";
            this.checkBoxDopplerSpec.Size = new System.Drawing.Size(134, 17);
            this.checkBoxDopplerSpec.TabIndex = 7;
            this.checkBoxDopplerSpec.Text = "Doppler Spectrum at Ht";
            this.checkBoxDopplerSpec.UseVisualStyleBackColor = false;
            // 
            // checkBoxDopplerAScan
            // 
            this.checkBoxDopplerAScan.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxDopplerAScan.AutoSize = true;
            this.checkBoxDopplerAScan.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxDopplerAScan.Cursor = System.Windows.Forms.Cursors.Hand;
            this.checkBoxDopplerAScan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxDopplerAScan.Location = new System.Drawing.Point(21, 59);
            this.checkBoxDopplerAScan.Name = "checkBoxDopplerAScan";
            this.checkBoxDopplerAScan.Size = new System.Drawing.Size(98, 17);
            this.checkBoxDopplerAScan.TabIndex = 6;
            this.checkBoxDopplerAScan.Text = "Doppler A-Scan";
            this.checkBoxDopplerAScan.UseVisualStyleBackColor = false;
            // 
            // numericUpDownPlotHt
            // 
            this.numericUpDownPlotHt.Location = new System.Drawing.Point(174, 16);
            this.numericUpDownPlotHt.Name = "numericUpDownPlotHt";
            this.numericUpDownPlotHt.Size = new System.Drawing.Size(46, 20);
            this.numericUpDownPlotHt.TabIndex = 4;
            this.numericUpDownPlotHt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numericUpDownPlotHt.ValueChanged += new System.EventHandler(this.numericUpDownPlotHt_ValueChanged);
            // 
            // checkBoxDopplerTS
            // 
            this.checkBoxDopplerTS.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxDopplerTS.AutoSize = true;
            this.checkBoxDopplerTS.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxDopplerTS.Cursor = System.Windows.Forms.Cursors.Hand;
            this.checkBoxDopplerTS.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxDopplerTS.Location = new System.Drawing.Point(155, 45);
            this.checkBoxDopplerTS.Name = "checkBoxDopplerTS";
            this.checkBoxDopplerTS.Size = new System.Drawing.Size(144, 17);
            this.checkBoxDopplerTS.TabIndex = 3;
            this.checkBoxDopplerTS.Text = "Doppler Time Series at Ht";
            this.checkBoxDopplerTS.UseVisualStyleBackColor = false;
            // 
            // numericUpDownPlotRx
            // 
            this.numericUpDownPlotRx.Location = new System.Drawing.Point(54, 16);
            this.numericUpDownPlotRx.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownPlotRx.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownPlotRx.Name = "numericUpDownPlotRx";
            this.numericUpDownPlotRx.Size = new System.Drawing.Size(37, 20);
            this.numericUpDownPlotRx.TabIndex = 0;
            this.numericUpDownPlotRx.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numericUpDownPlotRx.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(26, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Rx #";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(129, 20);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Ht Index";
            // 
            // POPN4MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.PaleTurquoise;
            this.ClientSize = new System.Drawing.Size(355, 423);
            this.Controls.Add(this.buttonBrowsePar);
            this.Controls.Add(this.buttonReconnect);
            this.Controls.Add(this.labelConfigExpand);
            this.Controls.Add(this.buttonServiceStatus);
            this.Controls.Add(this.groupBoxService);
            this.Controls.Add(this.checkBoxPlotOptions);
            this.Controls.Add(this.buttonParameters);
            this.Controls.Add(this.comboBoxConfigFile);
            this.Controls.Add(this.labelTimeStamp);
            this.Controls.Add(this.buttonKill);
            this.Controls.Add(this.checkBoxPause);
            this.Controls.Add(this.xpProgressBar1);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonGo);
            this.Controls.Add(this.groupBoxPlotOptions);
            this.Controls.Add(this.labelServiceStatus);
            this.Controls.Add(this.listBoxMessages);
            this.Controls.Add(this.labelCurrentParxFile);
            this.MinimumSize = new System.Drawing.Size(270, 120);
            this.Name = "POPN4MainForm";
            this.Text = "POPN 4.15.1 (2016/02/26)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.POPNMainForm_FormClosing);
            this.Load += new System.EventHandler(this.POPN4MainForm_Load);
            this.groupBoxService.ResumeLayout(false);
            this.groupBoxPlotOptions.ResumeLayout(false);
            this.groupBoxPlotOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPlotHt)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPlotRx)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonGo;
        private System.Windows.Forms.Button buttonStop;
        private Framework.Controls.XpProgressBar xpProgressBar1;
		private System.Windows.Forms.CheckBox checkBoxPause;
        private System.Windows.Forms.Button buttonKill;
		private System.Windows.Forms.Label labelTimeStamp;
		private System.Windows.Forms.ComboBox comboBoxConfigFile;
		private System.Windows.Forms.Button buttonParameters;
		private System.Windows.Forms.ListBox listBoxMessages;
        private System.Windows.Forms.Button buttonStopService;
        private System.Windows.Forms.GroupBox groupBoxPlotOptions;
        private System.Windows.Forms.CheckBox checkBoxSampledTS;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericUpDownPlotRx;
        private System.Windows.Forms.CheckBox checkBoxPlotOptions;
        private System.Windows.Forms.Button buttonReplot;
        private System.Windows.Forms.CheckBox checkBoxDopplerSpec;
        private System.Windows.Forms.CheckBox checkBoxDopplerAScan;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericUpDownPlotHt;
        private System.Windows.Forms.CheckBox checkBoxDopplerTS;
        private System.Windows.Forms.CheckBox checkBoxDoppler;
        private System.Windows.Forms.CheckBox checkBoxMoments;
        private System.Windows.Forms.Button buttonUninstall;
        private System.Windows.Forms.GroupBox groupBoxService;
        private System.Windows.Forms.Button buttonStartService;
        private System.Windows.Forms.Button buttonServiceStatus;
        private System.Windows.Forms.Label labelServiceStatus;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label labelConfigExpand;
        private System.Windows.Forms.Button buttonReconnect;
        private System.Windows.Forms.Label labelCurrentParxFile;
        private System.Windows.Forms.Button buttonBrowsePar;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.CheckBox checkBoxCrossCorr;
        private System.Windows.Forms.CheckBox checkBoxClutterWavelet;
    }
}

