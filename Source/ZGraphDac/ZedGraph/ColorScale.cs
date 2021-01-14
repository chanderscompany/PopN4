using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.ComponentModel;
using ZedGraph;

namespace ZedGraph {
    public class ColorScale {

		private double _maxValue;
		private double _minValue;
		private bool _blendColors;
		private ColorScheme _scheme, _prevScheme;
		private Color _backgroundColor;
		private Color _colorBelowMin;
		public bool _extendLower, _repeatUpper;
		private bool _reverseOrder;
		private double _contourStep;
		private bool _useEachColorStep;
		private bool _userSetDataRange;

		public enum ColorScheme {
			GreenWhiteMagenta_15,
			BlueWhiteRed_15,
			Hue_23,
			Hue2_22,
			Hue3_27,
			//Rainbow1_20,
			Rainbow2_18,
			RainbowSteps_13,
			RainbowMatlab_17,
			RainbowETL_52,
			NWS0_14,
			NWS1_15,
			POP_9,
			OrangePurple_11,
			BlueYellowRed_11,
			Blue2Red_2,
			BlueGrayRed_3,
            GrayScale_2,
            RGB_3,
			Temperature_15,
            Default
        }

        public struct ColorSchemeStruct {
            public List<int> PercentValue;
            public List<Color> ColorValue;
        }

        public ColorSchemeStruct ColorSchemeDefinition;

		// indexer for ColorScale
		// colorScale[i] returns the ith color in the ColorSchemeDefinition list
		public Color this[int index] {
			get {
				if ((index >= 0) && (index < ColorSchemeDefinition.ColorValue.Count)) {
					return ColorSchemeDefinition.ColorValue[index];
				}
				else {
					return Color.Black;
				}
			}
		}

		/*
		// not implemented yet:
		[Description("If true, reverses the sequence of colors, max to min."), Category("Color Scale")]
		public bool ReverseOrder {
			get { return _reverseOrder; }
			set {
				_reverseOrder = value;
				SetColorRange();
			}
		}
		*/
		[Description("If true, colors of each contour are determined by the defined colors in the color scale." +
			" Otherwise, colors are distributed uniformly across the range of values."), Category("Color Scale")]
		public bool UseEachContourColorStep {
			get { return _useEachColorStep; }
			set { _useEachColorStep = value; }
		}
		[Description("If true, the min/max values of the color range are fixed by the user." +
			" Otherwise, the max value is determined by the max data value."), Category("Color Scale")]
		public bool UserSetDataMinMax {
			get { return _userSetDataRange; }
			set { _userSetDataRange = value; }
		}
		[Description("If true, z-values greater than max have color scale repeated" +
			" beginning with min color." + " Otherwise, large values all have max color."), Category("Color Scale")]
		public bool RepeatUpper {
			get { return _repeatUpper; }
			set { _repeatUpper = value; }
		}
		[Description("If true, z-values below min value all have min color." +
			" Otherwise, small values are not plotted and background color is shown."), Category("Color Scale")]
		public bool ExtendLower {
			get { return _extendLower; }
			set { _extendLower = value; }
		}
		[Description("Z-value corresponding to the high end of the color scale."), Category("Color Scale")]
		public double MaxValue {
			get { return _maxValue; }
			set { _maxValue = value; }
		}
		[Description("Z-value corresponding to the low end of the color scale."), Category("Color Scale")]
		public double MinValue {
			get { return _minValue; }
			set { _minValue = value; }
		}
		[Description("For contour plots: Z-value interval between contours."), Category("Color Scale")]
		public double ContourStep {
			get { return _contourStep; }
			set { _contourStep = value; }
		}
		[Description("On box plots: If true, colors are smoothly interpolated between defined values." +
		   " Otherwise, z-values have intervals of fixed colors." ), Category("Color Scale")]
		public bool BlendColors {
			get { return _blendColors; }
			set {
				_blendColors = value;
				ResetColorScheme(_scheme);
			}
		}
		[Description("Name of the type of color pattern used for z-values"), Category("Color Scale")]
		public ColorScheme Scheme {
			get { return _scheme; }
			set {
				_scheme = value;
				if (_scheme != _prevScheme) {
					// if we manually change to a new scheme,
					//	we need to reset color tables.
					//Init(_minValue, _maxValue, _scheme, _blendColors, _contourStep, _backgroundColor);
					ResetColorScheme(_scheme);
					//_extendLower = _prevScheme.ExtendLower;
					//_repeatUpper = _prevScheme.RepeatUpper;
					//_backgroundColor = _prevScheme.BackgroundColor;

				}
			}
		}
		[Description("Color used when no data to plot."), Category("Color Scale")]
		public Color BackgroundColor {
			get { return _backgroundColor; }
			set { _backgroundColor = value; }
		}
		[Description("Color used for data below min value, if ExtendLower is false."), Category("Color Scale")]
		public Color ColorBelowMin {
			get { return _colorBelowMin; }
			set { _colorBelowMin = value; }
		}

