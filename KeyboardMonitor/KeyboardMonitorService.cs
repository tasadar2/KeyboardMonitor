using System.Threading;
using KeyboardMonitor.Gathering.FrameRate;
using KeyboardMonitor.Stats;
using Newtonsoft.Json;

namespace KeyboardMonitor
{
    public class KeyboardMonitorService
    {
        public Timer StatisticsTimer;
        public Timer RealtimeTimer;
        public Info Info { get; set; }
        public SubscriptionCommunicator Communicator;
        public FrapsService FrapsService;

        public void Start()
        {
            Info = new Info
            {
                Processors = new PerformanceCounterStatCollection("Processor", "% Processor Time", (counter, sum) => sum / counter.Count),
                BytesReceived = new PerformanceCounterStatCollection("Network Interface", "Bytes Received/sec"),
                BytesSent = new PerformanceCounterStatCollection("Network Interface", "Bytes Sent/sec")
            };

            FrapsService = new FrapsService();
            Communicator = new SubscriptionCommunicator(SubscriptionCommunicator.DiscoverPort);

            StatisticsTimer = new Timer(Statistics_Elapsed, null, 0, 1000);
            RealtimeTimer = new Timer(Realtime_Elapsed, null, 0, 150);
        }

        private void Statistics_Elapsed(object state)
        {
            Info.Update();

            Communicator.SendToSubscribers(JsonConvert.SerializeObject(Info));
        }

        private FrapsData _lastFrapsData;

        private void Realtime_Elapsed(object state)
        {
            var frapsData = FrapsService.GetFrapsData();

            if (frapsData.FramesPerSecond != _lastFrapsData.FramesPerSecond)
            {
                _lastFrapsData = frapsData;
                Communicator.SendFrapsToSubscribers(frapsData);
            }
        }

        public void Stop()
        {
            StatisticsTimer.Change(Timeout.Infinite, Timeout.Infinite);
            RealtimeTimer.Change(Timeout.Infinite, Timeout.Infinite);
            StatisticsTimer.Dispose();
            RealtimeTimer.Dispose();
            Communicator.Close();
        }
    }
}