using System.Text.Json.Serialization;
using OSMPBF;

namespace OSMDataParser.Elements;

public abstract class AbstractNode : AbstractElementInternal
{
    protected const double CoordinateFactor = .000000001;

    public abstract double Latitude { get; }
    public abstract double Longitude { get; }
}

internal class SimpleNode : AbstractNode
{
    private readonly Node _osmNode;
    private TagList? _tags;

    public SimpleNode(Node osmNode, PrimitiveBlock primitiveBlock)
    {
        _osmNode = osmNode;
        Latitude = CoordinateFactor * (primitiveBlock.OffsetLatitude + primitiveBlock.Granularity * osmNode.Lat);
        Longitude = CoordinateFactor * (primitiveBlock.OffsetLongitude + primitiveBlock.Granularity * osmNode.Lon);
    }

    public override long Id => _osmNode.Id;
    public override double Latitude { get; }
    public override double Longitude { get; }

    [JsonIgnore] public override AbstractTagList Tags => _tags == null ? throw new InvalidDataException() : _tags;

    internal override void SetStringTable(StringTable stringTable)
    {
        _tags = new TagList(_osmNode.Keys, _osmNode.Vals, stringTable);
    }
}

internal class DenseNode : AbstractNode
{
    private readonly DenseTagList _tags;

    public DenseNode(OSMPBF.DenseNodes osmDenseNodes, int index, int tagOffset, long previousId, long previousLat,
        long previousLon, PrimitiveBlock primitiveBlock)
    {
        _tags = new DenseTagList(osmDenseNodes.KeysVals, tagOffset);

        Id = previousId + osmDenseNodes.Id[index];

        var latitude = previousLat + osmDenseNodes.Lat[index];
        Latitude = CoordinateFactor * (primitiveBlock.OffsetLatitude + primitiveBlock.Granularity * latitude);

        var longitude = previousLon + osmDenseNodes.Lon[index];
        Longitude = CoordinateFactor * (primitiveBlock.OffsetLongitude + primitiveBlock.Granularity * longitude);
    }

    public override long Id { get; }
    public override double Latitude { get; }
    public override double Longitude { get; }
    public override AbstractTagList Tags => _tags;

    internal override void SetStringTable(StringTable stringTable)
    {
        _tags?.SetStringTable(stringTable);
    }

    private class DenseTagList : AbstractTagList
    {
        private readonly IList<int> _keyValues;
        private readonly int _offset;
        private StringTable? _stringTable;

        public DenseTagList(IList<int> keyValues, int offset)
        {
            _keyValues = keyValues;
            _offset = offset;

            var count = 0;
            for (var i = offset; i < keyValues.Count && keyValues[i] != 0; ++i, ++count) ;
            Count = count / 2;
        }

        public override KeyValuePair<string, string> this[int index]
        {
            get
            {
                if (_stringTable == null)
                    return new KeyValuePair<string, string>("", "");

                var key = _keyValues[_offset + index * 2];
                var value = _keyValues[_offset + index * 2 + 1];
                return new KeyValuePair<string, string>(_stringTable[key], _stringTable[value]);
            }
        }

        public override int Count { get; }

        public void SetStringTable(StringTable stringTable)
        {
            _stringTable = stringTable;
        }
    }
}