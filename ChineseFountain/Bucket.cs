namespace ChineseFountain;

/// <summary>
/// Data sink for the Fountain.
/// This receives 'bundles' and tries to reconstruct the
/// original data fed to the fountain.
/// </summary>
public class Bucket: ChineseBase
{
    
    private readonly int length;
    private readonly int bundle_size;
    private readonly int bundle_shorts;
    private readonly int padded_length;
    private readonly int slice_size;
    private readonly int min_bundles;
    private readonly int hunk_size;
    private readonly int num_hunks;
    private readonly ByteMap bundles;

    public Bucket(int length, int bundle_size) {
        this.length = length;
        this.bundle_size = bundle_size;
        bundle_shorts = this.bundle_size / SIZE_OF_SHORT;
        assert(bundle_shorts * SIZE_OF_SHORT == bundle_size); // throw if odd bundle_size
        padded_length = div_round_up(this.length, this.bundle_size) * this.bundle_size;
        slice_size = SIZE_OF_SHORT;
        min_bundles = padded_length / this.bundle_size;
        hunk_size = min_bundles * slice_size;
        num_hunks = 0 | (padded_length / hunk_size);
        assert(num_hunks == padded_length / hunk_size);

        bundles = new ByteMap(); // TODO: IEB: container object!
    }
    
    /// <summary>
    /// Feed more data into the bucket
    /// </summary>
    /// <param name="bundle_num">the bundle index</param>
    /// <param name="bundle_data">data we received</param>
    public void Push(int bundle_num, byte[] bundle_data) {
        assert(bundle_data.Length == bundle_size);
        bundles[bundle_num] = bundle_data;
    }
    
    /// <summary>
    /// Returns true if we have enough bundles to recover the data
    /// </summary>
    public bool IsComplete() {
        // do the cops multiply to more than the min_bundles value
        var prod = new Big(1);
        foreach (var bundle_num in bundles.Keys) {
            //console.log('prod', prod);
            //console.log('bundle_num', bundle_num);
            prod = prod.mul(CoPrimes.coprime16(bundle_num));
        }
        /*
        var t0 = new Big(this.min_bundles);
        //console.log('t0', t0);
        var t1 = new Big(65536).pow(t0);
        */
        
        var t1 = Big.pow(65536, (uint)min_bundles);
        var t2 = prod.gt(t1);
        return t2; //prod.gt(mpz(65536).pow(mpz(this.num_hunks)));
    }
    
    /// <summary>
    /// Try to recover the original data given the bundles we have received
    /// </summary>
    /// <returns></returns>
    public byte[] RecoverData() {
        //console.log('--------------------');
        var hunks = new List<byte[]>();
        var subset_cops = new List<Big>();
        foreach (var bundle_num in bundles.Keys) {
            subset_cops.Add(CoPrimes.coprime16(bundle_num));
        }
        for (var hunk_num = 0; hunk_num < num_hunks; hunk_num++) {
            //console.log('~~~~~~~~~~~~~~~~~~~~~~~');
            var parts = new List<Big>();
            foreach (var bundle_num in bundles.Keys) {
                parts.Add(new Big(256 * bundles[bundle_num][hunk_num * slice_size] +
                               bundles[bundle_num][hunk_num * slice_size + 1]));
            }
            //console.log('parts', parts);
            //console.log('subset_cops', subset_cops);
            var mpz_hunk = CoPrimes.combine(parts, subset_cops);
            var hunk = mpz_hunk.ToBuffer();
            if (hunk.Length < hunk_size) {
                //console.log('short hunk');
                var padding = new byte[hunk_size - hunk.Length];
                //padding.set(0);
                //hunk = Buffer.concat([hunk, padding]);
                hunks.Add(hunk);
                hunks.Add(padding);
                assert(hunk.Length + padding.Length == hunk_size);
            } else if (hunk.Length > hunk_size) {
                //console.log('long hunk', hunk.length, this.hunk_size);
                //console.log('mpz_hunk', mpz_hunk);
                //console.log('hunk', hunk);

                //mpz_hunk = CoPrimes.combine(Slice(parts, 0, -1), Slice(subset_cops,0, -1));
                mpz_hunk = CoPrimes.combine(RemoveLast(parts), RemoveLast(subset_cops));
                hunk = mpz_hunk.ToBuffer();

                //console.log('mpz_hunk', mpz_hunk);
                //console.log('hunk', hunk);
                assert(hunk.Length == hunk_size);
                //process.exit(1);
                throw new Exception("not sure what this is about?"); // TODO: find out
            } else {
                //console.log('hunk size OK');
                //console.log('mpz_hunk', mpz_hunk);
                //console.log('hunk', hunk);
                hunks.Add(hunk);
            }
            //hunks.push(hunk);
        }
        /*
        var buffer = Buffer.concat(hunks);
        console.log(buffer.length, this.padded_length);
        assert(buffer.length === this.padded_length);
        return buffer.slice(0, this.length);
        */
        var fullLength = WholeSize(hunks);
        assert(fullLength == padded_length);
        return RepackToArray(hunks, length);
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
        var output = new byte[size];
        var position = 0;

        foreach (var byteValue in hunks.SelectMany(hunk => hunk))
        {
            output[position++] = byteValue;
        }

        return output;
    }

    private static int WholeSize(IEnumerable<byte[]> hunks) => hunks.Sum(hunk => hunk.Length);
}