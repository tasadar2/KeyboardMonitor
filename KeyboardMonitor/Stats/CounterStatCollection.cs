using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using KeyboardMonitor.Serialization;
using Newtonsoft.Json;

namespace KeyboardMonitor.Stats
{
    [JsonConverter(typeof(CounterStatCollectionSerializer))]
    public class CounterStatCollection : List<CounterStat>
    {
        public string Name => this.First().Name;

        public float Value { get; set; }
        public Func<CounterStatCollection, float, float> ValueCalculation { get; set; }

        public CounterStatCollection(string category, string counterName, Func<CounterStatCollection, float, float> valueCalculation = null)
        {
            var cpuCategory = new PerformanceCounterCategory(category);
            var instanceNames = cpuCategory.GetInstanceNames()
                                           .Where(name => name != "_Total")
                                           .OrderBy(name => name);

            foreach (var instanceName in instanceNames)
            {
                Add(new CounterStat(category, counterName, instanceName));
            }

            ValueCalculation = valueCalculation ?? ((counter, sum) => sum);
        }

        public float Update()
        {
            return Value = ValueCalculation(this, this.Sum(counterStat => counterStat.Update()));
        }
    }
}