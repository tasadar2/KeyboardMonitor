using System.Collections.Generic;

namespace KeyboardMonitor.Stats.Support
{
    public interface ICounterStatCollection : IEnumerable<ICounterStat>
    {
        string Name { get; }
        float Value { get; }
        float Update();
    }
}