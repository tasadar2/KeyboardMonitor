namespace KeyboardMonitor.Stats.Support
{
    public interface ICounterStat
    {
        string Name { get; }
        float Value { get; }
        float Update();
    }
}