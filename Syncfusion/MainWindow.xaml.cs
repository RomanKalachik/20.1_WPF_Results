﻿using DataGenerator;
using Syncfusion.UI.Xaml.Charts;
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
            Loaded += (s, e) =>
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    string[] args = Environment.GetCommandLineArgs();
                    if (args.Length > 1) MeasureAll(null, null);
                }));
            };
        }
        void bindDataWpf()
        {
            Chart.Series.Clear();
            var line = new FastLineSeries();
            line.ItemsSource = chartSource;
            line.XBindingPath = "Argument";
            line.YBindingPath = "Value";
            Chart.Series.Add(line);
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
            Application.Current.Shutdown();
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
            string result = string.Empty;
            Iterate(new Action<int>(count =>
            {
                Stopwatch resampled = new Stopwatch();
                LoadDataEx(count);
                Chart.PrimaryAxis.ZoomFactor = 0.5;
                resampled.Start();
                for (int i = 0; i < 25; i++)
                {
                    Chart.PrimaryAxis.ZoomPosition = i * 100;
                    DoEvents();
                }
                resampled.Stop();
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
                resampled.Start();
                for (int i = 0; i < 5; i++)
                {
                    Chart.PrimaryAxis.ZoomFactor = i / 5.0;
                    DoEvents();
                }
                resampled.Stop();
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
