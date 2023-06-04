using System.Collections;

namespace OSMDataParser;

using Tag = KeyValuePair<string, string>;

public abstract class AbstractTagList : IReadOnlyList<Tag>
{
    public abstract Tag this[int index] { get; }

    public abstract int Count { get; }

    public IEnumerator<Tag> GetEnumerator()
    {
        return new TagEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class TagEnumerator : IEnumerator<Tag>
{
    private int _currentIndex;
    private bool _disposedValue;
    private readonly int _tagCount;
    private readonly AbstractTagList _tagList;

    public TagEnumerator(AbstractTagList tagList)
    {
        _tagList = tagList;
        _tagCount = tagList.Count;
    }

    public Tag Current { get; private set; }

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_currentIndex >= _tagCount)
            return false;

        Current = _tagList[_currentIndex++];
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