        private Random _randInt;
        private Color[] _colorRange;



		private bool _equalSpaceColors;

		public ColorScale(double minValue, double maxValue, ColorScheme scheme, bool blendColors) {
			Init(minValue, maxValue, scheme, blendColors, 0, Color.White);
		}

		public ColorScale(double minValue, double maxValue, ColorScheme scheme, bool blendColors, double contourStep) {
			Init(minValue, maxValue, scheme, blendColors, contourStep, Color.White);
		}

		public ColorScale(ColorScheme scheme, bool blendColors) {
			Init(0.0, 100.0, scheme, blendColors, 0, Color.White);
		}

        public ColorScale() {
            Init(0.0, 100.0, ColorScheme.Default, true, 0, Color.White);
        }

		public ColorScale(ColorScale otherScale) {
			Init(otherScale.MinValue, otherScale.MaxValue, otherScale.Scheme, otherScale.BlendColors, otherScale.ContourStep, otherScale.BackgroundColor);
			_extendLower = otherScale.ExtendLower;
			_repeatUpper = otherScale.RepeatUpper;
			_backgroundColor = otherScale.BackgroundColor;
			_userSetDataRange = otherScale.UserSetDataMinMax;
			_useEachColorStep = otherScale.UseEachContourColorStep;
			_colorBelowMin = otherScale.ColorBelowMin;
		}


        private void Init(double minValue, double maxValue, ColorScheme scheme, bool blend, double step, Color backgroundColor) {
			ResetColorScheme(scheme);
            _maxValue = maxValue;
            _minValue = minValue;
			_contourStep = step; 
            _randInt = new Random();
			_blendColors = blend;
			_equalSpaceColors = false;
			_backgroundColor = backgroundColor;
			_extendLower = false;
			_repeatUpper = false;
			_reverseOrder = false;	// TODO: reverseOrder feature not working
			SetColorRange();
			_prevScheme = _scheme;
			_useEachColorStep = false;
			_userSetDataRange = false;
			_colorBelowMin = backgroundColor;
		}

		private void ResetColorScheme(ColorScheme scheme) {
			_scheme = scheme;
			ColorSchemeDefinition.PercentValue = new List<int>();
			ColorSchemeDefinition.ColorValue = new List<Color>();
			DefineColorScheme();
			_colorRange = new Color[101];
			SetColorRange();
			_prevScheme = _scheme;
		}

