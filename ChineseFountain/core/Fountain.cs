namespace ChineseFountain.core;

/// <summary>
/// Generates packets from data
/// </summary>
public class Fountain: ChineseBase
{
    private readonly int _bundleSize;
    private readonly int _bundleShorts;
    private readonly Big[] _bigIntHunks;

    /// <summary>
    /// Create a new 'fountain' source for the given data to be transmitted
    /// </summary>
    /// <param name="data">Complete data to be transmitted</param>
    /// <param name="bundleSize">size that each transmit packet should be (in bytes)</param>
    public Fountain(byte[] data, int bundleSize) {
        _bundleSize = bundleSize;
        _bundleShorts = _bundleSize / SizeOfShort;
        Assert(_bundleShorts * SizeOfShort == bundleSize, ()=>"Bundle size is odd"); // throw if odd bundle_size

        var length = data.Length;
        var paddedLength = div_round_up(length, _bundleSize) * _bundleSize;
        var padding = new byte[paddedLength - length];
        
        var paddedData = data.Concat(padding).ToArray();

        // TODO: handle data whose size is not divisible by BUNDLE_SHORTS
        var minBundles = paddedLength / _bundleSize;
        if (minBundles > 100) throw new Exception($"data too long, would require more than {minBundles} bundles");

        var hunkSize = minBundles * SizeOfShort;
        var numHunks = 0 | (paddedLength / hunkSize);
        Assert(numHunks == paddedLength / hunkSize, ()=>"Hunk size does not match number of hunks");

        _bigIntHunks = new Big[numHunks];
        for (var i = 0; i < numHunks; i++) {
            //var hunk = this.padded_data.slice(i * this.hunk_size, (i+1) * this.hunk_size); // inclusive lower bound, exclusive upper bound
            var hunk = paddedData.Skip(i * hunkSize).Take(hunkSize).ToArray(); // inclusive lower bound, exclusive upper bound
            
            Assert(hunkSize == hunk.Length, ()=>$"Hunk size {hunk.Length} does not match expected {hunkSize}");
            _bigIntHunks[i] = Big.FromBuffer(hunk);
        }
    }

    /// <summary>
    /// Generate a new transmit packet.
    /// These should be sent to the receiver.
    /// When enough are gathered, the receiver should be able to
    /// correctly reconstruct the original data.
    /// </summary>
    /// <param name="bundleNum">Count of the bundle. This should start at zero and increment.</param>
    /// <param name="extraSize">Extra bytes to include in the bundle</param>
    /// <param name="offset">Byte offset into the bundle that bytes are written</param>
    public byte[] Generate(int bundleNum, int extraSize = 0, int offset = 0) {
        var buffer = new byte[_bundleSize + extraSize];
        for (var i = 0; i < _bundleShorts; i++) {
            var bigIntHunk = _bigIntHunks[i];
            
            var part = bigIntHunk.Mod(CoPrimes.CoPrime16(bundleNum));
            
            var partBuf = part.ToBuffer();
            
            if (partBuf.Length == 2) {
                buffer[offset + i * SizeOfShort] = partBuf[0];
                buffer[offset + i * SizeOfShort + 1] = partBuf[1];
            } else if (partBuf.Length == 1) {
                buffer[offset + i * SizeOfShort] = 0;
                buffer[offset + i * SizeOfShort + 1] = partBuf[0];
            }else if (partBuf.Length == 0) { // an actual 'zero' value
                buffer[offset + i * SizeOfShort] = 0;
                buffer[offset + i * SizeOfShort + 1] = 0;
            } else {
                throw new Exception($"Data overflow. Bundle {bundleNum} created a packet of size {partBuf.Length}");
            }
        }
        return buffer;
    }
    
}