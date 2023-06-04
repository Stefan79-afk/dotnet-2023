using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mapster.Common.MemoryMappedTypes;

/// <summary>
///     Action to be called when iterating over <see cref="MapFeature" /> in a given bounding box via a call to
///     <see cref="DataFile.ForeachFeature" />
/// </summary>
/// <param name="feature">The current <see cref="MapFeature" />.</param>
/// <param name="label">The label of the feature, <see cref="string.Empty" /> if not available.</param>
/// <param name="coordinates">The coordinates of the <see cref="MapFeature" />.</param>
/// <returns></returns>
public delegate bool MapFeatureDelegate(MapFeatureData featureData);

/// <summary>
///     Aggregation of all the data needed to render a map feature
/// </summary>
public readonly ref struct MapFeatureData
{
    public long Id { get; init; }

    public GeometryType Type { get; init; }
    public ReadOnlySpan<char> Label { get; init; }
    public ReadOnlySpan<Coordinate> Coordinates { get; init; }
    public Dictionary<string, string> Properties { get; init; }
    public MapProperty MapProperty { get; init; }
}

/// <summary>
///     Represents a file with map data organized in the following format:<br />
///     <see cref="FileHeader" /><br />
///     Array of <see cref="TileHeaderEntry" /> with <see cref="FileHeader.TileCount" /> records<br />
///     Array of tiles, each tile organized:<br />
///     <see cref="TileBlockHeader" /><br />
///     Array of <see cref="MapFeature" /> with <see cref="TileBlockHeader.FeaturesCount" /> at offset
///     <see cref="TileHeaderEntry.OffsetInBytes" /> + size of <see cref="TileBlockHeader" /> in bytes.<br />
///     Array of <see cref="Coordinate" /> with <see cref="TileBlockHeader.CoordinatesCount" /> at offset
///     <see cref="TileBlockHeader.CharactersOffsetInBytes" />.<br />
///     Array of <see cref="StringEntry" /> with <see cref="TileBlockHeader.StringCount" /> at offset
///     <see cref="TileBlockHeader.StringsOffsetInBytes" />.<br />
///     Array of <see cref="char" /> with <see cref="TileBlockHeader.CharactersCount" /> at offset
///     <see cref="TileBlockHeader.CharactersOffsetInBytes" />.<br />
/// </summary>
public unsafe class DataFile : IDisposable
{
    private readonly FileHeader* _fileHeader;
    private readonly MemoryMappedViewAccessor _mma;
    private readonly MemoryMappedFile _mmf;

    private readonly byte* _ptr;
    private readonly int _coordinateSizeInBytes = Marshal.SizeOf<Coordinate>();
    private readonly int _fileHeaderSizeInBytes = Marshal.SizeOf<FileHeader>();
    private readonly int _mapFeatureSizeInBytes = Marshal.SizeOf<MapFeature>();
    private readonly int _stringEntrySizeInBytes = Marshal.SizeOf<StringEntry>();
    private readonly int _tileBlockHeaderSizeInBytes = Marshal.SizeOf<TileBlockHeader>();
    private readonly int _tileHeaderEntrySizeInBytes = Marshal.SizeOf<TileHeaderEntry>();

    private bool _disposedValue;

    public DataFile(string path)
    {
        _mmf = MemoryMappedFile.CreateFromFile(path);
        _mma = _mmf.CreateViewAccessor();
        _mma.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
        _fileHeader = (FileHeader*)_ptr;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _mma?.SafeMemoryMappedViewHandle.ReleasePointer();
                _mma?.Dispose();
                _mmf?.Dispose();
            }

