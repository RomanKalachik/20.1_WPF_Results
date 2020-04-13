using DataGenerator;
using DevExpress.Xpf.Charts;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Windows;
using System.Windows.Threading;
namespace WPFChart {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        ObservableCollection<DataItem> chartSource;
        string log;
        long prevAvailable = 0;
        public MainWindow()
        {
            InitializeComponent();
        }
        void bindDataWpf(bool allowResample)
        {
            XYDiagram2D diagram = new XYDiagram2D();
            Chart.Diagram = diagram;
            diagram.EnableAxisXNavigation = true;
            diagram.EnableAxisYNavigation = true;
            diagram.NavigationOptions = new NavigationOptions() { AxisXMaxZoomPercent = 100000000, AxisYMaxZoomPercent = 100000000 };
            LineSeries2D series = new LineSeries2D();
            series.AllowResample = allowResample;
            Chart.CrosshairEnabled = true;
            diagram.Series.Add(series);

            series.DataSource = chartSource;
            series.ArgumentDataMember = "Argument";
            series.ValueDataMember = "Value";
        }
        static String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
        void ClearChart()
        {
            DoEvents();

            Diagram diagram = Chart.Diagram;
            Chart.Diagram = null;
            Series series = diagram?.Series?.First();
            diagram?.Series?.Clear();
            if (series != null) series.DataSource = null;
        }
        void DoEvents()
        {
            Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        }
        void LoadData(int pointsCount, bool allowResample)
        {
            ClearChart();
            chartSource = Generator.Generate(pointsCount);
            LogMemConsumption();
            bindDataWpf(allowResample);
            DoEvents();
        }
        void LoadDataEx(int pointsCount, bool allowResample)
        {
            chartSource = Generator.Generate(pointsCount);
            bindDataWpf(allowResample);
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
        }
        void MeasureLoading(object sender, RoutedEventArgs e)
        {
            string result = string.Empty;
            Iterate(new Action<int>(count =>
            {
                Stopwatch resampledLoading = new Stopwatch();
                Stopwatch nonResampledLoading = new Stopwatch();
                resampledLoading.Start();
                LoadDataEx(count, true);
                resampledLoading.Stop();
                nonResampledLoading.Start();
                LoadDataEx(count, false);
                nonResampledLoading.Stop();
                result += string.Format("{0}, {1}, {2}{3}", count, resampledLoading.ElapsedMilliseconds, nonResampledLoading.ElapsedMilliseconds, Environment.NewLine);
            }));
            File.WriteAllText("result_loading.txt", result);


        }
        void MeasureMemConsumption(object sender, RoutedEventArgs e)
        {
            MeasureMemConsumptionCore(true);
            MeasureMemConsumptionCore(false);
        }
        void MeasureMemConsumptionCore(bool allowResampling)
        {
            string result = string.Empty;
            Iterate(new Action<int>(count =>
            {
                LoadData(count, allowResampling);
                long chartSize = LogMemConsumption();
                result += string.Format("{0}, {1}{2}", count, chartSize, Environment.NewLine);
            }));
            File.WriteAllText(string.Format("result_scroll_{0}.txt", allowResampling), result);
        }
        void MeasurePan(object sender, RoutedEventArgs e)
        {
            string result = string.Empty;
            Iterate(new Action<int>(count =>
            {
                Stopwatch resampled = new Stopwatch();
                Stopwatch nonResampled = new Stopwatch();
                LoadDataEx(count, true);
                XYDiagram2D xyd2d = Chart.Diagram as XYDiagram2D;
                xyd2d.ZoomIn(null);
                resampled.Start();
                PerofrmScroll(xyd2d);
                resampled.Stop();
                LoadDataEx(count, false);
                xyd2d = Chart.Diagram as XYDiagram2D;
                nonResampled.Start();
                PerofrmScroll(xyd2d);
                nonResampled.Stop();
                result += string.Format("{0}, {1}, {2}{3}", count, resampled.ElapsedMilliseconds, nonResampled.ElapsedMilliseconds, Environment.NewLine);
            }));
            File.WriteAllText("result_scroll.txt", result);

        }
        void MeasureZoom(object sender, RoutedEventArgs e)
        {
            string result = string.Empty;
            Iterate(new Action<int>(count =>
            {
                Stopwatch resampled = new Stopwatch();
                Stopwatch nonResampled = new Stopwatch();
                LoadDataEx(count, true);
                XYDiagram2D xyd2d = Chart.Diagram as XYDiagram2D;
                resampled.Start();
                PerformZoom(xyd2d);
                resampled.Stop();
                LoadDataEx(count, false);
                xyd2d = Chart.Diagram as XYDiagram2D;
                nonResampled.Start();
                PerformZoom(xyd2d);
                nonResampled.Stop();
                result += string.Format("{0}, {1}, {2}{3}", count, resampled.ElapsedMilliseconds, nonResampled.ElapsedMilliseconds, Environment.NewLine);
            }));
            File.WriteAllText("result_zoom.txt", result);
        }
        void PerformZoom(XYDiagram2D xyd2d)
        {
            for (int i = 0; i < 5; i++)
            {
                xyd2d.ZoomIn(null);
                DoEvents();
            }
        }
        void PerofrmScroll(XYDiagram2D xyd2d)
        {
            for (int i = 0; i < 5; i++)
            {
                xyd2d.ScrollHorizontally(10);
                DoEvents();
            }
        }
        protected void Iterate(Action<int> action)
        {
            int[] counts = new int[] { 1000, 10000, 20000, 50000, 100000, 300000, 500000, 750000, 1000000, 2000000 };
            foreach (int count in counts)
            {
                action(count);
            }
        }
        public ChartControl Chart { get { return chart; } }
    }
}
