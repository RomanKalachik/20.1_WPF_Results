using DataGenerator;
using Steema.TeeChart.WPF.Drawing;
using Steema.TeeChart.WPF.Styles;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
namespace WPFChart {
    public partial class MainWindow : Window {
        ObservableCollection<DataItem> chartSource;
        long prevAvailable = 0;
        public MainWindow()
        {
            InitializeComponent();
        }
        void bindDataWpf()
        {
            Chart.Series.Clear();
            Chart.Visibility = Visibility.Visible;
            Chart.Aspect.View3D = false;
            Chart.Header.Font.Size = 16;
            Chart.Header.Text = "FastLine Example";
            Chart.Axes.Left.AutomaticMaximum = true;
            Chart.Axes.Left.AutomaticMinimum = true;

            Chart.Axes.Left.Increment = 1000;
            Chart.Panel.Gradient.Visible = false;
            Chart.Panel.Brush.Transparency = 100;
            Chart.Panel.Bevel.Inner = BevelStyles.None;
            Chart.Panel.Bevel.Outer = BevelStyles.None;

            FastLine line = new FastLine(Chart.Chart)
            {
                Title = "Example FastLine 1",
                Color = Color.FromRgb(0xfa, 0x68, 0x00),
            };
            line.Marks.Arrow.Visible = false;
            line.Marks.Brush.Visible = false;
            line.Marks.Pen.Visible = false;
            Chart.Chart.ToolTip.InitialDelay = 500;
            Chart.Chart.ToolTip.BackColor = Color.FromRgb(0x25, 0x25, 0x25);
            Chart.Chart.ToolTip.ForeColor = Color.FromRgb(0xFF, 0xFF, 0xFF);
            line.XValues.DataMember = "Argument";
            line.YValues.DataMember = "Value";

            line.DataSource = chartSource;

        }
        void ClearChart()
        {
            DoEvents();
        }
        void DoEvents()
        {
            Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        }
        void LoadData(int pointsCount)
        {
            ClearChart();
            chartSource = Generator.Generate(pointsCount);
            LogMemConsumption();
            bindDataWpf();
            DoEvents();
        }
        void LoadDataEx(int pointsCount)
        {
            chartSource = Generator.Generate(pointsCount);
            bindDataWpf();
            DoEvents();
        }
        long LogMemConsumption()
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForFullGCComplete();
            long available = GC.GetTotalMemory(true);

            long delta = prevAvailable - available;
            prevAvailable = available;
            return delta;
        }
        void MeasureAll(object sender, RoutedEventArgs e)
        {
            MeasureLoading(null, null);
            MeasureMemConsumption(null, null);
            MeasureZoom(null, null);
            MeasurePan(null, null);
            MessageBox.Show("Done");
        }
        void MeasureLoading(object sender, RoutedEventArgs e)
        {
            string result = string.Empty;
            Iterate(new Action<int>(count =>
            {
                Stopwatch resampledLoading = new Stopwatch();
                resampledLoading.Start();
                LoadDataEx(count);
                resampledLoading.Stop();
                result += string.Format("{0}, {1}, {2}{3}", count, resampledLoading.ElapsedMilliseconds, 0, Environment.NewLine);
            }));
            File.WriteAllText("result_loading.txt", result);


        }
        void MeasureMemConsumption(object sender, RoutedEventArgs e)
        {
            MeasureMemConsumptionCore();
        }
        void MeasureMemConsumptionCore()
        {
            string result = string.Empty;
            Iterate(new Action<int>(count =>
            {
                LoadData(count);
                long chartSize = LogMemConsumption();
                result += string.Format("{0}, {1}{2}", count, -chartSize, Environment.NewLine);
            }));
            File.WriteAllText(string.Format("result_memconsumptoin_{0}.txt", false), result);
        }
        void MeasurePan(object sender, RoutedEventArgs e)
        {
            //Chart.Zoom.ZoomPercent(100);

            string result = string.Empty;
            Iterate(new Action<int>(count =>
            {
                Stopwatch resampled = new Stopwatch();
                LoadDataEx(count);
                Chart.Zoom.ZoomPercent(100);
                resampled.Start();
                for (int i = 0; i < 13; i++)
                {
                    Chart.Axes.Bottom.Scroll(i * 100, false);
                    DoEvents();
                    Chart.Axes.Bottom.Scroll(-i * 100, false);
                    DoEvents();
                }
                resampled.Stop();
                Chart.Zoom.Undo();
                result += string.Format("{0}, {1}, {2}{3}", count, resampled.ElapsedMilliseconds, 0, Environment.NewLine);
            }));
        
            File.WriteAllText("result_scroll.txt", result);
        }
        void MeasureZoom(object sender, RoutedEventArgs e)
        {
            string result = string.Empty;
            Iterate(new Action<int>(count =>
            {
                Stopwatch resampled = new Stopwatch();
                LoadDataEx(count);
                Chart.Zoom.ZoomPercent(100);
                resampled.Start();
                for (int i = 0; i < 3; i++)
                {
                    Chart.Zoom.ZoomPercent(100 + i*10);
                    DoEvents();
                    Chart.Zoom.Undo();
                    DoEvents();
                }
              
                resampled.Stop();
                Chart.Zoom.Undo();
                result += string.Format("{0}, {1}, {2}{3}", count, resampled.ElapsedMilliseconds, 0, Environment.NewLine);
            }));
            File.WriteAllText("result_scroll.txt", result); File.WriteAllText("result_zoom.txt", result);
        }
        protected void Iterate(Action<int> action)
        {
            int[] counts = new int[] { 1000, 10000, 20000, 50000, 100000, 300000, 500000, 750000, 1000000, 2000000 };
            foreach (int count in counts)
            {
                action(count);
            }
        }
    }
}
