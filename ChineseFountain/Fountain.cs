namespace ChineseFountain;

/// <summary>
/// Generates packets from data
/// </summary>
public class Fountain: ChineseBase
{
    private int bundle_size;
    private int bundle_shorts;
    private int length;
    private int padded_length;
    private byte[] padded_data;
    private int min_bundles;
    private int slice_size;
    private int hunk_size;
    private int num_hunks;
    private Big[] mpz_hunks;

    // <summary>
    /// Create a new 'fountain' source for the given data to be transmitted
    /// </summary>
    /// <param name="data">Complete data to be transmitted</param>
    /// <param name="bundleSize">size that each transmit packet should be (in bytes)</param>
    public Fountain(byte[] data, int bundleSize) {
        bundle_size = bundleSize;
        bundle_shorts = 0 | (bundle_size / SIZE_OF_SHORT);
        assert(bundle_shorts * SIZE_OF_SHORT == bundleSize); // throw if odd bundle_size

        length = data.Length;
        padded_length = div_round_up(length, bundle_size) * bundle_size;
        var padding = new byte[padded_length - length];
        
        padded_data = data.Concat(padding).ToArray(); // see if this can be just Enumerable<byte>

        // TODO: handle data whose size is not divisible by BUNDLE_SHORTS
        min_bundles = padded_length / bundle_size;
        if (min_bundles > 100) throw new Exception($"data too long, would require more than {min_bundles} bundles");

        slice_size = SIZE_OF_SHORT;
        hunk_size = min_bundles * slice_size;
        num_hunks = 0 | (padded_length / hunk_size);
        assert(num_hunks == padded_length / hunk_size);

        mpz_hunks = new Big[num_hunks];
        for (var i = 0; i < num_hunks; i++) {
            //var hunk = this.padded_data.slice(i * this.hunk_size, (i+1) * this.hunk_size); // inclusive lower bound, exclusive upper bound
            var hunk = padded_data.Skip(i * hunk_size).Take(hunk_size).ToArray(); // inclusive lower bound, exclusive upper bound
            
            assert(hunk_size == hunk.Length);
            mpz_hunks[i] = Big.FromBuffer(hunk);
        }
    }
    
    /// <summary>
    /// Generate a new transmit packet.
    /// These should be sent to the receiver.
    /// When enough are gathered, the receiver should be able to
    /// correctly reconstruct the original data.
    /// </summary>
    /// <param name="bundleNum">Count of the bundle. This should start at zero and increment.</param>
    public byte[] Generate(int bundleNum) {
        var buffer = new byte[bundle_size];
        for (var i = 0; i < bundle_shorts; i++) {
            var mpz_hunk = mpz_hunks[i];
            
            var part = mpz_hunk.mod(CoPrimes.coprime16(bundleNum));
            
            var part_buf = part.ToBuffer();
            
            if (part_buf.Length == 2) {
                buffer[i * SIZE_OF_SHORT] = part_buf[0];
                buffer[i * SIZE_OF_SHORT + 1] = part_buf[1];
            } else if (part_buf.Length == 1) {
                buffer[i * SIZE_OF_SHORT] = 0;
                buffer[i * SIZE_OF_SHORT + 1] = part_buf[0];
            }else if (part_buf.Length == 0) { // an actual 'zero' value
                buffer[i * SIZE_OF_SHORT] = 0;
                buffer[i * SIZE_OF_SHORT + 1] = 0;
            } else {
                throw new Exception($"Data overflow. Bundle {bundleNum} created a packet of size {part_buf.Length}");
            }
        }
        return buffer;
    }
    
}