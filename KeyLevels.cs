 #region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class KeyLevels : Indicator
	{
//		private int		ninja_Start_Time;
//		private int		ninja_End_Time;
//		private int		ninja_IB_End_Time;
		private int		latestRTHbar;
		private int		lastBar;
		private bool 	sunday = false;
		private int 	rthStartBarNum;
		private string 	yDate;
		private double	yHigh;
		private double	yLow;
		private int 	rthEndBarNum; 
		private int 	gxBars;
		private double 	gxHigh = 0.0;
	    private double  gxLow = 0.0;
		private double 	gxMid = 0.0;
		private double  todayOpen = 0.0;
		private double  Gap_D = 0.0;
		private double  Close_D = 0.0;
		private string 	message = "no message";
		private int 	preMarketLength = 0;
		private int 	MaxGapBoxSize = 10;
		private bool 	showKeyLevels = false;
		private int 	IBLength = 0;
		private double ibigh = 0.0;
	    private double ibLow = 0.0;
		private bool 	RTHchart = false;
		private double	rthHigh = 0.0;
	    private double 	rthLow = 0.0;
		private double HalfGapLevel = 0.0;
		private NinjaTrader.Gui.Tools.SimpleFont myFont = new NinjaTrader.Gui.Tools.SimpleFont("Helvetica", 12) { Size = 12, Bold = false };
				
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Key Levels";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				LineColor					= Brushes.DimGray;
				//VAColor						= Brushes.SkyBlue;
				//PocColor					= Brushes.Red;
				GapUp						= Brushes.LimeGreen;
				GapDown						= Brushes.Red;
				RTHOpen						= DateTime.Parse("08:30", System.Globalization.CultureInfo.InvariantCulture);
				RTHClose					= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
				BarsRight					= 5;
				//ShowRange					= true;
				ShowDate					= true;
				/*
				RangeHighLevel					= 4420;
				RangeVAHLevel					= 4415;
				RangePOCLevel					= 4410;
				RangeVALLevel					= 4400;
				RangeLowLevel					= 4395;
				YesterdaysPOC					= 4420;
				*/
				DailyHighAlert 					= true;
				NewHighSound 					= "NewHigh.wav";
				NewLowSound 					= "NewLow.wav";
				LoadDaysAgo						= 2;
				
				AddPlot(LineColor, "YHigh");
				AddPlot(LineColor, "YLow");
				AddPlot(LineColor, "OpenLine");
				AddPlot(LineColor, "GXHigh");
				AddPlot(LineColor, "GXLow");
				
//				AddPlot(VAColor, "RangeHigh");
//				AddPlot(VAColor, "RangeLow");
//				AddPlot(PocColor, "RangePOC");
//				AddPlot(VAColor, "RangeVAH");
//				AddPlot(VAColor, "RangeVAL");
//				AddPlot(PocColor, "YPOC");
				
				AddPlot(LineColor, "IBHigh");
				AddPlot(LineColor, "IBLow"); 
				AddPlot(LineColor, "GXMid"); 
				AddPlot(LineColor, "HalfGap"); 
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Minute, 1);
				ClearOutputWindow();
			}
		}
		
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 20 ) { return; }
			lastBar = CurrentBar - 1;
			CheckHolidayOrSunday();
			//if (yDate == "Sunday" ) { return; }
			checkDaysAgo();
			if ( sunday || !showKeyLevels ) { return; }
			SessionStart();
			SessionEnd();
			RegularSession(); 
			ShowPremarketGap();
			InitialBalance();
			Draw.TextFixed(this, "MyTextFixed", message, TextPosition.TopLeft);
		}
		
		
		/// y low on mionday and tuesday not correct 
