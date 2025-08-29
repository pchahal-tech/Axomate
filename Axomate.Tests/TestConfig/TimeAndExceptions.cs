using System;

namespace Axomate.Tests.TestConfig
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
        DateTime NowLocal { get; }
    }

    public sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime NowLocal => DateTime.Now;
    }

    public sealed class FixedTimeProvider : ITimeProvider
    {
        private DateTime _utc;
        private readonly TimeZoneInfo _tz;

        public FixedTimeProvider(DateTime utc, TimeZoneInfo? tz = null)
        {
            _utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            _tz = tz ?? TimeZoneInfo.Local;
        }

        public DateTime UtcNow => _utc;
        public DateTime NowLocal => TimeZoneInfo.ConvertTimeFromUtc(_utc, _tz);

        public void Advance(TimeSpan delta)
        {
            _utc = _utc.Add(delta);
        }
    }

    public class DuplicateResourceException : Exception
    {
        public DuplicateResourceException(string message) : base(message) {}
    }
}
