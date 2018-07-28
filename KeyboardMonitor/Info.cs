using KeyboardMonitor.Stats;

namespace KeyboardMonitor
{
    public class Info
    {
        public PerformanceCounterStatCollection Processors { get; set; }
        public PerformanceCounterStatCollection BytesReceived { get; set; }
        public PerformanceCounterStatCollection BytesSent { get; set; }

        public void Update()
        {
            Processors.Update();
            BytesReceived.Update();
            BytesSent.Update();
        }
    }
}