namespace ChineseFountain;

/// <summary>
/// An out-of-order container for bytes
/// </summary>
public class ByteMap
{
    // TODO: replace with position/value pairs
    
    private readonly Dictionary<int, byte[]> _data = new();

    public byte[] this[int bundleNum]
    {
        get => _data[bundleNum];
        set {
            if (_data.ContainsKey(bundleNum)) { _data[bundleNum] = value; }
            else                              { _data.Add(bundleNum, value); }
        }
    }

    public IEnumerable<int> Keys => _data.Keys.ToArray();
}