namespace Lykke.Service.LykkeAdapter.Core.Throttling
{
    public interface IThrottling
    {
        bool NeedThrottle(string instrument);
    }
}
