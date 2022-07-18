using ChineseFountain.core;

namespace ChineseFountain;

/// <summary>
/// Provides methods to send and receive data over a lossy and corrupting channel
/// </summary>
public class CfCodec
{
    public static CfEncoder EncodeForSend(byte[] data)
    {
        return new CfEncoder(data);
    }

    public static CfDecoder PrepareToReceive()
    {
        return new CfDecoder();
    }

    internal static class CfTools
    {
        public const int IndexSize = sizeof(int);
        public const int SourceSize = sizeof(int);
        public const int ChecksumSize = sizeof(int);

        public static int GetBundleSize(int dataLength)
        {
            var bundleSize = dataLength / 100;
            bundleSize += bundleSize % 2;
            if (bundleSize < 128) bundleSize = 128;
            return bundleSize;
        }

        public static uint DataHash(byte[] data, uint len)
        {
            var hash = len;
            for (uint i = 0; i < len; i++)
            {
                hash += data[i];
                hash ^= hash >> 16;
                hash *= 0x7feb352d;
                hash ^= hash >> 15;
                hash *= 0x846ca68b;
                hash ^= hash >> 16;
            }

            hash ^= len;
            hash ^= hash >> 16;
            hash *= 0x7feb352d;
            hash ^= hash >> 15;
            hash *= 0x846ca68b;
            hash ^= hash >> 16;
            hash += len;

            // never return zero value
            return hash == 0 ? 0x800800 : hash;
        }
    }

    public class CfDecoder
    {
        private Bucket? _coreDecode;
        private int _sourceDataSize;

        public bool IsComplete()
        {
            return _coreDecode is not null && _coreDecode.IsComplete();
        }

        /// <summary>
        /// Deliver latest packet from the sender.
        /// Returns true if packet was accepted, false otherwise
        /// </summary>
        public bool Deliver(byte[] packet)
        {
            // checksum packet, reject if failed
            var hash = CfTools.DataHash(packet, (uint)(packet.Length - 4));
            if (packet[^4] != (byte)((hash >> 24) & 0xff)) return false;
            if (packet[^3] != (byte)((hash >> 16) & 0xff)) return false;
            if (packet[^2] != (byte)((hash >> 8) & 0xff)) return false;
            if (packet[^1] != (byte)((hash >> 0) & 0xff)) return false;

            // assume data is good now.
            // read index and expected data size

            // write bundle index into header
            var index = (packet[0] << 24) | (packet[1] << 16) | (packet[2] << 8) | packet[3];

            // write length of original data into header
            var size = (packet[4] << 24) | (packet[5] << 16) | (packet[6] << 8) | packet[7];

            // setup or sanity check
            if (_coreDecode is null)
            {
                // First valid packet. Setup decoder
                var bundleSize = CfTools.GetBundleSize(size);
                _coreDecode = new Bucket(size, bundleSize);
                _sourceDataSize = size;
            }
            else
            {
                // Check packets match input
                if (size != _sourceDataSize) throw new Exception("Source data size claim changed during decode. Entire decode must be abandoned.");
            }

            // feed the data
            var coreData = packet.Skip(CfTools.IndexSize + CfTools.SourceSize).Take(packet.Length - (CfTools.IndexSize + CfTools.SourceSize + CfTools.ChecksumSize)).ToArray(); // todo: improve
            _coreDecode.Push(index, coreData);
            return true;
        }

        public byte[] RecoverData()
        {
            if (_coreDecode is null) throw new Exception("Decode has not received any valid packets");
            if (!_coreDecode.IsComplete()) throw new Exception("Decode has not received enough valid packets");

            return _coreDecode.RecoverData();
        }
    }

    public class CfEncoder
    {
        private readonly int _dataLength;
        private readonly Fountain _coreEncoder;
        private int _bundleIndex;

        public CfEncoder(byte[] data)
        {
            _dataLength = data.Length;
            // guess a good bundle size, preventing it from being too small
            var bundleSize = CfTools.GetBundleSize(data.Length);

            _coreEncoder = new Fountain(data, bundleSize);
            _bundleIndex = 1;
        }

        public byte[] NextPacket()
        {

            var index = _bundleIndex++;
            // get raw bundle, with extra space for header & footer
            var bundle = _coreEncoder.Generate(index,
                extraSize: CfTools.IndexSize + CfTools.SourceSize + CfTools.ChecksumSize,
                offset: CfTools.IndexSize + CfTools.SourceSize);

            // write bundle index into header
            bundle[0] = (byte)((index >> 24) & 0xff);
            bundle[1] = (byte)((index >> 16) & 0xff);
            bundle[2] = (byte)((index >> 8) & 0xff);
            bundle[3] = (byte)((index >> 0) & 0xff);

            // write length of original data into header
            bundle[4] = (byte)((_dataLength >> 24) & 0xff);
            bundle[5] = (byte)((_dataLength >> 16) & 0xff);
            bundle[6] = (byte)((_dataLength >> 8) & 0xff);
            bundle[7] = (byte)((_dataLength >> 0) & 0xff);

            // calculate checksum
            var hash = CfTools.DataHash(bundle, (uint)(bundle.Length - 4));

            // write sum to end
            bundle[^4] = (byte)((hash >> 24) & 0xff);
            bundle[^3] = (byte)((hash >> 16) & 0xff);
            bundle[^2] = (byte)((hash >> 8) & 0xff);
            bundle[^1] = (byte)((hash >> 0) & 0xff);

            return bundle;
        }
    }
}