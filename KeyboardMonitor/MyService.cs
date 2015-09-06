using System.Timers;
using KeyboardMonitor.Stats;
using Newtonsoft.Json;

namespace KeyboardMonitor
{
    public class KeyboardMonitorService
    {
        public Timer StatisticsTimer;
        public Info Info { get; set; }
        public SubscriptionCommunicator Communicator;

        public void Start()
        {
            Info = new Info
            {
                Processors = new CounterStatCollection("Processor", "% Processor Time", (counter, sum) => sum / counter.Count),
                BytesReceived = new CounterStatCollection("Network Interface", "Bytes Received/sec"),
                BytesSent = new CounterStatCollection("Network Interface", "Bytes Sent/sec")
            };

            StatisticsTimer = new Timer(1000);
            StatisticsTimer.Elapsed += timer_Elapsed;
            StatisticsTimer.Start();

            Communicator = new SubscriptionCommunicator(SubscriptionCommunicator.DiscoverPort);
        }


        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            StatisticsTimer.Stop();

            Info.Update();

            Communicator.SendToSubscribers(JsonConvert.SerializeObject(Info));

            StatisticsTimer.Start();
        }

        public void Stop()
        {
            Communicator.Close();
        }

    }

    public class Info
    {
        public CounterStatCollection Processors { get; set; }
        public CounterStatCollection BytesReceived { get; set; }
        public CounterStatCollection BytesSent { get; set; }

        public void Update()
        {
            Processors.Update();
            BytesReceived.Update();
            BytesSent.Update();
        }
    }
}