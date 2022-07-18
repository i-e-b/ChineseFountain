namespace ChineseFountain;

/// <summary>
/// Data sink for the Fountain.
/// This receives 'bundles' and tries to reconstruct the
/// original data fed to the fountain.
/// </summary>
public class Bucket: ChineseBase
{
    
    private readonly int _length;
    private readonly int _bundleSize;
    private readonly int _paddedLength;
    private readonly int _sliceSize;
    private readonly int _minBundles;
    private readonly int _hunkSize;
    private readonly int _numHunks;
    private readonly ByteMap _bundles;

    public Bucket(int length, int bundleSize) {
        _length = length;
        _bundleSize = bundleSize;
        var bundleShorts = _bundleSize / SizeOfShort;
        Assert(bundleShorts * SizeOfShort == bundleSize); // throw if odd bundle_size
        _paddedLength = div_round_up(_length, _bundleSize) * _bundleSize;
        _sliceSize = SizeOfShort;
        _minBundles = _paddedLength / _bundleSize;
        _hunkSize = _minBundles * _sliceSize;
        _numHunks = 0 | (_paddedLength / _hunkSize);
        Assert(_numHunks == _paddedLength / _hunkSize);

        _bundles = new ByteMap();
    }
    
    /// <summary>
    /// Feed more data into the bucket
    /// </summary>
    /// <param name="bundleNum">the bundle index</param>
    /// <param name="bundleData">data we received</param>
    public void Push(int bundleNum, byte[] bundleData) {
        Assert(bundleData.Length == _bundleSize);
        _bundles[bundleNum] = bundleData;
    }
    
    /// <summary>
    /// Returns true if we have enough bundles to recover the data
    /// </summary>
    public bool IsComplete() {
        // do the cops multiply to more than the min_bundles value
        var prod = new Big(1);
        foreach (var bundleNum in _bundles.Keys) {
            prod = prod.Mul(CoPrimes.CoPrime16(bundleNum));
        }
        
        var t1 = Big.Pow(65536, (uint)_minBundles);
        var t2 = prod.GT(t1);
        return t2; //prod.gt(mpz(65536).pow(mpz(this.num_hunks)));
    }
    
    /// <summary>
    /// Try to recover the original data given the bundles we have received
    /// </summary>
    /// <returns></returns>
    public byte[] RecoverData() {
        var hunks = new List<byte[]>();
        var subsetCops = new List<Big>();
        foreach (var bundleNum in _bundles.Keys) {
            subsetCops.Add(CoPrimes.CoPrime16(bundleNum));
        }
        for (var hunkNum = 0; hunkNum < _numHunks; hunkNum++) {
            var parts = new List<Big>();
            foreach (var bundleNum in _bundles.Keys) {
                parts.Add(new Big(256 * _bundles[bundleNum][hunkNum * _sliceSize] +
                               _bundles[bundleNum][hunkNum * _sliceSize + 1]));
            }
            
            var bigIntHunk = CoPrimes.Combine(parts, subsetCops);
            var hunk = bigIntHunk.ToBuffer();
            if (hunk.Length < _hunkSize) {
                var padding = new byte[_hunkSize - hunk.Length];
                
                hunks.Add(padding);
                hunks.Add(hunk);
                Assert(hunk.Length + padding.Length == _hunkSize);
            } else if (hunk.Length > _hunkSize) {
                bigIntHunk = CoPrimes.Combine(RemoveLast(parts), RemoveLast(subsetCops));
                hunk = bigIntHunk.ToBuffer();

                Assert(hunk.Length == _hunkSize);
                hunks.Add(hunk);
            } else {
                hunks.Add(hunk);
            }
        }
        
        var fullLength = WholeSize(hunks);
        Assert(fullLength == _paddedLength);
        return RepackToArray(hunks, _length);
    }

    private static List<Big> RemoveLast(IReadOnlyList<Big> src)
    {
        var dst = new List<Big>();
        for (var i = 0; i < src.Count - 1; i++)
        {
            dst.Add(src[i]);
        }
        return dst;
    }

    private byte[] RepackToArray(List<byte[]> hunks, int size)
    {
        var output = new byte[size]; // this is the size it should be
        var position = 0;

        foreach (var byteValue in hunks.SelectMany(hunk => hunk))
        {
            output[position++] = byteValue;
            if (position >= size) break; // sometimes we will go over due to bundle size
        }

        return output;
    }

    private static int WholeSize(IEnumerable<byte[]> hunks) => hunks.Sum(hunk => hunk.Length);
}