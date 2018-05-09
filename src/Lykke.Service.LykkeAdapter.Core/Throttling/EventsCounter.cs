using System;

namespace Lykke.Service.LykkeAdapter.Core.Throttling
{
    internal class EventsCounter
    {
        internal int NumberOfEventsInLastTimeFrame { get; set; }
        internal DateTime LastEventTimeStamp { get; set; }
    }
}
