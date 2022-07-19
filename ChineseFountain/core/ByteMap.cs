namespace ChineseFountain.core;

/// <summary>
/// An out-of-order container for bytes
/// </summary>
public class ByteMap
{
    // TODO: replace with position/value pairs
    
    private readonly List<NumberedBytes> _data = new();

    public byte[] this[int bundleNum]
    {
        set => _data.Add(new NumberedBytes{Index=bundleNum, Bytes=value});
    }

    public IEnumerable<int> BundleNumbers => _data.Select(d=>d.Index);

    public IEnumerable<byte[]> ByteSets => _data.Select(d=>d.Bytes);
}

internal class NumberedBytes
{
    public byte[] Bytes = Array.Empty<byte>();
    public int Index = 0;
}