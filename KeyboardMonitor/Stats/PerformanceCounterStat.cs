using System.Diagnostics;
using KeyboardMonitor.Stats.Support;
using Newtonsoft.Json;

namespace KeyboardMonitor.Stats
{
    public class PerformanceCounterStat : ICounterStat
    {
        [JsonIgnore]
        public string Name
        {
            get
            {
                return _counter.CounterName;
            }
        }

        [JsonIgnore]
        public string InstanceName
        {
            get
            {
                return _counter.InstanceName;
            }
        }

        public float Value { get; set; }
        private readonly PerformanceCounter _counter;

        public PerformanceCounterStat(string category, string name)
        {
            _counter = new PerformanceCounter(category, name);
        }

        public PerformanceCounterStat(string category, string name, string instance)
        {
            _counter = new PerformanceCounter(category, name, instance);
        }

        public float Update()
        {
            return Value = _counter.NextValue();
        }
    }
}