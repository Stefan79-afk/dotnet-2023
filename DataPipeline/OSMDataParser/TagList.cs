namespace OSMDataParser;

using Tag = KeyValuePair<string, string>;

public class TagList : AbstractTagList
{
    private readonly IList<uint> _keys;
    private readonly StringTable _stringTable;
    private readonly IList<uint> _values;

    internal TagList(IList<uint> keys, IList<uint> values, StringTable stringTable)
    {
        _keys = keys;
        _values = values;
        _stringTable = stringTable;
    }

    public override Tag this[int index]
    {
        get
        {
            var key = (int)_keys[index];
            var value = (int)_values[index];
            return new Tag(_stringTable[key], _stringTable[value]);
        }
    }

    public override int Count => _keys.Count;
}