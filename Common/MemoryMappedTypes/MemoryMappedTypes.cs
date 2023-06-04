using System.Runtime.InteropServices;

namespace Mapster.Common.MemoryMappedTypes;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct FileHeader
{
    [FieldOffset(0)] private readonly long Version;
    [FieldOffset(8)] public readonly int TileCount;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct TileHeaderEntry
{
    [FieldOffset(0)] public readonly int ID;
    [FieldOffset(4)] public readonly ulong OffsetInBytes;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct TileBlockHeader
{
    /// <summary>
    ///     Number of render-able features in the tile.
    /// </summary>
    [FieldOffset(0)] public readonly int FeaturesCount;

    /// <summary>
    ///     Number of coordinates used for the features in the tile.
    /// </summary>
    [FieldOffset(4)] private readonly int CoordinatesCount;

    /// <summary>
    ///     Number of strings used for the features in the tile.
    /// </summary>
    [FieldOffset(8)] private readonly int StringCount;

    /// <summary>
    ///     Number of characters used by the strings in the tile.
    /// </summary>
    [FieldOffset(12)] private readonly int CharactersCount;

    [FieldOffset(16)] public readonly ulong CoordinatesOffsetInBytes;
    [FieldOffset(24)] public readonly ulong StringsOffsetInBytes;
    [FieldOffset(32)] public readonly ulong CharactersOffsetInBytes;
}

/// <summary>
///     References a string in a large character array.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct StringEntry
{
    [FieldOffset(0)] public readonly int Offset;
    [FieldOffset(4)] public readonly int Length;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly struct Coordinate
{
    [FieldOffset(0)] public readonly double Latitude;
    [FieldOffset(8)] public readonly double Longitude;

    public Coordinate()
    {
        Latitude = 0;
        Longitude = 0;
    }

    public Coordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    private bool Equals(Coordinate other)
    {
        return Math.Abs(Latitude - other.Latitude) < double.Epsilon &&
               Math.Abs(Longitude - other.Longitude) < double.Epsilon;
    }

    public override bool Equals(object? obj)
    {
        return obj is Coordinate other && Equals(other);
    }

    public static bool operator ==(Coordinate self, Coordinate other)
    {
        return self.Equals(other);
    }

    public static bool operator !=(Coordinate self, Coordinate other)
    {
        return !(self == other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Latitude, Longitude);
    }
}

public enum GeometryType : byte
{
    Polyline,
    Polygon,
    Point
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct PropertyEntryList
{
    [FieldOffset(0)] private readonly int Count;
    [FieldOffset(4)] private readonly ulong OffsetInBytes;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct MapFeature
{
    // https://wiki.openstreetmap.org/wiki/Key:highway
    private static readonly string[] HighwayTypes =
    {
        "motorway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "road"
    };

    [FieldOffset(0)] public readonly long Id;
    [FieldOffset(8)] public readonly int LabelOffset;
    [FieldOffset(12)] public readonly GeometryType GeometryType;
    [FieldOffset(13)] public readonly int CoordinateOffset;
    [FieldOffset(17)] public readonly int CoordinateCount;
    [FieldOffset(21)] public readonly int PropertiesOffset;
    [FieldOffset(25)] public readonly int PropertyCount;
}
