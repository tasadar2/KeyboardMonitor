using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KeyboardMonitor
{
    public class KeyboardMonitorService
    {
        public Timer StatisticsTimer;
        public Info Info { get; set; }
        public Communicator Communicator;

        public void Start()
        {
            Info = new Info();

            Info.Processors = new CounterStatCollection("Processor", "% Processor Time");
            Info.BytesReceived = new CounterStatCollection("Network Interface", "Bytes Received/sec");
            Info.BytesSent = new CounterStatCollection("Network Interface", "Bytes Sent/sec");

            StatisticsTimer = new Timer(1000);
            StatisticsTimer.Elapsed += timer_Elapsed;
            StatisticsTimer.Start();

            Communicator = new Communicator(Communicator.DiscoverPort);
        }


        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            StatisticsTimer.Stop();

            Info.Update();

            foreach (var processor in Info.Processors)
            {
                Console.Write(processor.Value + " ");
            }
            Console.WriteLine();

            Console.WriteLine(Info.BytesReceived.Value);
            Console.WriteLine(Info.BytesSent.Value);

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

    [JsonConverter(typeof(CounterStatCollectionSerializer))]
    public class CounterStatCollection : List<CounterStat>
    {
        public string Name
        {
            get
            {
                return this.First().Name;
            }
        }

        public float Value { get; set; }

        public CounterStatCollection(string category, string counterName)
        {
            var cpuCategory = new PerformanceCounterCategory(category);
            var instanceNames = cpuCategory.GetInstanceNames()
                .Where(name => name != "_Total")
                .OrderBy(name => name);

            foreach (var instanceName in instanceNames)
            {
                Add(new CounterStat(category, counterName, instanceName));
            }
        }

        public float Update()
        {
            return Value = this.Sum(counterStat => counterStat.Update());
        }
    }

    public class CounterStatCollectionSerializer : JsonConverter
    {
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var cc = value as CounterStatCollection;
            writer.WriteStartObject();

            writer.WritePropertyName("Name");
            serializer.Serialize(writer, cc.Name);

            writer.WritePropertyName("Value");
            serializer.Serialize(writer, cc.Value);

            writer.WritePropertyName("Values");
            writer.WriteStartArray();
            foreach (var v in cc.Select(t => t.Value))
            {
                serializer.Serialize(writer, v);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param><param name="objectType">Type of the object.</param><param name="existingValue">The existing value of object being read.</param><param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }

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