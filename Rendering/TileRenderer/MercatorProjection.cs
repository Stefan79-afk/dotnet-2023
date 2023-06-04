namespace Mapster.Rendering;

public static class MercatorProjection
{
    private static readonly double RMajor = 6378137.0;
    private static readonly double RMinor = 6356752.3142;
    private static readonly double Ratio = RMinor / RMajor;
    private static readonly double Eccent = Math.Sqrt(1.0 - Ratio * Ratio);
    private static readonly double Com = 0.5 * Eccent;

    private static readonly double Deg2Rad = Math.PI / 180.0;
    private static readonly double Rad2Deg = 180.0 / Math.PI;
    private static readonly double Pi2 = Math.PI / 2.0;

    public static double LonToX(double lon)
    {
        return RMajor * DegToRad(lon);
    }

    public static double LatToY(double lat)
    {
        lat = Math.Min(89.5, Math.Max(lat, -89.5));
        var phi = DegToRad(lat);
        var sinphi = Math.Sin(phi);
        var con = Eccent * sinphi;
        con = Math.Pow((1.0 - con) / (1.0 + con), Com);
        var ts = Math.Tan(0.5 * (Math.PI * 0.5 - phi)) / con;
        return 0 - RMajor * Math.Log(ts);
    }

    private static double RadToDeg(double rad)
    {
        return rad * Rad2Deg;
    }

    private static double DegToRad(double deg)
    {
        return deg * Deg2Rad;
    }
}