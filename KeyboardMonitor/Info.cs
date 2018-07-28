using KeyboardMonitor.Stats.Support;

namespace KeyboardMonitor
{
    public class Info
    {
        public ICounterStatCollection Processors { get; set; }
        public ICounterStatCollection BytesReceived { get; set; }
        public ICounterStatCollection BytesSent { get; set; }

        public ICounterStat ProcessorTemperature { get; set; }
        public ICounterStat ProcessorClock { get; set; }

        public ICounterStat GpuClock { get; set; }
        public ICounterStat GpuTemperature { get; set; }
        
        public void Update()
        {
            Processors.Update();
            BytesReceived.Update();
            BytesSent.Update();
        }
    }
}