using System.Collections;
using OSMDataParser.Elements;

namespace OSMDataParser;

public class PrimitiveGroup : IEnumerable<AbstractElement>
{
    public enum ElementType
    {
        Unknown,
        Node,
        Way,
        Relation,
        ChangeSet
    }

    private readonly DenseNodes? _denseNodes;

    private readonly OSMPBF.PrimitiveGroup _osmPrimitiveGroup;
    private readonly PrimitiveBlock _primitiveBlock;

    internal PrimitiveGroup(PrimitiveBlock primitiveBlock, OSMPBF.PrimitiveGroup? osmPrimitiveGroup = null,
        StringTable? stringTable = null)
    {
        _osmPrimitiveGroup = osmPrimitiveGroup == null ? new OSMPBF.PrimitiveGroup() : osmPrimitiveGroup;
        _primitiveBlock = primitiveBlock;

        StringTable = stringTable = stringTable == null ? new StringTable() : stringTable;

        // Since a PrimitiveGroup only contains one type of item we look for the repetition that has a .Count > 0
        // to determinte the type of the whole group
        if (_osmPrimitiveGroup.Nodes.Count > 0)
        {
            ElementGetter = GetNode;
            ContainedType = ElementType.Node;
            Count = _osmPrimitiveGroup.Nodes.Count;
        }
        else if (_osmPrimitiveGroup.Ways.Count > 0)
        {
            ElementGetter = GetWay;
            ContainedType = ElementType.Way;
            Count = _osmPrimitiveGroup.Ways.Count;
        }
        else if (_osmPrimitiveGroup.Relations.Count > 0)
        {
            ElementGetter = GetRelation;
            ContainedType = ElementType.Relation;
            Count = _osmPrimitiveGroup.Relations.Count;
        }
        else if (_osmPrimitiveGroup.Changesets.Count > 0)
        {
            ElementGetter = GetChangeSet;
            ContainedType = ElementType.ChangeSet;
            Count = _osmPrimitiveGroup.Changesets.Count;
        }
        else if (_osmPrimitiveGroup.Dense != null && _osmPrimitiveGroup.Dense.Id.Count > 1)
        {
            ElementGetter = GetNextDenseNode;
            _denseNodes = new DenseNodes(_osmPrimitiveGroup.Dense, _primitiveBlock);

            ContainedType = ElementType.Node;
            Count = _denseNodes.Count;
        }
        else
        {
            ElementGetter = GetUnknown;
            ContainedType = ElementType.Unknown;
            Count = 0;
        }
    }

    internal StringTable StringTable { get; }
    internal Func<int, AbstractElementInternal> ElementGetter { get; }

    public ElementType ContainedType { get; }

    public int Count { get; }

    public IEnumerator<AbstractElement> GetEnumerator()
    {
        return new ElementEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private AbstractElementInternal GetUnknown(int index)
    {
        return new Unknown();
    }

    private AbstractElementInternal GetNode(int index)
    {
        return new SimpleNode(_osmPrimitiveGroup.Nodes[index], _primitiveBlock);
    }

    private AbstractElementInternal GetWay(int index)
    {
        return new Way(_osmPrimitiveGroup.Ways[index]);
    }

    private AbstractElementInternal GetRelation(int index)
    {
        return new Relation();
    }

    private AbstractElementInternal GetChangeSet(int index)
    {
        return new ChangeSet();
    }

    private AbstractElementInternal GetNextDenseNode(int index)
    {
        // Index is not used here since each element depends on the previous
        return _denseNodes!.GetNextNode();
    }
}

public class ElementEnumerator : IEnumerator<AbstractElement>
{
    private AbstractElementInternal _currentElement = new Unknown();
    private int _currentIndex;
    private bool _disposedValue;
    private readonly int _elementCount;
    private readonly PrimitiveGroup _primitiveGroup;

    public ElementEnumerator(PrimitiveGroup primitiveGroup)
    {
        _primitiveGroup = primitiveGroup;
        _elementCount = primitiveGroup.Count;
    }

    public AbstractElement Current => _currentElement;

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_currentIndex >= _elementCount)
            return false;

        var element = _primitiveGroup.ElementGetter(_currentIndex++);
        element.SetStringTable(_primitiveGroup.StringTable);

        _currentElement = element;
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