            _disposedValue = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private TileHeaderEntry* GetNthTileHeader(int i)
    {
        return (TileHeaderEntry*)(_ptr + i * _tileHeaderEntrySizeInBytes + _fileHeaderSizeInBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private (TileBlockHeader? Tile, ulong TileOffset) GetTile(int tileId)
    {
        for (var i = 0; i < _fileHeader->TileCount; ++i)
        {
            var tileHeaderEntry = GetNthTileHeader(i);
            if (tileHeaderEntry->ID == tileId)
            {
                var tileOffset = tileHeaderEntry->OffsetInBytes;
                return (*(TileBlockHeader*)(_ptr + tileOffset), tileOffset);
            }
        }

        return (null, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private MapFeature* GetFeature(int i, ulong offset)
    {
        return (MapFeature*)(_ptr + offset + _tileBlockHeaderSizeInBytes + i * _mapFeatureSizeInBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ReadOnlySpan<Coordinate> GetCoordinates(ulong coordinateOffset, int ithCoordinate, int coordinateCount)
    {
        return new ReadOnlySpan<Coordinate>(_ptr + coordinateOffset + ithCoordinate * _coordinateSizeInBytes,
            coordinateCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void GetString(ulong stringsOffset, ulong charsOffset, int i, out ReadOnlySpan<char> value)
    {
        var stringEntry = (StringEntry*)(_ptr + stringsOffset + i * _stringEntrySizeInBytes);
        value = new ReadOnlySpan<char>(_ptr + charsOffset + stringEntry->Offset * 2, stringEntry->Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void GetProperty(ulong stringsOffset, ulong charsOffset, int i, out ReadOnlySpan<char> key,
        out ReadOnlySpan<char> value)
    {
        if (i % 2 != 0)
            throw new ArgumentException(
                "Properties are key-value pairs and start at even indices in the string list (i.e. i % 2 == 0)");

        GetString(stringsOffset, charsOffset, i, out key);
        GetString(stringsOffset, charsOffset, i + 1, out value);
    }


    private static MapProperty ClassifyProperties(IDictionary<string, string> properties, GeometryType geometryType)
    {
        if (properties.TryGetValue("highway", out var highwayValue))
            return highwayValue switch
            {
                "motorway" => MapProperty.HighwayMotorway,
                "trunk" => MapProperty.HighwayTrunk,
                "primary" => MapProperty.HighwayPrimary,
                "secondary" => MapProperty.HighwaySecondary,
                "tertiary" => MapProperty.HighwayTertiary,
                "residential" => MapProperty.HighwayResidential,
                "unclassified" or "road" => MapProperty.Highway,
                _ => MapProperty.Unknown
            };
        if (properties.Keys.Any(key => key.StartsWith("water")) && geometryType != GeometryType.Point)
            return MapProperty.Waterway;
        if (properties.TryGetValue("boundary", out var boundaryValue) && boundaryValue.StartsWith("administrative") &&
            properties.TryGetValue("admin_level", out var adminLevelValue) && adminLevelValue == "2")
            return MapProperty.Border;
        if (geometryType != GeometryType.Point && properties.TryGetValue("place", out var placeValue) &&
            new[] { "city", "town", "locality", "hamlet" }.Contains(placeValue))
            return MapProperty.PlaceName;
        if (properties.ContainsKey("railway"))
            return MapProperty.Railway;
        if (geometryType == GeometryType.Polygon && properties.TryGetValue("natural", out var naturalValue))
            return naturalValue switch
            {
                "fell" or "grassland" or "heath" or "moor" or "scrub" or "wetland" => MapProperty.LandusePlain,
                "wood" or "tree_row" => MapProperty.LanduseForest,
                "bare_rock" or "rock" or "scree" => MapProperty.LanduseNaturalMountains,
                "sand" or "beach" => MapProperty.LanduseNaturalDesert,
                "water" => MapProperty.LanduseNaturalWater,
                _ => MapProperty.LanduseNatural
            };
        if (properties.TryGetValue("boundary", out boundaryValue) && boundaryValue.StartsWith("forest"))
            return MapProperty.LanduseForest;
        if (properties.TryGetValue("landuse", out var landuseValue) &&
            (landuseValue.StartsWith("forest") || landuseValue.StartsWith("orchard")))
            return MapProperty.LanduseForest;
        if (geometryType == GeometryType.Polygon && properties.TryGetValue("landuse", out landuseValue))
            return landuseValue switch
            {
                "residential" or "cemetery" or "industrial" or "commercial" or "square" or
                    "construction" or "military" or "quarry" or "brownfield"
                    => MapProperty.LanduseResidential,
                "farm" or "meadow" or "grass" or "greenfield" or
                    "recreation_ground" or "winter_sports" or
                    "allotments"
                    => MapProperty.LandusePlain,
                "reservoir" or "basin"
                    => MapProperty.LanduseNaturalWater,
                _ => MapProperty.Unknown
            };
        if (geometryType == GeometryType.Polygon && properties.ContainsKey("building"))
            return MapProperty.Building;
        if (geometryType == GeometryType.Polygon && properties.ContainsKey("leisure"))
            return MapProperty.LandusePlain;
        if (geometryType == GeometryType.Polygon && properties.ContainsKey("amenity"))
            return MapProperty.LanduseResidential;

        return MapProperty.Unknown;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ForeachFeature(BoundingBox b, MapFeatureDelegate? action)
    {
        if (action == null) return;

        var tiles = TiligSystem.GetTilesForBoundingBox(b.MinLat, b.MinLon, b.MaxLat, b.MaxLon);
        for (var i = 0; i < tiles.Length; ++i)
        {
            var header = GetTile(tiles[i]);
            if (header.Tile == null) continue;
            for (var j = 0; j < header.Tile.Value.FeaturesCount; ++j)
            {
                var feature = GetFeature(j, header.TileOffset);
                var coordinates = GetCoordinates(header.Tile.Value.CoordinatesOffsetInBytes, feature->CoordinateOffset,
                    feature->CoordinateCount);
                var isFeatureInBBox = false;

                for (var k = 0; k < coordinates.Length; ++k)
                    if (b.Contains(coordinates[k]))
                    {
                        isFeatureInBBox = true;
                        break;
                    }

                var label = ReadOnlySpan<char>.Empty;
                if (feature->LabelOffset >= 0)
                    GetString(header.Tile.Value.StringsOffsetInBytes, header.Tile.Value.CharactersOffsetInBytes,
                        feature->LabelOffset, out label);

                if (isFeatureInBBox)
                {
                    var properties = new Dictionary<string, string>(feature->PropertyCount);
                    for (var p = 0; p < feature->PropertyCount; ++p)
                    {
                        GetProperty(header.Tile.Value.StringsOffsetInBytes, header.Tile.Value.CharactersOffsetInBytes,
                            p * 2 + feature->PropertiesOffset, out var key, out var value);
                        properties.Add(key.ToString(), value.ToString());
                    }

                    if (!action(new MapFeatureData
                        {
                            Id = feature->Id,
                            Label = label,
                            Coordinates = coordinates,
                            Type = feature->GeometryType,
                            Properties = properties,
                            MapProperty = ClassifyProperties(properties, feature->GeometryType)
                        }))
                        break;
                }
            }
        }
    }
}