using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataGenerator {
    public class DataItem {
        public float Argument { get; set; }
        public float Value { get; set; }
    }
    public class Generator {
        public static ObservableCollection<DataItem> Generate(long count) {
            ObservableCollection<DataItem> result = new ObservableCollection<DataItem>();
            for (long i = 0; i < count; i++)

                AddOneRecord(result, i);
            return result;
        }

        static void AddOneRecord(ObservableCollection<DataItem> result, long i) {
            result.Add(new DataItem()
            {
                Argument = i,
                Value = (float)(Math.Sin(i) +
                                                                                Math.Sin(i / 100.0) +
                                                                               30 * Math.Sin(i / 1000.0) +
                                                                               1000 * Math.Sin(i / 100000.0))
            });
        }

        public static void UpdateSource(ObservableCollection<DataItem> chartSource) {
            float lastArgument = chartSource.Last().Argument;
            chartSource.RemoveAt(0);
            AddOneRecord(chartSource, (int)lastArgument + 1);
        }
    }
}
