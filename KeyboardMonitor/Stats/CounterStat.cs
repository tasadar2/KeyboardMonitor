using System.Diagnostics;
using Newtonsoft.Json;

namespace KeyboardMonitor.Stats
{
    public class CounterStat
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

        public CounterStat(string category, string name)
        {
            _counter = new PerformanceCounter(category, name);
        }

        public CounterStat(string category, string name, string instance)
        {
            _counter = new PerformanceCounter(category, name, instance);
        }

        public float Update()
        {
            return Value = _counter.NextValue();
        }
    }
}