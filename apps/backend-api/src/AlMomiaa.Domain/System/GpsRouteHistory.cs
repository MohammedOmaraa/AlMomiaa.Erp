using AlMomiaa.Domain.Identity;

namespace AlMomiaa.Domain.SystemOps;

public sealed class GpsRouteHistory
{
    private GpsRouteHistory() { }

    public long Id { get; private set; }
    public Guid RepId { get; private set; }
    public User Rep { get; private init; } = null!;
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public decimal? AccuracyMeters { get; private set; }
    public Guid? NearCustomerId { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
}
