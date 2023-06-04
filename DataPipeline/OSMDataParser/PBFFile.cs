using System.Buffers.Binary;
using System.Collections;
using Google.Protobuf;
using OSMPBF;

namespace OSMDataParser;

public class PBFFile : IEnumerable<Blob>
{
    private bool _disposedValue;
    private readonly FileStream _fileStream;

    public PBFFile(ReadOnlySpan<char> filePath)
    {
        _fileStream = File.OpenRead(filePath.ToString());
    }

    public IEnumerator<Blob> GetEnumerator()
    {
        return new BlobEnumerator(_fileStream);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing) _fileStream.Dispose();

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

public class BlobEnumerator : IEnumerator<Blob>
{
    private const int MAX_BLOB_HEADER_SIZE = 64 * 1024;
    private const int MAX_BLOB_MESSAGE_SIZE = 32 * 1024 * 1024;

    private static readonly ThreadLocal<byte[]> _headerSizeBuffer = new(() => new byte[4]);

    private bool _disposedValue;
    private readonly Stream _stream;

    public BlobEnumerator(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        _stream = stream;
    }

    public Blob Current { get; private set; } = new();

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        var stream = _stream;

        if (stream.Position == stream.Length)
            return false;

        var remainingBytes = stream.Length - 1 - stream.Position;
        if (remainingBytes == 0)
            throw new IndexOutOfRangeException(
                $"Not enough bytes to read header size at position {stream.Position}; expected 4 bytes -> available {remainingBytes} bytes");

        var headerSizeBuffer = _headerSizeBuffer.Value!;
        stream.Read(headerSizeBuffer, 0, headerSizeBuffer.Length);
        var headerSize = (int)BinaryPrimitives.ReadUInt32BigEndian(headerSizeBuffer.AsSpan());

        if (headerSize >= MAX_BLOB_HEADER_SIZE)
            throw new ArgumentOutOfRangeException(
                $"Header is too large. Header size {headerSize} exceeds the maximum of {MAX_BLOB_HEADER_SIZE} bytes");

        var blobHeader = DeserializeMessage<BlobHeader>(stream, headerSize);

        if (blobHeader.Datasize >= MAX_BLOB_MESSAGE_SIZE)
            throw new ArgumentOutOfRangeException(
                $"Blob is too large. Blob size {blobHeader.Datasize} exceeds the maximum of {MAX_BLOB_MESSAGE_SIZE} bytes");

        var protoBlob = DeserializeMessage<OSMPBF.Blob>(stream, blobHeader.Datasize);
        var blobType = BlobType.Unknown;
        switch (blobHeader.Type)
        {
            case "OSMHeader":
                blobType = BlobType.Header;
                break;

            case "OSMData":
                blobType = BlobType.Primitive;
                break;

            default:
                throw new InvalidDataException($"Unknown or unsupported blob type: {blobHeader.Type}");
        }

        switch (protoBlob.DataCase)
        {
            case OSMPBF.Blob.DataOneofCase.Raw:
            {
                Current = new Blob(blobType, true, protoBlob.Raw.Memory);
                break;
            }
            case OSMPBF.Blob.DataOneofCase.ZlibData:
            {
                Current = new Blob(blobType, true, protoBlob.ZlibData.Memory);
                break;
            }
            case OSMPBF.Blob.DataOneofCase.None: throw new InvalidDataException("Blob does not contain any data");
            case OSMPBF.Blob.DataOneofCase.LzmaData:
            case OSMPBF.Blob.DataOneofCase.OBSOLETEBzip2Data:
            case OSMPBF.Blob.DataOneofCase.Lz4Data:
            case OSMPBF.Blob.DataOneofCase.ZstdData:
                throw new InvalidDataException($"Unsupported data compression used in blob: {protoBlob.DataCase}");
        }

        return true;
    }

    public void Reset()
    {
        _stream?.Seek(0, SeekOrigin.Begin);
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
            if (disposing) _stream?.Dispose();

            _disposedValue = true;
        }
    }

    private static T DeserializeMessage<T>(Stream stream, int size) where T : IMessage<T>, new()
    {
        var remainingBytes = stream.Length - stream.Position;
        if (remainingBytes < size)
            throw new IndexOutOfRangeException(
                $"Not enough bytes to read data at position {stream.Position}. Expected {size} bytes -> available {remainingBytes} bytes");

        T result;
        var buffer = new byte[size];
        stream.Read(buffer, 0, size);
        result = new MessageParser<T>(() => new T()).ParseFrom(new ReadOnlySpan<byte>(buffer));

        return result;
    }
}