/*
		private void PlotCompositeRange() {   
			if ( ShowRange ) {
				RangeHigh[0] = RangeHighLevel;
				RangeLow[0] = RangeLowLevel;
				RangePOC[0] = RangePOCLevel;
				RangeVAH[0] = RangeVAHLevel;
				RangeVAL[0] = RangeVALLevel;
				
				LineText(name: "range h", price: RangeHighLevel);
				LineText(name: "range l", price: RangeLowLevel);
				LineText(name: "range poc", price: RangePOCLevel);
				LineText(name: "range vah", price: RangeVAHLevel);
				LineText(name: "range val", price: RangeVALLevel);
			}
		} */
		
		private void SessionStart() {  			
			
			if (BarsInProgress == 1 && IsEqual(start: ToTime(RTHOpen), end: ToTime(Time[0])) ) {
				rthStartBarNum = CurrentBar ;
				gxBars = rthStartBarNum - rthEndBarNum;
				todayOpen = Open[0];
				Gap_D = todayOpen - Close_D; 				
				message =  Time[0].ToShortDateString() + " "  + Time[0].ToShortTimeString();
				
				if ( gxBars > 0 ) {
	                gxHigh = MAX(High, gxBars)[0];
	                gxLow = MIN(Low, gxBars)[0];
					gxMid = ((gxHigh - gxLow) * 0.5) + gxLow; 
				} 
				
				for (int i = 0; i < TradingHours.Sessions.Count; i++)
				{ 					
					int BeginTimey = TradingHours.Sessions[i].BeginTime;
					if (BeginTimey == 1700) { 
						RTHchart = false;
					} 
					if (BeginTimey == 830) { 
						RTHchart = true;
					}
				}
            }
		}
		
		private void SessionEnd() { 
			if (BarsInProgress == 1 && IsEqual(start: ToTime(RTHClose), end: ToTime(Time[0])) ) {
				Close_D = Close[0];
				rthEndBarNum = CurrentBar;
				preMarketLength = 0; 
				// find RTH High + Low
				int rthLength = rthEndBarNum - rthStartBarNum;
				if ( rthLength > 0 ) {
	                yHigh = MAX(High, rthLength)[0];
	                yLow = MIN(Low, rthLength)[0];
				}
            }
		}
		
		private void RegularSession() {   
			if (IsBetween(start: ToTime(RTHOpen), end: ToTime(RTHClose))) {
				if (yHigh > 0.0 &&  yLow > 0.0) {
					YHigh[0] = yHigh; 
					YLow[0] = yLow; 					
					LineText(name: "yh", price: yHigh);
					LineText(name: "yl", price: yLow);
					
					HalfGap[0] = HalfGapLevel;
					LineText(name: "half gap", price: HalfGapLevel);
				} 
				
				if (gxHigh > 0.0 &&  gxLow > 0.0 && !RTHchart) {
					GXHigh[0] = gxHigh; 
					GXLow[0] = gxLow; 
					GXMid[0] = gxMid;
					LineText(name: "gxH", price: gxHigh);
					LineText(name: "gxL", price: gxLow);
					LineText(name: "gxMid", price: gxMid);
				} 
				
				if (todayOpen > 0.0 ) {
					OpenLine[0] = todayOpen;   					
					LineText(name: "Open", price: todayOpen); 
				} 
				
				//PlotCompositeRange();
				message =  Time[0].ToShortDateString() + " "  + Time[0].ToShortTimeString();
				RTHrange();
			}
		}
		 
		private void RTHrange() {  
			// only alert 20 mins after open to close
			if (IsBetween(start: ToTime(RTHOpen) + 4000, end: ToTime(RTHClose))) {
				int rthLengthy =  CurrentBar - rthStartBarNum; 
				bool Debug = true;
				if ( rthLengthy> 0 ) {
					rthHigh = MAX(High, rthLengthy)[1];
		    		rthLow = MIN(Low, rthLengthy)[1]; 
					
					if (DailyHighAlert && High[0] > rthHigh) { 
						if ( Debug ) { Draw.TriangleUp(this, "newHigh"+CurrentBar, false, 0, High[0], Brushes.LimeGreen); }
						Alert("newhigh"+CurrentBar, Priority.High, "New High", 
						NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ NewHighSound,10, 
						Brushes.Black, Brushes.Yellow);  
						if ( Debug ) {  Print("New High " + Time[0].ToShortTimeString());}
					}
					
					if (DailyHighAlert && Low[0] < rthLow) { 
						if ( Debug ) { Draw.TriangleDown(this, "newHLow"+CurrentBar, false, 0, Low[0], Brushes.Red);}
						Alert("newHLow"+CurrentBar, Priority.High, "New Low", 
						NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ NewLowSound,10, 
						Brushes.Black, Brushes.Yellow);  
						if ( Debug ) {  Print("New Low " + Time[0].ToShortTimeString());}
					}
				}
			}
		}
		
		private void InitialBalance() {  
			
			if (BarsInProgress == 1 && IsEqual(start: ToTime(RTHOpen) +10000, end: ToTime(Time[0])) ) {
				IBLength += CurrentBar - rthStartBarNum; 
				ibigh = MAX(High, IBLength)[0];
	            ibLow = MIN(Low, IBLength)[0]; 
			}
			if (IsBetween(start: ToTime(RTHOpen) +10000, end: ToTime(RTHClose))) { 
				IBHigh[0] = ibigh;
				IBLow[0] = ibLow; 			
				LineText(name: "ibH", price: ibigh);
				LineText(name: "ibL", price: ibLow); 
			}
		}
				
		private bool IsEqual(int start, int end) {
			if (start == end) {
				return true;
			} else { return false; }
			
		}
		private bool IsBetween(int start, int end) {
			var Now = ToTime(Time[0]) ;
			if (Now > start && Now < end) {
				return true;
			} else { return false; }
		}
		
		private void ShowPremarketGap() {   
			if (IsBetween(start: ToTime(RTHOpen) -20000, end: ToTime(RTHOpen))) { 
				double GapHigh = 0.0;
				double	GapLow = 0.0;
				
				if ( BarsInProgress == 1 ) {
					Gap_D = Close[0] - Close_D;
					preMarketLength += 1;
					GapHigh = Close[0];
					GapLow = Close_D;
					HalfGapLevel = ((GapHigh - GapLow) / 2) + GapLow;
				}
				BoxConstructor(BoxLength: preMarketLength, BoxTopPrice: GapHigh, BottomPrice: GapLow, BoxName: "gapBox");
				message =  Time[0].ToShortDateString() + " "  + Time[0].ToShortTimeString() +  "  Pre Market Gap: " + Gap_D.ToString();
				
			}
		}
		
		private void BoxConstructor(int BoxLength, double BoxTopPrice, double BottomPrice, string BoxName) {
			if ( BoxLength < 2 || BoxTopPrice == 0.0 || BottomPrice == 0.0) { return; }
			if ( BoxLength > MaxGapBoxSize ) { BoxLength = MaxGapBoxSize; }
			double spacer = TickSize; 
			Brush	BoxColor = GapDown;
			if ( Gap_D > 0 ) {
				BoxColor = GapUp;
				spacer = -TickSize;
			}
			RemoveDrawObject(BoxName + lastBar);
			RemoveDrawObject(BoxName+ "Txt" + lastBar);
			Draw.Rectangle(this, BoxName + CurrentBar, false, BoxLength, BottomPrice, 2, BoxTopPrice, Brushes.Transparent, BoxColor, 15);
			Draw.Text(this, BoxName + "Txt" + CurrentBar, false, Gap_D.ToString(), BoxLength, BoxTopPrice + spacer, 0,  BoxColor, myFont, TextAlignment.Left, Brushes.Transparent, BoxColor, 0);
			if ( ShowDate ) {
				yDate = Time[0].DayOfWeek.ToString();
				Draw.Text(this, "day"+yDate, false, yDate, BoxLength, BottomPrice + spacer, 0,  BoxColor, myFont, TextAlignment.Left, Brushes.Transparent, BoxColor, 0);
			}
		}

		private void LineText(string name, double price) { 
			DateTime myDate = Time[0];   
				string prettyDate = myDate.ToString("MM/d/yyyy");
				//Print(prettyDate);
			Draw.Text(this, name+prettyDate, false, name, -BarsRight, price, 0,  LineColor, myFont, TextAlignment.Left, Brushes.Transparent, LineColor, 0);
		}
		
		private void checkDaysAgo() {
			// only show open line starting 5 days ago  
			if (BarsInProgress == 1 && IsEqual(start: ToTime(RTHOpen), end: ToTime(Time[0])) ) {
				//Print("inside check days "  + "  " + ToTime(Time[0]));
		        // Get the current DateTime.
		        DateTime now = DateTime.Now;
				DateTime startDTE = now.AddDays(-LoadDaysAgo);
		        // Get the TimeSpan of the difference.
		        TimeSpan elapsed = now.Subtract(startDTE);
		        // Get number of days ago.
		        double daysAgo = elapsed.TotalDays; 
				//Print("Int chart time " + ToDay(Time[0]) + " now int " + ToDay(startDTE)); 
				if ( ToDay(Time[0])  > ToDay(startDTE) ) {
					//Print("\n"+Time[0] + " is greater than 5 days ago");
					showKeyLevels = true;
				}
			}
		}
		
		private void CheckHolidayOrSunday() { 
			// bettwee sunday ope 1 pm CST and 6:30 AM CST -> check if its sunday
			if (IsBetween(start: ToTime(RTHClose) -20000, end: ToTime(RTHOpen)-20000)) {  
				DateTime myDate = Time[0];   
				string prettyDate = myDate.ToString("MM/d/yyyy"); 
				yDate = Time[0].DayOfWeek.ToString();
				//Draw.Text(this, "day"+CurrentBar, yDate, 0, High[0] + 2 * TickSize, Brushes.Blue);
				
				if (yDate == "Sunday" ) { 
					sunday = true;
					Draw.Text(this, "sunday"+prettyDate, true, yDate, 0, MAX(High, 20)[1], 1, 
						Brushes.DarkGoldenrod, myFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 50);
				} else {
					sunday = false;	
				}
			}
			
			//MARK: - TODO - Holiday doesnt print anything
			
			foreach(KeyValuePair<DateTime, string> holiday in TradingHours.Holidays)
			{
                string dateOnly = String.Format("{0:MM/dd/yyyy}", holiday.Key);
                DateTime myDate = Time[0];   
				string prettyDate = myDate.ToString("MM/d/yyyy"); 
             	//Print("holiday " + dateOnly + ",   today " + prettyDate);
				
                if (dateOnly == prettyDate)
                {
                    Print("\nToday is " + holiday.Value + "\n");
					sunday = true;
                    //if (Bars.IsFirstBarOfSession)
                    //{ 
                        
                        Draw.Text(this, "holiday"+prettyDate, true, holiday.Value, 0, MAX(High, 20)[1], 1, Brushes.DarkGoldenrod, myFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 50);
						Print(holiday.Value + "  " + Time[0].ToShortDateString() );
                    //}
                }
			}
			
		}
		
		#region Properties
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Line Color", Order=1, GroupName="Parameters")]
		public Brush LineColor
		{ get; set; }

		[Browsable(false)]
		public string LineColorSerializable
		{
			get { return Serialize.BrushToString(LineColor); }
			set { LineColor = Serialize.StringToBrush(value); }
		}			

		/*
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="VA Color", Order=2, GroupName="Parameters")]
		public Brush VAColor
		{ get; set; }

		[Browsable(false)]
		public string VAColorSerializable
		{
			get { return Serialize.BrushToString(VAColor); }
			set { VAColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="POC Color", Order=3, GroupName="Parameters")]
		public Brush PocColor
		{ get; set; }

		[Browsable(false)]
		public string PocColorSerializable
		{
			get { return Serialize.BrushToString(PocColor); }
			set { PocColor = Serialize.StringToBrush(value); }
		}			
		*/
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Gap Up Color", Order=4, GroupName="Parameters")]
		public Brush GapUp
		{ get; set; }

		[Browsable(false)]
		public string GapUpSerializable
		{
			get { return Serialize.BrushToString(GapUp); }
			set { GapUp = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Gap Down Color", Order=5, GroupName="Parameters")]
		public Brush GapDown
		{ get; set; }

		[Browsable(false)]
		public string GapDownSerializable
		{
			get { return Serialize.BrushToString(GapDown); }
			set { GapDown = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTH Open", Order=6, GroupName="Parameters")]
		public DateTime RTHOpen
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTH Close", Order=7, GroupName="Parameters")]
		public DateTime RTHClose
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Label Space Bars", Order=8, GroupName="Parameters")]
		public int BarsRight
		{ get; set; }

//		[NinjaScriptProperty]
//		[Display(Name="Show Composite Levels", Order=9, GroupName="Composite Levels")]
//		public bool ShowRange
//		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show Day Of Week", Order=9, GroupName="Parameters")]
		public bool ShowDate
		{ get; set; }
/*
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Range High Level", Order=10, GroupName="Composite Levels")]
		public double RangeHighLevel
		{ get; set; }

		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Range Low Level", Order=11, GroupName="Composite Levels")]
		public double RangeLowLevel
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Range POC Level", Order=12, GroupName="Composite Levels")]
		public double RangePOCLevel
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Range VAH Level", Order=13, GroupName="Composite Levels")]
		public double RangeVAHLevel
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Range VAL Level", Order=14, GroupName="Composite Levels")]
		public double RangeVALLevel
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Yesterdays POC", Order=15, GroupName="Composite Levels")]
		public double YesterdaysPOC
		{ get; set; }
 */
		[NinjaScriptProperty]
		[Display(Name="Daily High Alert", Order=16, GroupName="Parameters")]
		public bool DailyHighAlert
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="New High Sound", Order=17, GroupName="Parameters")]
		public string NewHighSound
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="New Low Sound", Order=18, GroupName="Parameters")]
		public string NewLowSound
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name="Maximum Days To Load", Order=19, GroupName="Parameters")]
		public int LoadDaysAgo
		{ get; set; }
		
		//-------------------------------- lines ----------------------------------
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YHigh
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YLow
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> OpenLine
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> GXHigh
		{
			get { return Values[3]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> GXLow
		{
			get { return Values[4]; }
		}
/*
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangeHigh
		{
			get { return Values[5]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangeLow
		{
			get { return Values[6]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangePOC
		{
			get { return Values[7]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangeVAH
		{
			get { return Values[8]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangeVAL
		{
			get { return Values[9]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YPOC
		{
			get { return Values[10]; }
		}
		*/
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBHigh
		{
			get { return Values[5]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBLow
		{
			get { return Values[6]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> GXMid
		{
			get { return Values[7]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> HalfGap
		{
			get { return Values[8]; }
		}
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private KeyLevels[] cacheKeyLevels;
		public KeyLevels KeyLevels(Brush lineColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showDate, bool dailyHighAlert, string newHighSound, string newLowSound, int loadDaysAgo)
		{
			return KeyLevels(Input, lineColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showDate, dailyHighAlert, newHighSound, newLowSound, loadDaysAgo);
		}

		public KeyLevels KeyLevels(ISeries<double> input, Brush lineColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showDate, bool dailyHighAlert, string newHighSound, string newLowSound, int loadDaysAgo)
		{
			if (cacheKeyLevels != null)
				for (int idx = 0; idx < cacheKeyLevels.Length; idx++)
					if (cacheKeyLevels[idx] != null && cacheKeyLevels[idx].LineColor == lineColor && cacheKeyLevels[idx].GapUp == gapUp && cacheKeyLevels[idx].GapDown == gapDown && cacheKeyLevels[idx].RTHOpen == rTHOpen && cacheKeyLevels[idx].RTHClose == rTHClose && cacheKeyLevels[idx].BarsRight == barsRight && cacheKeyLevels[idx].ShowDate == showDate && cacheKeyLevels[idx].DailyHighAlert == dailyHighAlert && cacheKeyLevels[idx].NewHighSound == newHighSound && cacheKeyLevels[idx].NewLowSound == newLowSound && cacheKeyLevels[idx].LoadDaysAgo == loadDaysAgo && cacheKeyLevels[idx].EqualsInput(input))
						return cacheKeyLevels[idx];
			return CacheIndicator<KeyLevels>(new KeyLevels(){ LineColor = lineColor, GapUp = gapUp, GapDown = gapDown, RTHOpen = rTHOpen, RTHClose = rTHClose, BarsRight = barsRight, ShowDate = showDate, DailyHighAlert = dailyHighAlert, NewHighSound = newHighSound, NewLowSound = newLowSound, LoadDaysAgo = loadDaysAgo }, input, ref cacheKeyLevels);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.KeyLevels KeyLevels(Brush lineColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showDate, bool dailyHighAlert, string newHighSound, string newLowSound, int loadDaysAgo)
		{
			return indicator.KeyLevels(Input, lineColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showDate, dailyHighAlert, newHighSound, newLowSound, loadDaysAgo);
		}

		public Indicators.KeyLevels KeyLevels(ISeries<double> input , Brush lineColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showDate, bool dailyHighAlert, string newHighSound, string newLowSound, int loadDaysAgo)
		{
			return indicator.KeyLevels(input, lineColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showDate, dailyHighAlert, newHighSound, newLowSound, loadDaysAgo);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.KeyLevels KeyLevels(Brush lineColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showDate, bool dailyHighAlert, string newHighSound, string newLowSound, int loadDaysAgo)
		{
			return indicator.KeyLevels(Input, lineColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showDate, dailyHighAlert, newHighSound, newLowSound, loadDaysAgo);
		}

		public Indicators.KeyLevels KeyLevels(ISeries<double> input , Brush lineColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showDate, bool dailyHighAlert, string newHighSound, string newLowSound, int loadDaysAgo)
		{
			return indicator.KeyLevels(input, lineColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showDate, dailyHighAlert, newHighSound, newLowSound, loadDaysAgo);
		}
	}
}

#endregion
