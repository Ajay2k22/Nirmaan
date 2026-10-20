namespace Nirmaan.Api.Utils;

public static class GeoUtils
{
    // Haversine - distance in meters
    public static double GetDistanceInMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // earth radius in meters
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;

    // Simple Geohash for indexing - precision 7 = ~150m x 150m cell
    public static string ToGeoHash(double lat, double lon, int precision = 7)
    {
        // simple grid hash - enough for dedup, no library needed
        // 4 decimal places = ~11m accuracy
        return $"{Math.Round(lat, 4)}_{Math.Round(lon, 4)}";
        // For production use library: Geohash.Encode(lat, lon, precision)
    }
}