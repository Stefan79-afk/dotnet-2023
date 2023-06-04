using System.Collections;

namespace OSMDataParser;

public class PrimitiveBlock : IReadOnlyList<PrimitiveGroup>
{
    private readonly OSMPBF.PrimitiveBlock _osmPrimitiveBlock;
    private readonly StringTable _stringTable;

    public PrimitiveBlock(Blob blob)
    {
        _osmPrimitiveBlock = Detail.DeserializeContent<OSMPBF.PrimitiveBlock>(blob);
        _stringTable = new StringTable(_osmPrimitiveBlock.Stringtable.S);
    }

    // Granularity, units of nano-degrees, used to store coordinates in this block
    public int Granularity => _osmPrimitiveBlock.Granularity;

    // Offset value between the output coordinates coordinates and the granularity grid, in units of nano-degrees.
    public int DateGranularity => _osmPrimitiveBlock.DateGranularity;
    public long OffsetLatitude => _osmPrimitiveBlock.LatOffset;

    // Granularity of dates, normally represented in units of milliseconds since the 1970 epoch.
    public long OffsetLongitude => _osmPrimitiveBlock.LonOffset;

    public PrimitiveGroup this[int index]
    {
        get
        {
            var primitiveGroup = _osmPrimitiveBlock.Primitivegroup[index];
            return new PrimitiveGroup(this, primitiveGroup, _stringTable);
        }
    }

    public int Count => _osmPrimitiveBlock.Primitivegroup.Count;

    public IEnumerator<PrimitiveGroup> GetEnumerator()
    {
        return new PrimitiveGroupEnumerator(this, _osmPrimitiveBlock.Primitivegroup.Count);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class PrimitiveGroupEnumerator : IEnumerator<PrimitiveGroup>
{
    private readonly int _groupCount;
    private readonly PrimitiveBlock _primitiveBlock;
    private int _currentIndex;
    private bool _disposedValue;

    public PrimitiveGroupEnumerator(PrimitiveBlock primitiveBlock, int primitiveGroupCount)
    {
        _primitiveBlock = primitiveBlock;
        _groupCount = primitiveGroupCount;
        Current = new PrimitiveGroup(primitiveBlock);
    }

    public PrimitiveGroup Current { get; private set; }

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_currentIndex >= _groupCount)
            return false;

        Current = _primitiveBlock[_currentIndex++];
        return true;
    }

    public void Reset()
    {
        _currentIndex = 0;
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
            }

            _disposedValue = true;
        }
    }
}