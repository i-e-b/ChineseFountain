namespace ChineseFountain.core;

/// <summary>
/// Data sink for the Fountain.
/// This receives 'bundles' and tries to reconstruct the
/// original data fed to the fountain.
/// </summary>
public class Bucket: ChineseBase
{
    private readonly int _length;
    private readonly int _bundleSize;
    private readonly int _sliceSize;
    private readonly int _minBundles;
    private readonly int _hunkSize;
    private readonly int _numHunks;
    private readonly ByteMap _bundles;

    public Bucket(int length, int bundleSize) {
        _length = length;
        _bundleSize = bundleSize;
        var bundleShorts = _bundleSize / SizeOfShort;
        Assert(bundleShorts * SizeOfShort == bundleSize, ()=>"bundle size is odd"); // throw if odd bundle_size
        var paddedLength = div_round_up(_length, _bundleSize) * _bundleSize;
        _sliceSize = SizeOfShort;
        _minBundles = paddedLength / _bundleSize;
        _hunkSize = _minBundles * _sliceSize;
        _numHunks = 0 | (paddedLength / _hunkSize);
        Assert(_numHunks == paddedLength / _hunkSize, ()=>"hunk size does not match hunk count");

        _bundles = new ByteMap();
    }
    
    /// <summary>
    /// Feed more data into the bucket
    /// </summary>
    /// <param name="bundleNum">the bundle index</param>
    /// <param name="bundleData">data we received</param>
    public void Push(int bundleNum, byte[] bundleData) {
        Assert(bundleData.Length == _bundleSize, ()=>$"bundle data size {bundleData.Length} does not match expected bundle size {_bundleSize}");
        _bundles[bundleNum] = bundleData;
    }
    
    /// <summary>
    /// Returns true if we have enough bundles to recover the data
    /// </summary>
    public bool IsComplete() {
        // do the cops multiply to more than the min_bundles value
        var prod = new Big(1);
        foreach (var bundleNum in _bundles.BundleNumbers) {
            prod = prod.Mul(CoPrimes.CoPrime16(bundleNum));
        }
        
        var t1 = Big.Pow(65536, (uint)_minBundles);
        var t2 = prod.GT(t1);
        return t2;
    }
    
    /// <summary>
    /// Try to recover the original data given the bundles we have received
    /// </summary>
    /// <returns></returns>
    public byte[] RecoverData() {
        var hunks = new byte[_length];
        var pos = 0;
        var subsetCops = new List<Big>();
        foreach (var bundleNum in _bundles.BundleNumbers) {
            subsetCops.Add(CoPrimes.CoPrime16(bundleNum));
        }
        
        var bAndC = CoPrimes.CalculateCoefficients(subsetCops); // pre-compute as much as we can
        var copsCount = subsetCops.Count;
        
        for (var hunkNum = 0; hunkNum < _numHunks; hunkNum++) {
            var parts = new List<Big>();
            foreach (var bytes in _bundles.ByteSets) {
                // recover the 16-bit terms
                parts.Add(new Big((bytes[hunkNum * _sliceSize] << 8) + bytes[hunkNum * _sliceSize + 1]));
            }
            
            var bigIntHunk = CoPrimes.Combine(parts, bAndC, copsCount);
            
            var hunk = bigIntHunk.ToBuffer();
            if (hunk.Length < _hunkSize) {
                var padding = new byte[_hunkSize - hunk.Length];
                
                pos = Add(hunks, pos, padding);
                pos = Add(hunks, pos, hunk);
                Assert(hunk.Length + padding.Length == _hunkSize, () => "Unexpected hunk size after padding");
            } else if (hunk.Length > _hunkSize) {
                bigIntHunk = CoPrimes.Combine(parts, subsetCops, subsetCops.Count - 1); // combine, ignoring last element
                hunk = bigIntHunk.ToBuffer();

                Assert(hunk.Length == _hunkSize, ()=>"Unexpected hunk size after combining");
                pos = Add(hunks, pos, hunk);
            } else {
                pos = Add(hunks, pos, hunk);
            }
        }
        
        return hunks;
    }

    private static int Add(byte[] dst, int offset, byte[] src)
    {
        var limit = dst.Length - offset;
        if (limit > src.Length) limit = src.Length;
        
        for (var i = 0; i < limit; i++)
        {
            dst[i + offset] = src[i];
        }
        
        return offset + limit;
    }
}