        private void DefineColorScheme() { 
            switch (_scheme) {
                case ColorScheme.GrayScale_2:
                case ColorScheme.Default:
                    ColorSchemeDefinition.PercentValue.Add(0);
                    ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(50, 50, 50));
                    ColorSchemeDefinition.PercentValue.Add(100);
                    ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(200, 200, 200));
                    break;
				case ColorScheme.Temperature_15:
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 34));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 88));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 135));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 165));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 210));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(124, 0, 210));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(165, 0, 190));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(200, 0, 145));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(250, 0, 19));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 75, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 120, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 183, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 213, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 65));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 255));
					break;
				case ColorScheme.Blue2Red_2:
					ColorSchemeDefinition.PercentValue.Add(0);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 200));
					ColorSchemeDefinition.PercentValue.Add(100);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(200, 0, 0));
					break;
				case ColorScheme.BlueGrayRed_3:
					ColorSchemeDefinition.PercentValue.Add(0);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 200));
					ColorSchemeDefinition.PercentValue.Add(50);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(200, 200, 200));
					ColorSchemeDefinition.PercentValue.Add(100);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(200, 0, 0));
					break;
				case ColorScheme.BlueWhiteRed_15:
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 150));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 200));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(55, 55, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(100, 100, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(150, 150, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(190, 190, 255));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(220, 220, 255));

					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(234, 234, 234));

					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 220, 220));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 190, 190));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 150, 150));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 110, 110));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 65, 65));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(250, 10, 10));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(220, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(170, 0, 0));
					break;
				case ColorScheme.GreenWhiteMagenta_15:
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 80, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 134, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 187, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 241, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(80, 255, 80));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(134, 255, 134));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(187, 255, 187));

					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(234, 234, 234));

					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 187, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 134, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 80, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(241, 0, 241));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(187, 0, 187));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(134, 0, 134));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(100, 0, 100));

					break;
				case ColorScheme.OrangePurple_11:
					ColorSchemeDefinition.PercentValue.Add(0);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(127, 59, 8));
					ColorSchemeDefinition.PercentValue.Add(10);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(179, 88, 6));
					ColorSchemeDefinition.PercentValue.Add(20);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(224, 130, 20));
					ColorSchemeDefinition.PercentValue.Add(30);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(253, 184, 99));
					ColorSchemeDefinition.PercentValue.Add(45);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(254, 224, 182));
					ColorSchemeDefinition.PercentValue.Add(50);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(247, 247, 247));
					ColorSchemeDefinition.PercentValue.Add(55);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(216, 218, 235));
					ColorSchemeDefinition.PercentValue.Add(70);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(178, 171, 210));
					ColorSchemeDefinition.PercentValue.Add(80);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(128, 115, 172));
					ColorSchemeDefinition.PercentValue.Add(90);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(84, 39, 136));
					ColorSchemeDefinition.PercentValue.Add(100);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(45, 0, 75));
					break;
				case ColorScheme.BlueYellowRed_11:
					ColorSchemeDefinition.PercentValue.Add(0);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(49, 54, 149));
					ColorSchemeDefinition.PercentValue.Add(12);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(69, 117, 180));
					ColorSchemeDefinition.PercentValue.Add(25);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(116, 173, 209));
					//ColorSchemeDefinition.PercentValue.Add(35);
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(171, 217, 233));
					ColorSchemeDefinition.PercentValue.Add(49);
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(224, 243, 248));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(208, 235, 243));
					ColorSchemeDefinition.PercentValue.Add(50);
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(244, 244, 244));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(234, 244, 218));
					ColorSchemeDefinition.PercentValue.Add(51);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 191));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 215));
					ColorSchemeDefinition.PercentValue.Add(60);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(254, 224, 114));
					ColorSchemeDefinition.PercentValue.Add(70);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(253, 174, 97));
					ColorSchemeDefinition.PercentValue.Add(80);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(244, 109, 67));
					ColorSchemeDefinition.PercentValue.Add(90);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(215, 48, 39));
					ColorSchemeDefinition.PercentValue.Add(100);
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(165, 0, 38));
					break;
				case ColorScheme.POP_9:
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 0));  // remove black 26Ap2007
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 255, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 174, 0));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(173, 125, 57));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(173, 225, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 100, 15));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(206, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 130, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 255));
					break;
				case ColorScheme.RainbowMatlab_17:
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 148));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 200));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 170));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 93, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 178, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 223, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 255, 255));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(50, 255, 222));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(115, 255, 155));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(180, 255, 90));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(230, 255, 50));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 211, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 158, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 113, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 70, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(189, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(148, 0, 0));
					break;
				case ColorScheme.Rainbow2_18:
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 90, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 150, 255));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 185, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 220, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 255, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 250, 190));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 255, 172));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 235, 134));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 225, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(153, 235, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(204, 255, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 220, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 180, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 135, 0));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 95, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 75, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 50, 150));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 0, 175));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 90, 250));
					break;
				case ColorScheme.RainbowSteps_13:
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 250));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 90, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 120, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 185, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 255, 255));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 250, 190));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 225, 155));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 200, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(153, 235, 0));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(204, 255, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 200, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 111, 0));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 100, 0));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 85, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(220, 0, 0));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 50, 150));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 72, 250));
					break;
				/*
				case ColorScheme.NWSX:
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 247));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 162, 247));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 239, 239));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 235, 145));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 255, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 203, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 146, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(153, 235, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(231, 195, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 146, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 80, 80));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(245, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(200, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 0, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(190, 85, 206));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 255));
					break;
				*/
				case ColorScheme.RainbowETL_52:
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 217));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 35, 232));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 71, 247));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 84, 247));	
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 98, 247));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 108, 247));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 119, 238));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 126, 238));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 133, 237));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 140, 237));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 148, 238));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 159, 238));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 171, 228));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 184, 244));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(44, 196, 239));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(64, 207, 215));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(16, 209, 185));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(12, 214, 173));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(12, 214, 148));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(12, 214, 118));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(13, 213, 38));	
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(13, 218, 7));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(113, 221, 4));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(156, 222, 3));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(183, 235, 5));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(203, 238, 4));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(216, 251, 19));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(225, 252, 73));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(233, 253, 115));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(250, 253, 115));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 248, 102));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 243, 2));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 230, 2));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 215, 1));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 203, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 195, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 183, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 172, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 157, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 141, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 126, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 126, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 106, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 106, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 87, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 87, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(244, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(244, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(225, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(225, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(200, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(200, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(170, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(170, 0, 0));
					break;
					/*
				case ColorScheme.RainbowETL:
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 220));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 255));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 33, 247));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 55, 247));	// added
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 78, 247));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 85, 247));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 98, 247));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 110, 247));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 119, 238));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 125, 237));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 139, 237));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 140, 238));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 148, 238));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 159, 235));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 171, 228));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 184, 244));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(44, 196, 239));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(64, 207, 215));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(16, 209, 185));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(12, 214, 173));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(14, 212, 179));	// added
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(12, 214, 148));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(12, 214, 118));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(13, 213, 38));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(13, 218, 7));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 225, 0));	// added
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(113, 221, 4));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(156, 222, 3));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(183, 235, 5));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(203, 238, 4));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(216, 251, 19));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(225, 252, 73));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(233, 253, 115));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(250, 253, 115));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 248, 102));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 243, 2));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 230, 2));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 215, 1));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 203, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 195, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 183, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 172, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 157, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 141, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 126, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 116, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 106, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 96, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 87, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(250, 65, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(244, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(235, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(225, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(212, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(200, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(185, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(170, 0, 0));
					break;
					 * */
				case ColorScheme.Hue_23:
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(325), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(300), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(290), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(280), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(270), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(250), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(225), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(220), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(210), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(200), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(190), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(180), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(160), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(120), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(90), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(75), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(60), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(50), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(40), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(30), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(20), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(15), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(0), 255, 255)));
					break;
				case ColorScheme.Hue2_22:
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(325), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(300), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(290), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(280), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(270), 255, 255)));

					//ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(240), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(222), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(214), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(206), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(196), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(188), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(181), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(160), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(120), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(90), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(75), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(60), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(52), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(44), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(34), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(26), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(18), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(0), 255, 255)));
					break;
				case ColorScheme.Hue3_27:
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(350), 255, 255)));
					//ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(340), 255, 255)));
					//ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(330), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(320), 255, 255)));
					//ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(310), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(300), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(290), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(280), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(270), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(260), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(250), 255, 255)));
					//ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(240), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(230), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(220), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(210), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(200), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(190), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(180), 255, 255)));
					//ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(170), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(160), 255, 255)));
					//ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(150), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(140), 255, 255)));
					//ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(130), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(120), 255, 255)));
					//ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(110), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(100), 255, 255)));
					//ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(90), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(80), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(70), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(60), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(50), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(40), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(30), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(20), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(10), 255, 255)));
					ColorSchemeDefinition.ColorValue.Add(HSBColor.ToRGB(new HSBColor(deg2bin(0), 255, 255)));
					break;
				case ColorScheme.NWS0_14:
					// green(-) to red (+)
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(2, 252, 2));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(1, 228, 1));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(1, 197, 1));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(7, 172, 4));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(6, 143, 3));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(4, 114, 2));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(124, 151, 123));
					// zero
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(152, 119, 119));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(137, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(162, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(185, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(216, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(239, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(254, 0, 0));
					break;
				case ColorScheme.NWS1_15:
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 239, 239));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 162, 247));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 247));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 255, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 203, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 146, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(231, 195, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 146, 0));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 0, 0));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 50, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 80, 80));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(220, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(245, 0, 0));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(198, 0, 100));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(200, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 0, 255));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(156, 85, 206));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(190, 85, 206));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 255));
					break;
				/*
				case ColorScheme.Rainbow1_20:
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(150, 90, 254));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(140, 0, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(105, 40, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 90, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 150, 255));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 185, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 220, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 255, 255));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 250, 190));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 255, 172));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 235, 134));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 225, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(153, 235, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(204, 255, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 255, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 220, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 180, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 135, 0));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 95, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 75, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 50, 150));
					//ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 0, 175));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 90, 250));
					break;
				*/
				case ColorScheme.RGB_3:
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(255, 0, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 255, 0));
					ColorSchemeDefinition.ColorValue.Add(Color.FromArgb(0, 0, 255));
					break;
			}
        }

		private int deg2bin(int x) {
			return (256 * x / 360 % 360);
		}

        public void SetColorRange() {
			Color prevColor, nextColor;
            int numColors = ColorSchemeDefinition.ColorValue.Count;
			if (numColors < 2) {
				throw new Exception("ColorScheme table must have at least 2 entries");
			}
			if (ColorSchemeDefinition.PercentValue.Count == 0) {
				_equalSpaceColors = true;
			}
			else {
				if (ColorSchemeDefinition.PercentValue.Count != numColors) {
					throw new Exception("ColorScheme Definition  number of PercentValues not equal to number of colors.");
				}
				if (ColorSchemeDefinition.PercentValue[0] != 0) {
					throw new Exception("First ColorScheme entry must be 0.");
				}
				if (ColorSchemeDefinition.PercentValue[numColors - 1] != 100) {
					throw new Exception("Last ColorScheme entry must be 100.");
				}
			}
			int iColor = 0;
			int iSchemeIndex = -1;
			int prevIndex = -1;
			prevColor = Color.White;  // only necessary to fool compiler 
			// make a table of 101 colors at intervals of 1% of total range of color scheme.
            for (int i = 0; i < 101; i++) {
				// read color and its position from the color scheme
				iSchemeIndex = GetColorRangePercent(iColor);
				if (!_blendColors) {
					// display discrete color regions
					if (i == iSchemeIndex) {
						prevColor = ColorSchemeDefinition.ColorValue[iColor];
						_colorRange[i] = prevColor;
						iColor++;
						//prevIndex = iSchemeIndex;
					}
					else if (i < iSchemeIndex) {
						_colorRange[i] = prevColor;
					}
				}
				else {
					// blend colors by interpolating
					nextColor = ColorSchemeDefinition.ColorValue[iColor];
					if (i > iSchemeIndex) {
						// if we have already gone past this color's position in the scheme
						//		then go to next color in color scheme.
						iColor++;
						prevIndex = iSchemeIndex;
						//iSchemeIndex = ColorSchemeDefinition.PercentValue[iColor];
						iSchemeIndex = GetColorRangePercent(iColor);
						nextColor = ColorSchemeDefinition.ColorValue[iColor];
					}
					if (iSchemeIndex <= prevIndex) {
						throw new Exception("ColorScheme table entries must be must be in increasing order.");
					}
					if (i < iSchemeIndex) {
						// if we have not yet reached this color's position in the table,
						//		then interpolate colors.
						//int prevPercent = ColorSchemeDefinition.PercentValue[iColor-1];
						int prevPercent = GetColorRangePercent(iColor - 1);
						//int nextPercent = ColorSchemeDefinition.PercentValue[iColor];
						int nextPercent = GetColorRangePercent(iColor);
						double weight = (double)(i - prevPercent) / (double)(nextPercent - prevPercent);
						int red = (int)((1.0 - weight) * prevColor.R + weight * nextColor.R);
						int blue = (int)((1.0 - weight) * prevColor.B + weight * nextColor.B);
						int green = (int)((1.0 - weight) * prevColor.G + weight * nextColor.G);
						_colorRange[i] = Color.FromArgb(red, green, blue);
					}
					else if (i == iSchemeIndex) {
						// We are right at the position for this color,
						//		so use it and go to next color in table;
						prevColor = ColorSchemeDefinition.ColorValue[iColor];
						_colorRange[i] = prevColor;
						iColor++;
						prevIndex = iSchemeIndex;
					}
					
				}
			}
        }

		private int GetColorRangePercent(int colorIndex) {
			int percent;
			if (_blendColors == false) {
				if (colorIndex >= ColorSchemeDefinition.ColorValue.Count) {
					percent = 101;
				}
				else {
					percent = (int)(100.0 * colorIndex / (ColorSchemeDefinition.ColorValue.Count) + 0.5);
				}
			}
			else if (_equalSpaceColors) {
				percent = (int)(100.0 * colorIndex / (ColorSchemeDefinition.ColorValue.Count - 1) + 0.5);
			}
			else {
				percent =  ColorSchemeDefinition.PercentValue[colorIndex];
			}

			if (_reverseOrder) {
				return 100 - percent;
			}
			else {
				return percent;
			}
		}

		public Color GetColor(int percent) {
			return _colorRange[percent];
		}

		public Color GetColor(double zValue) {
			if (double.IsNaN(zValue)) {
				return _backgroundColor;
			}
			int percentZ = 0;
			if (false) {
				if (zValue > _maxValue) {
					percentZ = 100;
				}
				else if (zValue < _minValue) {
					percentZ = 0;
				}
				else {
					percentZ = (int)(100.0 * (zValue - _minValue) / (double)(_maxValue - _minValue));
				}
				return _colorRange[percentZ];
			}
			else {
				int colorIndex = 0;
				int numColors = ColorSchemeDefinition.ColorValue.Count;
				if (_extendLower) {
					colorIndex = (int)Math.Floor((zValue - _minValue) / (_maxValue - _minValue) * (numColors-1));
					if (colorIndex < 0) {
						// first color is below minValue
						// so use extended first color
						colorIndex = 0;
					}
					else {
						colorIndex++;
					}
				}
				else {
					// first color begins at minValue
					colorIndex = (int)Math.Floor((zValue - _minValue) / (_maxValue - _minValue) * numColors);
				}
				if (colorIndex < 0) {
					// color is below color for min value
					return _colorBelowMin;
				}
				if (colorIndex > numColors - 1) {
					if (_repeatUpper) {
						colorIndex = colorIndex % numColors;
					}
					else {
						colorIndex = numColors - 1;
						return ColorSchemeDefinition.ColorValue[colorIndex];
					}
				}
				if (_blendColors) {
					if ((zValue > _maxValue) && !_repeatUpper) {
						percentZ = 100;
					}
					else if (zValue < _minValue) {
						percentZ = 0;
					}
					else {
						percentZ = (int)(100.0 * (zValue - _minValue) / (double)(_maxValue - _minValue));
						if (_repeatUpper) {
							percentZ = percentZ % 100;
						}
					}
					return _colorRange[percentZ];
				}
				else {
					return ColorSchemeDefinition.ColorValue[colorIndex];
				}
			}
        }


    }
}
