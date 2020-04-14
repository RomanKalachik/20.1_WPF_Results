using DataGenerator;
using Infragistics.Controls.Charts;
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
        long prevAvailable = 0;
        public MainWindow()
        {
            InitializeComponent();
        }
        void bindDataWpf()
        {
            SplineSeries series = new SplineSeries();
            series.XAxis = xmXAxis;
            series.YAxis = xmYAxis;
            DataChart.Series.Add(series);
            series.ValueMemberPath = "Value";
            series.ItemsSource = chartSource;
            DataChart.IsHorizontalZoomEnabled = true;
            DataChart.IsVerticalZoomEnabled = true;

            DataChart.DataContext = chartSource;
            this.DataChart.WindowScaleHorizontal = 1;
            this.DataChart.WindowScaleVertical = 1;
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
                Stopwatch nonResampledLoading = new Stopwatch();

                nonResampledLoading.Start();
                LoadDataEx(count);
                nonResampledLoading.Stop();
                result += string.Format("{0}, {1}, {2}{3}", count, 0, nonResampledLoading.ElapsedMilliseconds, Environment.NewLine);
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
            string result = string.Empty;
            Iterate(new Action<int>(count =>
            {
                LoadData(count);
                Stopwatch nonResampledLoading = new Stopwatch();
                this.DataChart.WindowScaleHorizontal = 0.5;

                nonResampledLoading.Start();

                for (int i = 0; i < 25; i++)
                {
                    this.DataChart.WindowPositionHorizontal += 10;
                    DoEvents();

                }
                nonResampledLoading.Stop();
                result += string.Format("{0}, {1}, {2}{3}", count, 0, nonResampledLoading.ElapsedMilliseconds, Environment.NewLine);
            }));
            File.WriteAllText("result_scroll.txt", result);

        }
        void MeasureZoom(object sender, RoutedEventArgs e)
        {
            string result = string.Empty;
            Iterate(new Action<int>(count =>
            {
                LoadData(count);
                Stopwatch nonResampledLoading = new Stopwatch();
                nonResampledLoading.Start();

                for (int i = 0; i < 5; i++)
                {
                     this.DataChart.WindowScaleHorizontal -= 0.05;
                    this.DataChart.WindowScaleVertical -= 0.05;
                    DoEvents();

                }
                nonResampledLoading.Stop();
                result += string.Format("{0}, {1}, {2}{3}", count, 0, nonResampledLoading.ElapsedMilliseconds, Environment.NewLine);
            }));
            File.WriteAllText("result_zoom.txt", result);
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
