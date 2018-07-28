using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using KeyboardMonitor.Serialization;
using KeyboardMonitor.Stats.Support;
using Newtonsoft.Json;

namespace KeyboardMonitor.Stats
{
    [JsonConverter(typeof(CounterStatCollectionSerializer))]
    public class PerformanceCounterStatCollection : List<ICounterStat>, ICounterStatCollection
    {
        public string Name => this.First().Name;

        public float Value { get; set; }
        public Func<PerformanceCounterStatCollection, float, float> ValueCalculation { get; set; }

        public PerformanceCounterStatCollection(string category, string counterName, Func<PerformanceCounterStatCollection, float, float> valueCalculation = null)
        {
            var cpuCategory = new PerformanceCounterCategory(category);
            var instanceNames = cpuCategory.GetInstanceNames()
                                           .Where(name => name != "_Total")
                                           .OrderBy(name => name);

            foreach (var instanceName in instanceNames)
            {
                Add(new PerformanceCounterStat(category, counterName, instanceName));
            }

            ValueCalculation = valueCalculation ?? ((counter, sum) => sum);
        }

        public float Update()
        {
            return Value = ValueCalculation(this, this.Sum(counterStat => counterStat.Update()));
        }
    }

    public class HardwareStat : ICounterStat
    {
        public string Name { get; }
        public float Value { get; }

        public float Update()
        {
            throw new NotImplementedException();
        }
    }

    public class HardwareService
    {
        public HardwareService()
        {
            
        }
    }
}