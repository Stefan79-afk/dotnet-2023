using System.Collections;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Collections;

namespace OSMDataParser;

internal class StringTable : IReadOnlyList<string>
{
    private readonly IList<ByteString> _stringTable;

    public StringTable(IList<ByteString>? stringTable = null)
    {
        _stringTable = stringTable == null ? new RepeatedField<ByteString>() : stringTable;
    }

    public string this[int index]
    {
        get
        {
            var bytes = _stringTable[index].Span;
            return Encoding.UTF8.GetString(bytes);
        }
    }

    public int Count => _stringTable.Count;

    public IEnumerator<string> GetEnumerator()
    {
        return new StringTableEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

internal class StringTableEnumerator : IEnumerator<string>
{
    private readonly int _count;
    private readonly StringTable _stringTable;
    private int _currentIndex;
    private bool _disposedValue;

    public StringTableEnumerator(StringTable stringTable)
    {
        _stringTable = stringTable;
        _count = stringTable.Count;
    }

    public string Current { get; private set; } = string.Empty;

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_currentIndex >= _count)
            return false;

        Current = _stringTable[_currentIndex++];
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