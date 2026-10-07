namespace FairShare.Application.Mappings
{
    /// <summary>Small helpers used inside mapping expressions.</summary>
    public static class MappingExtensions
    {
        /// <summary>
        /// Normalizes a DateTime to UTC. PostgreSQL "timestamp with time zone" columns reject
        /// DateTime values with Kind = Unspecified, which is what JSON dates without "Z" deserialize to.
        /// </summary>
        public static DateTime AsUtc(this DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
