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
	public class DailyHighLowSharpDX : Indicator
	{
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
		
		private Series<double> yestHighSeries;
		private Series<double> yestLowSeries;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Daily High Low Sharp DX";
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
				
				LineColor					= Brushes.DarkGray;
				BarsRight					= 1;
				RTHOpen						= DateTime.Parse("08:30", System.Globalization.CultureInfo.InvariantCulture);
				RTHClose					= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
				
				GapUp						= Brushes.LimeGreen;
				GapDown						= Brushes.Red;
				ShowDate					= true;
				ShowGap						= false;
				
				AddPlot(Brushes.DimGray, "Open");
			    AddPlot(Brushes.DimGray, "Y High");
			    AddPlot(Brushes.DimGray, "Y Low");
			    AddPlot(Brushes.DimGray, "Gx High");
				AddPlot(Brushes.DimGray, "Gx Low");
				AddPlot(Brushes.DimGray, "Gx Mid");
				AddPlot(Brushes.DimGray, "IB High");
				AddPlot(Brushes.DimGray, "IB Low");
				AddPlot(Brushes.DimGray, "Half Gap"); 
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Minute, 1);
				ClearOutputWindow();
			}
			else if (State == State.DataLoaded)
		    { 
		        yestHighSeries = new Series<double>(this, MaximumBarsLookBack.Infinite);
				yestLowSeries = new Series<double>(this, MaximumBarsLookBack.Infinite);
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			// implicitly recreate and dispose of brush on each render pass
//			  using (SharpDX.Direct2D1.SolidColorBrush dxBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.Blue))
//			  {
//			    RenderTarget.FillRectangle(new SharpDX.RectangleF(ChartPanel.X, ChartPanel.Y, ChartPanel.W, ChartPanel.H), dxBrush);
//			  }
			
			  // call the base.OnRender() to ensure standard Plots work as designed
  			  base.OnRender(chartControl, chartScale);
			  // get the starting and ending bars from what is rendered on the chart
			  float startX = chartControl.GetXByBarIndex(ChartBars, ChartBars.FromIndex);
			  float endX = chartControl.GetXByBarIndex(ChartBars, ChartBars.ToIndex);
			 
			  // Loop through each Plot Values on the chart
			  for (int seriesCount = 0; seriesCount < Values.Length; seriesCount++)
			  {
			    // get the value at the last bar on the chart (if it has been set)
			    if (Values[seriesCount].IsValidDataPointAt(ChartBars.ToIndex))
			    {
			        double plotValue = Values[seriesCount].GetValueAt(ChartBars.ToIndex);
			 
			        // convert the plot value to the charts "Y" axis point
			        float chartScaleYValue = chartScale.GetYByValue(plotValue);
			 
			        // calculate the x and y values for the line to start and end
			        SharpDX.Vector2 startPoint = new SharpDX.Vector2(startX, chartScaleYValue);
			        SharpDX.Vector2 endPoint = new SharpDX.Vector2(endX, chartScaleYValue);
			 
			        // draw a line between the start and end point at each plot using the plots SharpDX Brush color and style
			        RenderTarget.DrawLine(startPoint, endPoint, Plots[seriesCount].BrushDX,
			          Plots[seriesCount].Width, Plots[seriesCount].StrokeStyle);
			 
			        // use the chart control text form to draw plot values along the line
			        SharpDX.DirectWrite.TextFormat textFormat = chartControl.Properties.LabelFont.ToDirectWriteTextFormat();
			 
			        // calculate the which will be rendered at each plot using it the plot name and its price
			        string textToRender = Plots[seriesCount].Name + ": " + plotValue;
			 
			        // calculate the layout of the text to be drawn
			        SharpDX.DirectWrite.TextLayout textLayout = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory,
			          textToRender, textFormat, 200, textFormat.FontSize);
			 
			        // draw a line at each plot using the plots SharpDX Brush color at the calculated start point
			        RenderTarget.DrawTextLayout(startPoint, textLayout, Plots[seriesCount].BrushDX);
			 
			        // dipose of the unmanaged resources used
			        textLayout.Dispose();
			        textFormat.Dispose();
			    }
			  }
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 20 ) { return; }
			lastBar = CurrentBar - 1;
			CheckHolidayOrSunday();
			//if ( sunday || !showKeyLevels ) { return; }
			ShowPremarketGap();
			SessionStart();
			RegularSession();  
			InitialBalance();
			SessionEnd();
		}
		
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
            }
		}
		
		private void SessionEnd() { 
			if (BarsInProgress == 1 && IsEqual(start: ToTime(RTHClose), end: ToTime(Time[0])) ) {
				Close_D = Close[0];
				IBLength  = 0;
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
					Values[8][0] = HalfGapLevel;
					Values[1][0] = yHigh;
					Values[2][0] = yLow;
					LineText(name: "Y High", price: yHigh); 
					LineText(name: "Y Low", price: yLow); 
					LineText(name: "half gap", price: HalfGapLevel);
				} 
				
				if (todayOpen > 0.0 ) {
					Values[0][0] = todayOpen;  					
					LineText(name: "Open", price: todayOpen); 
				} 
				
				if (gxHigh > 0.0 &&  gxLow > 0.0 && !RTHchart) {
					Values[3][0] = gxHigh; 
					Values[4][0] = gxLow; 
					Values[5][0] = gxMid;
					LineText(name: "Gx High", price: gxHigh);
					LineText(name: "Gx Low", price: gxLow);
					LineText(name: "Gx Mid", price: gxMid);
				} 
			} 
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
				if ( ShowGap ) { BoxConstructor(BoxLength: preMarketLength, BoxTopPrice: GapHigh, BottomPrice: GapLow, BoxName: "gapBox"); }
				message =  Time[0].ToShortDateString() + " "  + Time[0].ToShortTimeString() +  "  Pre Market Gap: " + Gap_D.ToString();
				
			}
		}
		
		private void InitialBalance() {  
			
			if (BarsInProgress == 1 && IsEqual(start: ToTime(RTHOpen) +10000, end: ToTime(Time[0])) ) {
				IBLength += CurrentBar - rthStartBarNum; 
				ibigh = MAX(High, IBLength)[0];
	            ibLow = MIN(Low, IBLength)[0]; 
			}
			if (IsBetween(start: ToTime(RTHOpen) +10000, end: ToTime(RTHClose))) { 
				Values[6][0] = ibigh;
				Values[7][0] = ibLow; 			
				LineText(name: "ibH", price: ibigh);
				LineText(name: "ibL", price: ibLow); 
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
			Draw.Text(this, name+prettyDate, false, name, -BarsRight, price, 0,  LineColor, myFont, TextAlignment.Left, Brushes.Transparent, LineColor, 0);
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

		/// <summary>
		/// --------------------------- Gap ---------------------
		/// </summary>
		
		[NinjaScriptProperty]
		[Display(Name="Show Gap Area", Order=1, GroupName="Gap Visualization")]
		public bool ShowGap
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show Day Of Week", Order=2, GroupName="Gap Visualization")]
		public bool ShowDate
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Gap Up Color", Order=3, GroupName="Gap Visualization")]
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
		[Display(Name="Gap Down Color", Order=4, GroupName="Gap Visualization")]
		public Brush GapDown
		{ get; set; }

		[Browsable(false)]
		public string GapDownSerializable
		{
			get { return Serialize.BrushToString(GapDown); }
			set { GapDown = Serialize.StringToBrush(value); }
		}	
		
		/// <summary>
		/// ---------------------------Parameters ---------------------
		/// </summary>
		/// 
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Right Label Color", Order=1, GroupName="Parameters")]
		public Brush LineColor
		{ get; set; }

		[Browsable(false)]
		public string LineColorSerializable
		{
			get { return Serialize.BrushToString(LineColor); }
			set { LineColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Label Spaces To Right", Order=8, GroupName="Parameters")]
		public int BarsRight
		{ get; set; }
		
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
		
		
		
		#endregion
		
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DailyHighLowSharpDX[] cacheDailyHighLowSharpDX;
		public DailyHighLowSharpDX DailyHighLowSharpDX(bool showGap, bool showDate, Brush gapUp, Brush gapDown, Brush lineColor, int barsRight, DateTime rTHOpen, DateTime rTHClose)
		{
			return DailyHighLowSharpDX(Input, showGap, showDate, gapUp, gapDown, lineColor, barsRight, rTHOpen, rTHClose);
		}

		public DailyHighLowSharpDX DailyHighLowSharpDX(ISeries<double> input, bool showGap, bool showDate, Brush gapUp, Brush gapDown, Brush lineColor, int barsRight, DateTime rTHOpen, DateTime rTHClose)
		{
			if (cacheDailyHighLowSharpDX != null)
				for (int idx = 0; idx < cacheDailyHighLowSharpDX.Length; idx++)
					if (cacheDailyHighLowSharpDX[idx] != null && cacheDailyHighLowSharpDX[idx].ShowGap == showGap && cacheDailyHighLowSharpDX[idx].ShowDate == showDate && cacheDailyHighLowSharpDX[idx].GapUp == gapUp && cacheDailyHighLowSharpDX[idx].GapDown == gapDown && cacheDailyHighLowSharpDX[idx].LineColor == lineColor && cacheDailyHighLowSharpDX[idx].BarsRight == barsRight && cacheDailyHighLowSharpDX[idx].RTHOpen == rTHOpen && cacheDailyHighLowSharpDX[idx].RTHClose == rTHClose && cacheDailyHighLowSharpDX[idx].EqualsInput(input))
						return cacheDailyHighLowSharpDX[idx];
			return CacheIndicator<DailyHighLowSharpDX>(new DailyHighLowSharpDX(){ ShowGap = showGap, ShowDate = showDate, GapUp = gapUp, GapDown = gapDown, LineColor = lineColor, BarsRight = barsRight, RTHOpen = rTHOpen, RTHClose = rTHClose }, input, ref cacheDailyHighLowSharpDX);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DailyHighLowSharpDX DailyHighLowSharpDX(bool showGap, bool showDate, Brush gapUp, Brush gapDown, Brush lineColor, int barsRight, DateTime rTHOpen, DateTime rTHClose)
		{
			return indicator.DailyHighLowSharpDX(Input, showGap, showDate, gapUp, gapDown, lineColor, barsRight, rTHOpen, rTHClose);
		}

		public Indicators.DailyHighLowSharpDX DailyHighLowSharpDX(ISeries<double> input , bool showGap, bool showDate, Brush gapUp, Brush gapDown, Brush lineColor, int barsRight, DateTime rTHOpen, DateTime rTHClose)
		{
			return indicator.DailyHighLowSharpDX(input, showGap, showDate, gapUp, gapDown, lineColor, barsRight, rTHOpen, rTHClose);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DailyHighLowSharpDX DailyHighLowSharpDX(bool showGap, bool showDate, Brush gapUp, Brush gapDown, Brush lineColor, int barsRight, DateTime rTHOpen, DateTime rTHClose)
		{
			return indicator.DailyHighLowSharpDX(Input, showGap, showDate, gapUp, gapDown, lineColor, barsRight, rTHOpen, rTHClose);
		}

		public Indicators.DailyHighLowSharpDX DailyHighLowSharpDX(ISeries<double> input , bool showGap, bool showDate, Brush gapUp, Brush gapDown, Brush lineColor, int barsRight, DateTime rTHOpen, DateTime rTHClose)
		{
			return indicator.DailyHighLowSharpDX(input, showGap, showDate, gapUp, gapDown, lineColor, barsRight, rTHOpen, rTHClose);
		}
	}
}

#endregion
