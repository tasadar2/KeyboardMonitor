using OpenHardwareMonitor.Hardware;

namespace KeyboardMonitor.Gathering
{
    public class HardwareService
    {
        public readonly Computer Computer;

        public HardwareService()
        {
            Computer = new Computer
            {
                CPUEnabled = true,
                GPUEnabled = true,
                RAMEnabled = true
            };
            Computer.Open();
        }

        public void Update()
        {
            foreach (var hardware in Computer.Hardware)
            {
                hardware.Update();
            }
        }
    }
}