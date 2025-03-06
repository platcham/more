/*
 * Screens.cs
 * 
 * In a multi monitor environment draws your screens (monitors) 
 * and lets you select one. This control is useful if you'd
 * like to move a window in your application to another screen
 * and you'd like to give user the choice.
 *
 * MIT License (see: LICENSE)
 * Copyright (c) 2020 Tomaz Stih
 * 
 * 01.02.2021   tstih
 * 
 * 3.5.2025 platcham - 
 * Changes:
 *      changed to .net8.0 and targeting windows 10.0.26100.0
 *      added multi monitor selection 
 *      fixed bug with clicking outside of monitors causing crashes
 *      Corrected for displays with different scaling factors
 *      display indexes now based off of display name in Screen.AllScreens (so \\.\\DISPLAY1 is 1, etc)
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;

namespace More.Windows.Forms
{
    public class Monitors : ControlBase
    {
        #region EnumDisplayDevices Definition

        // create structure for enumDisplayDevices to enable getting scaling factor
        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);


        #endregion

        #region Private(s)
        // Screen rectangles.
        private List<MonitorRectangle> _srects;
        #endregion // Private(s)

        #region Ctor
        public Monitors() : base()
        {
            // Set default property values.
            SetDefaults();
        }

        private void SetDefaults()
        {
            // Display numbers and edges?
            _showNumber = true;
            _showEdge = true;

            // Edge!
            _edgeThickness = 10;

            // Active screen is first screen.
            _selected = -1;
            _active = -1;

            //Multi select is off by default
            _allowMultiSelect = false;
            _multiSelected = new List<int>();
            _multiSelected.Add(0);

            // Colours.
            _monitorBackColor = Color.FromKnownColor(KnownColor.InactiveCaption);
            _monitorTextBackColor = Color.FromKnownColor(KnownColor.Window);
            _monitorTextForeColor = Color.FromKnownColor(KnownColor.WindowText);
            _selectedMonitorBackColor = Color.FromKnownColor(KnownColor.Highlight);
            _selectedMonitorTextBackColor = Color.FromKnownColor(KnownColor.Window);
            _selectedMonitorTextForeColor = Color.FromKnownColor(KnownColor.WindowText);
            _activeMonitorBackColor = Color.FromKnownColor(KnownColor.ActiveCaption);
            _activeMonitorTextBackColor = Color.FromKnownColor(KnownColor.Window);
            _activeMonitorTextForeColor = Color.FromKnownColor(KnownColor.WindowText);
            _edgeColor = Color.FromKnownColor(KnownColor.ScrollBar);
            _edgeDarkColor= Color.FromKnownColor(KnownColor.ControlDark);
            _edgeLightColor= Color.FromKnownColor(KnownColor.ControlLight);
        }
        #endregion

        #region Override(s)
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // First layout monitors.
            DoLayout();


            // Now draw.
            foreach(MonitorRectangle r in _srects)
            {
                if (r.Rectangle.Width > 8 && r.Rectangle.Height > 8)
                {
                    DrawMonitor(g, r.Rectangle, r.Index);
                    if (_showEdge) DrawEdge(g, r.Rectangle);
                    if (_showNumber) DrawMonitorNumber(g, r.Rectangle, r.Index);
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            // Whatever is active is not active anymore.
            _active = -1; Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int under = GetIndexAt(e.Location);
            if (under != _active) { _active = under; Invalidate(); }
        }


        protected override void OnMouseClick(MouseEventArgs e)
        {
            // What is under the mouse?
            int under = GetIndexAt(e.Location);
            Screen underScreen = GetScreenAtIndex(under);

            //if multiselect is allowed, add or remove monitor from selected list
            if (_allowMultiSelect)
            {
                // Click on selected again?
                if (_multiSelected.Contains(under) && under != -1)
                {
                    _multiSelected.Remove(under);
                    MonitorUnselected.Raise(this, new MonitorEventArgs() { MonitorIndex = under, Screen = underScreen });
                }
                else if (under == -1)
                {
                    // do nothing, clicked outside of monitors
                }
                else
                {
                    // add monitor to selected list
                    _multiSelected.Add(under);
                    MonitorSelected.Raise(this, new MonitorEventArgs() { MonitorIndex = under, Screen = underScreen });
                }
            }
            else
            {
                // Click on selected again?
                if (under == _selected && under != -1)
                {
                    _selected = -1;// Unselect!
                    MonitorUnselected.Raise(this, new MonitorEventArgs() { MonitorIndex = under, Screen = underScreen });
                }
                else if (under == -1)
                {
                    //do nothing, clicked outside of monitors
                }
                else
                {
                    _selected = under;
                    MonitorSelected.Raise(this, new MonitorEventArgs() { MonitorIndex = under, Screen = underScreen });
                }
                Invalidate();
            }
            
        }
        #endregion // Override(s)

        #region Event(s)
        [Description("When the monitor is selected!"), Category("Mouse")]
        public event EventHandler<MonitorEventArgs> MonitorSelected;

        [Description("When the monitor is deselected."), Category("Mouse")]
        public event EventHandler<MonitorEventArgs> MonitorUnselected;
        #endregion // Event(s)

        #region Properties
        private int _selected;
        /// <summary>
        /// Display number of selected monitor. Selected monitor is highlighted.
        /// </summary>
        [Description("Display Number of selected monitor. Selected monitor is highlighted."), Category("Appearance")]
        public int Selected { get { return _selected; } set { _selected = value; Invalidate(); } }

        private bool _allowMultiSelect;
        /// <summary>
        /// bool to allow multiple monitors to be selected
        /// </summary>
        [Description("bool to allow multiple monitors to be selected"), Category("Appearance")]
        public bool AllowMultiSelect { get { return _allowMultiSelect; } set { _allowMultiSelect = value; Invalidate(); } }

        private List<int> _multiSelected;
        /// <summary>
        /// list of selected monitors when AllowMultiSelect is true
        /// </summary>
        public List<int> MultiSelected
        {
            get
            {
                return _multiSelected;
            }
            set
            {
                _multiSelected = value;
                Invalidate();
            }
        }

        private int _active;
        /// <summary>
        /// Display Number of active monitor. Active monitor is temporarily of different color (as long as mouse is over it).
        /// </summary>
        [Description("Display Number of active monitor.Active monitor is temporarily of different color(as long as mouse is over it)."), Category("Appearance")]
        public int Active { get { return _active; } set { _active = value; Invalidate(); } }

        private Color _monitorBackColor;
        /// <summary>
        /// Back color of non selected and non active monitor.
        /// </summary>
        [Description("Back color of non selected and non active monitor."), Category("Appearance")]
        public Color MonitorBackColor { get { return _monitorBackColor; } set { _monitorBackColor = value; Invalidate(); } }

        private Color _monitorTextBackColor;
        /// <summary>
        /// Text fill color.
        /// </summary>
        [Description("Text fill color."), Category("Appearance")]
        public Color MonitorTextBackColor { get { return _monitorTextBackColor; } set { _monitorTextBackColor = value; Invalidate(); } }

        private Color _monitorTextForeColor;
        /// <summary>
        /// Text border color of monitor.
        /// </summary>
        [Description("Text border color of monitor."), Category("Appearance")]
        public Color MonitorTextForeColor { get { return _monitorTextForeColor; } set { _monitorTextForeColor = value; Invalidate(); } }

        private Color _selectedMonitorBackColor;
        /// <summary>
        /// Back color of selected monitor.
        /// </summary>
        [Description("Back color of selected monitor."), Category("Appearance")]
        public Color SelectedMonitorBackColor { get { return _selectedMonitorBackColor; } set { _selectedMonitorBackColor = value; Invalidate(); } }

        private Color _selectedMonitorTextBackColor;
        /// <summary>
        /// Text fill color for selected monitor
        /// </summary>
        [Description("Text fill color for selected monitor."), Category("Appearance")]
        public Color SelectedMonitorTextBackColor { get { return _selectedMonitorTextBackColor; } set { _selectedMonitorTextBackColor = value; Invalidate(); } }

        private Color _selectedMonitorTextForeColor;
        /// <summary>
        /// Text border color of selected monitor.
        /// </summary>
        [Description("Text border color of selected monitor."), Category("Appearance")]
        public Color SelectedMonitorTextForeColor { get { return _selectedMonitorTextForeColor; } set { _selectedMonitorTextForeColor = value; Invalidate(); } }

        private Color _activeMonitorBackColor;
        /// <summary>
        /// Back color of active monitor.
        /// </summary>
        [Description("Back color of active monitor."), Category("Appearance")]
        public Color ActiveMonitorBackColor { get { return _activeMonitorBackColor; } set { _activeMonitorBackColor = value; Invalidate(); } }

        private Color _activeMonitorTextBackColor;
        /// <summary>
        /// Text fill color for active monitor.
        /// </summary>
        [Description("Text fill color for active monitor."), Category("Appearance")]
        public Color ActiveMonitorTextBackColor { get { return _activeMonitorTextBackColor; } set { _activeMonitorTextBackColor = value; Invalidate(); } }

        private Color _activeMonitorTextForeColor;
        /// <summary>
        /// Text border color of active monitor.
        /// </summary>
        [Description("Text border color of active monitor."), Category("Appearance")]
        public Color ActiveMonitorTextForeColor { get { return _activeMonitorTextForeColor; } set { _activeMonitorTextForeColor = value; Invalidate(); } }

        private bool _showNumber;
        /// <summary>
        /// Do we show numbers on monitors?
        /// </summary>
        [Description("Do we show numbers on monitors?"), Category("Appearance")]
        public bool ShowNumber { get { return _showNumber; } set { _showNumber = value; Invalidate(); } }

        private bool _showEdge;
        /// <summary>
        /// Do we show the edge on monitors?
        /// </summary>
        [Description("Do we show the edge on monitors?"), Category("Appearance")]
        public bool ShowEdge { get { return _showEdge; } set { _showEdge = value; Invalidate(); } }

        private int _edgeThickness;
        /// <summary>
        /// If we show the edge, how thick is it?
        /// </summary>
        [Description(" If we show the edge, how thick is it?"), Category("Appearance")]
        public int EdgeThickness { get { return _edgeThickness; } set { _edgeThickness = value; Invalidate(); } }

        private Color _edgeLightColor;
        /// <summary>
        /// What is the light color of the edge for 3D effect?
        /// </summary>
        [Description("What is the light color of the edge for 3D effect?"), Category("Appearance")]
        public Color EdgeLightColor { get { return _edgeLightColor; } set { _edgeLightColor = value; Invalidate(); } }

        private Color _edgeDarkColor;
        /// <summary>
        /// What is the dark color of the edge for 3D effect?
        /// </summary>
        [Description("What is the dark color of the edge for 3D effect?"), Category("Appearance")]
        public Color EdgeDarkColor { get { return _edgeDarkColor; } set { _edgeDarkColor = value; Invalidate(); } }

        private Color _edgeColor;
        /// <summary>
        /// What is color of the edge?
        /// </summary>
        [Description("What is color of the edge?"), Category("Appearance")]
        public Color EdgeColor { get { return _edgeColor; } set { _edgeColor = value; Invalidate(); } }

        #endregion // Properties

        #region Helper(s)
        private int GetIndexAt(Point pt)
        {
            // No rects yet.
            if (_srects == null) return -1;

            // Get index.
            int under = -1;
            foreach (MonitorRectangle mon in _srects)
            {
                if (mon.Rectangle.Contains(pt)) under = mon.Index;
            }
            return under;
        }

        private Screen GetScreenAtIndex(int index)
        {
            for (int i =0; i < Screen.AllScreens.Length; i++)
            {
                if (int.Parse(new string(Screen.AllScreens[i].DeviceName.Where(c => char.IsDigit(c)).ToArray())) == index)
                    return Screen.AllScreens[i];
            }

            return null;
        }

        private Color GetMonitorBackColor(int i)
        {
            if (_allowMultiSelect)
            {
                if (_multiSelected == null)
                    _multiSelected = new List<int>();
                
                if (_multiSelected.Contains(i))
                    return _selectedMonitorBackColor;
                else if (i == _active)
                    return _activeMonitorBackColor;
                else
                    return _monitorBackColor;
            }
            else
            {
                if (i == _selected)
                    return _selectedMonitorBackColor;
                else if (i == _active)
                    return _activeMonitorBackColor;
                else
                    return _monitorBackColor;
            }
        }

        private Color GetMonitorTextBackColor(int i)
        {
            if (_allowMultiSelect)
            {
                if (_multiSelected.Contains(i))
                    return _selectedMonitorTextBackColor;
                else if (i == _active)
                    return _activeMonitorTextBackColor;
                else
                    return _monitorTextBackColor;
            }
            else
            {
                if (i == _selected)
                    return _selectedMonitorTextBackColor;
                else if (i == _active)
                    return _activeMonitorTextBackColor;
                else
                    return _monitorTextBackColor;
            }
        }


        private Color GetMonitorTextForeColor(int i)
        {
            if (_allowMultiSelect)
            {
                if (_multiSelected.Contains(i))
                    return _selectedMonitorTextForeColor;
                else if (i == _active)
                    return _activeMonitorTextForeColor;
                else
                    return _monitorTextForeColor;
            }
            else
            {
                if (i == _selected)
                    return _selectedMonitorTextForeColor;
                else if (i == _active)
                    return _activeMonitorTextForeColor;
                else
                    return _monitorTextForeColor;
            }
        }

        private void DrawMonitor(Graphics g, Rectangle r, int i) { 
            using(Brush backBrush=new SolidBrush(GetMonitorBackColor(i)))
            using(Pen forePen=new Pen(ForeColor))
            {
                g.FillRectangle(backBrush, r );
                g.DrawRectangle(forePen, r);
            }
        }

        private void DrawEdge(Graphics g, Rectangle r)
        {
            int ooffs = 1, ioffs = _edgeThickness;
            Rectangle outer = new Rectangle(r.X + ooffs, r.Y  +ooffs, r.Width-2*ooffs, r.Height-2*ooffs);
            Rectangle inner = new Rectangle(r.X + ioffs, r.Y + ioffs, r.Width - 2*ioffs, r.Height - 2*ioffs);
            Region orgn = new Region(outer), irgn = new Region(inner);
            orgn.Exclude(irgn);
            using(Brush edgeBrush=new SolidBrush(_edgeColor)) g.FillRegion(edgeBrush, orgn);
            g.DrawBorder(_edgeDarkColor, _edgeLightColor, outer, 1);
            g.DrawBorder(_edgeLightColor,_edgeDarkColor, inner, 1);
        }

        private void DrawMonitorNumber(Graphics g, Rectangle r, int i) {
            string s = (i).ToString();
            float fontSize = Font.Size; // Measure font size.
            SizeF textSize = g.MeasureString(s, Font); // Measure text size for font.
            float vratio = textSize.Height / fontSize;
            float hratio = textSize.Width / fontSize;
            float newSize = r.Height / vratio;
            if (r.Width / hratio < newSize) newSize = r.Width / hratio;

            //using (Font fFit = new Font(Font.FontFamily, newSize, FontStyle.Regular))
            using (StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (Brush backBrush = new SolidBrush(GetMonitorTextBackColor(i)))
            using (Pen forePen = new Pen(GetMonitorTextForeColor(i)))
                DrawOutlineText(g, forePen, backBrush, s, Font.FontFamily, newSize, sf, r);
        }

        private void DoLayout()
        {
            // Initiate screen rectangles.
            _srects = new List<MonitorRectangle>();

            // Iterate screens to find total dimensions.
            int minx=int.MaxValue, miny=int.MaxValue, maxx=int.MinValue, maxy=int.MinValue;
            foreach(var s in Screen.AllScreens)
            {
                // get scaled bounds
                DEVMODE dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                EnumDisplaySettings(s.DeviceName, -1, ref dm);

                var scalingFactor = Math.Round(Decimal.Divide(dm.dmPelsWidth, s.Bounds.Width), 2);
                var newBounds = new Rectangle(s.Bounds.X, s.Bounds.Y, (int)Math.Round(s.Bounds.Width * scalingFactor), (int)Math.Round(s.Bounds.Height * scalingFactor));
                var newBoundsRight = s.Bounds.Left + newBounds.Width;
                var newBoundsBottom = s.Bounds.Top + newBounds.Height;

                // And recalc min and max values.
                if (s.Bounds.Left < minx) minx = s.Bounds.Left;
                if (newBoundsRight > maxx) maxx = newBoundsRight;
                if (s.Bounds.Top < miny) miny = s.Bounds.Top;
                if (newBoundsBottom > maxy) maxy = newBoundsBottom;
            }

            // Calculate size to fit factor.
            int w = maxx - minx, h = maxy - miny;
            Rectangle totalRect = new Rectangle(0, 0, w, h);
            Rectangle client = new Rectangle(
                ClientRectangle.Left + Margin.Left,
                ClientRectangle.Top + Margin.Top,
                ClientRectangle.Width - Margin.Left - Margin.Right,
                ClientRectangle.Height - Margin.Top - Margin.Bottom
                );
            float f = totalRect.SizeToFitFactor(client);


            // And recalculate all rectangles.
            foreach (var s in Screen.AllScreens)
            {
                // get scaled bounds
                DEVMODE dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                EnumDisplaySettings(s.DeviceName, -1, ref dm);

                var scalingFactor = Math.Round(Decimal.Divide(dm.dmPelsWidth, s.Bounds.Width), 2);
                var newBounds = new Rectangle(s.Bounds.X, s.Bounds.Y, (int)Math.Round(s.Bounds.Width * scalingFactor), (int)Math.Round(s.Bounds.Height * scalingFactor));

                // Get screen rectangle.
                // To draw at 0,0 you will have to offset all rectangles by minx, miny.
                RectangleF srect = new RectangleF(
                    Margin.Left + (s.Bounds.Left - minx) * f + Padding.Left,
                    Margin.Top + (s.Bounds.Top - miny) * f + Padding.Top,
                    (float)(newBounds.Width) * f - Padding.Left - Padding.Right,
                    (float)(newBounds.Height) * f - Padding.Top - Padding.Bottom
                    );

                // And add it.
                MonitorRectangle mr = new MonitorRectangle();
                mr.Index = int.Parse(new string(s.DeviceName.Where(c => char.IsDigit(c)).ToArray()));
                mr.Rectangle = Rectangle.Round(srect);
                _srects.Add(mr);
            }
        }

        private void DrawOutlineText(Graphics g, Pen forePen, Brush backBrush, string s, FontFamily ff, float fontSize, StringFormat sf, Rectangle rect)
        {
            // assuming g is the Graphics object on which you want to draw the text
            GraphicsPath p = new GraphicsPath();
            p.AddString(
                s,
                ff,
                (int)FontStyle.Regular,
                fontSize,//g.DpiY * fontSize / 72,
                rect,
                sf);
            g.FillPath(backBrush, p);
            g.DrawPath(forePen, p);
        }
        #endregion // Helper(s)
    }

    public class MonitorEventArgs: EventArgs
    {
        public Screen Screen { get; internal set; }
        public int MonitorIndex { get; internal set; }
    }

    public class MonitorRectangle
    {
        public Rectangle Rectangle { get; internal set; }
        public int Index { get; internal set; }
    }
}
