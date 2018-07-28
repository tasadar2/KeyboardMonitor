using System.Linq;
using KeyboardMonitor.Stats.Support;
using OpenHardwareMonitor.Hardware;

namespace KeyboardMonitor.Stats
{
    public class HardwareStat : ICounterStat
    {
        private readonly ISensor _sensor;

        public HardwareStat(IComputer computer, HardwareType hardwareType, Identifier identifier)
        {
            _sensor = computer.Hardware
                              .Where(h => h.HardwareType == hardwareType)
                              .SelectMany(h => h.Sensors)
                              .FirstOrDefault(s => s.Identifier == identifier);

            Name = _sensor?.Name;
        }

        public HardwareStat(IComputer computer, HardwareType hardwareType, SensorType sensorType)
        {
            _sensor = computer.Hardware
                              .Where(h => h.HardwareType == hardwareType)
                              .SelectMany(h => h.Sensors)
                              .FirstOrDefault(s => s.SensorType == sensorType);

            Name = _sensor?.Name;
        }

        public HardwareStat(IComputer computer, HardwareType hardwareType, SensorType sensorType, string name)
        {
            _sensor = computer.Hardware
                              .Where(h => h.HardwareType == hardwareType)
                              .SelectMany(h => h.Sensors)
                              .FirstOrDefault(s => s.SensorType == sensorType && s.Name == name);

            Name = _sensor?.Name;
        }

        public string Name { get; }
        public float Value { get; private set; }

        public float Update()
        {
            return Value = _sensor?.Value ?? 0;
        }
    }
}