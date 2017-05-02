using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SlidingWindowControl
{
    /// <summary>
    /// Interaction logic for SlidingWindow.xaml
    /// </summary>
    public partial class SlidingWindow : UserControl
    {
        private bool _isMouseDown;

        public SlidingWindow()
        {
            InitializeComponent();

            this.SizeChanged += SlidingWindow_SizeChanged;
            this.Loaded += SlidingWindow_Loaded;
        }

        private void SlidingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var myWindow = Window.GetWindow(this);
            if(null != myWindow)
            {
                myWindow.MouseLeave += MyWindow_MouseLeave;
            }
        }

        private void MyWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            _isMouseDown = false; 
            // can probably do this better some other way, hack to deal with the case where
            // they started dragging, pulled the mouse out of the window, and then released the
            // button and i didn't hear about it

            // TODO: probably a global subscription i could do that would work better for this...
        }

        private void SlidingWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            OnSomethingChanged();
        }

        public double RangeMinimum
        {
            get { return (double)GetValue(RangeMinimumProperty); }
            set { SetValue(RangeMinimumProperty, value); }
        }

        public static readonly DependencyProperty RangeMinimumProperty =
            DependencyProperty.Register("RangeMinimum", typeof(double), typeof(SlidingWindow),
                new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnPropertyChanged)));

        public double RangeWindowSize
        {
            get { return (double)GetValue(RangeWindowSizeProperty); }
            set { SetValue(RangeWindowSizeProperty, value); }
        }

        public static readonly DependencyProperty RangeWindowSizeProperty =
            DependencyProperty.Register("RangeWindowSize", typeof(double), typeof(SlidingWindow),
                new PropertyMetadata(100.0, new PropertyChangedCallback(OnPropertyChanged)));

        public double RangeMaximum
        {
            get { return (double)GetValue(RangeMaximumProperty); }
            set { SetValue(RangeMaximumProperty, value); }
        }

        public static readonly DependencyProperty RangeMaximumProperty =
            DependencyProperty.Register("RangeMaximum", typeof(double), typeof(SlidingWindow),
                new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnPropertyChanged)));

        public double OverallMinimum
        {
            get { return (double)GetValue(OverallMinimumProperty); }
            set { SetValue(OverallMinimumProperty, value); }
        }

        public static readonly DependencyProperty OverallMinimumProperty =
            DependencyProperty.Register("OverallMinimum", typeof(double), typeof(SlidingWindow), 
                new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnPropertyChanged)));


        public double OverallMaximum
        {
            get { return (double)GetValue(OverallMaximumProperty); }
            set { SetValue(OverallMaximumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OverallMaximum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OverallMaximumProperty =
            DependencyProperty.Register("OverallMaximum", typeof(double), typeof(SlidingWindow), 
                new FrameworkPropertyMetadata(1.0, new PropertyChangedCallback(OnPropertyChanged)));
        
        // TODO: commands
        private static void OnPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var me = sender as SlidingWindow;
            if(me != null)
            {
                me.OnSomethingChanged();
            }
        }   

        private void OnSomethingChanged()
        {
            // Figure out what the actual width is
            var myTotalWidth = ActualWidth;

            if(myTotalWidth > 0)
            {
                // Figure out the ratios of each bit

                // Figure out ratio of thumb width to overall width
                var totalAvailableRange = Math.Abs(OverallMaximum - OverallMinimum);
                var thumbWidthPercentage = (RangeWindowSize) / totalAvailableRange;

                // Figure out the offset of the thumb relative to its ("minimum") position in the world
                var minimumPossible = OverallMinimum;
                var maximumPossible = OverallMaximum - RangeWindowSize;

                var downWidthInUnits = Math.Min(maximumPossible, Math.Max(minimumPossible, RangeMinimum));
                var downWidthInPercentage = downWidthInUnits / totalAvailableRange;

                var upWidthInUnits = Math.Max(0, myTotalWidth - (thumbWidthPercentage + _downButton.Width));
                var upWidthInPercentage = upWidthInUnits / totalAvailableRange;

                _downButton.Width = downWidthInPercentage * myTotalWidth;
                _thumbGrid.Width = thumbWidthPercentage * myTotalWidth;
            }            
        }

        private void SetFromMiddle(double falseCentre)
        {
            var proposedMinimum = falseCentre - (RangeWindowSize * 0.5);
            var proposedMaximum = falseCentre + (RangeWindowSize * 0.5);

            // Whoa this is dangerous, we need to handle the cases where the thumb is trying to go off the end of the slider.
            if(proposedMaximum >= OverallMaximum)
            {
                // off the top
                RangeMaximum = OverallMaximum;
                RangeMinimum = RangeMaximum - RangeWindowSize;
            }
            else if(proposedMinimum <= OverallMinimum)
            {
                // off the bottom
                RangeMinimum = OverallMinimum;
                RangeMaximum = RangeMinimum + RangeWindowSize;
            }
            else
            {
                // regular assignment
                RangeMinimum = proposedMinimum;
                RangeMaximum = proposedMaximum;
            }
        }

        private void ShiftRange(int offset)
        {
            RangeMinimum += offset;
            RangeMaximum += offset;
        }

        private void _upButton_Click(object sender, RoutedEventArgs e)
        {
            ShiftRange(10);
        }

        private void _downButton_Click(object sender, RoutedEventArgs e)
        {
            ShiftRange(-10);
        }

        private void _thumbGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isMouseDown = true;
            _thumbGrid.CaptureMouse();
        }

        private void _thumbGrid_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isMouseDown = false;
        }
        
        private void DockPanel_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                // move the max and min around
                var p = Mouse.GetPosition(this);
                var xPos = Math.Min(ActualWidth, Math.Max(0, p.X));

                var ratio = xPos / ActualWidth;
                var middle = OverallMinimum + ((OverallMaximum - OverallMinimum) * ratio); // lerp to the middle in engine units
                SetFromMiddle(middle);
            }
        }
    }
}
