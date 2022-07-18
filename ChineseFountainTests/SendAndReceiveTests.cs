using ChineseFountain;
using NUnit.Framework;

namespace ChineseFountainTests;

[TestFixture]
public class SendAndReceiveTests
{
    [Test(Description = "Demonstrate we can encode and decode when the channel is ordered, has no loss, and no corruption")]
    public void can_send_data_over_reliable_channel()
    {
        byte[] original = MakeData(4096);
        
        var encoder = CfCodec.EncodeForSend(original);
        var decoder = CfCodec.PrepareToReceive();
        
        
        var recSize = 0;
        int i;
        for (i = 0; i < 900; i++)
        {
            if (decoder.IsComplete()) break;
            var packet = encoder.NextPacket();
            
            recSize += packet.Length;
            
            var ok = decoder.Deliver(packet);
            Assert.That(ok, Is.True, "packet lost");
        }
        
        Assert.That(decoder.IsComplete(), Is.True, $"Failed to decode after {i} packets");
        Console.WriteLine($"Completed after {i} bundles: {recSize} bytes to transmit {original.Length}");
        
        var final = decoder.RecoverData(); // n^2 ?
        
        Assert.That(final.Length, Is.EqualTo(original.Length), "Data length is incorrect");

        for (i = 0; i < final.Length; i++)
        {
            Assert.That(final[i], Is.EqualTo(original[i]), $"Data corrupt at index {i}");
        }
    }
    
    [Test(Description = "Demonstrate we can encode and decode when the channel is ordered, has no corruption; but looses some packets")]
    public void can_send_data_over_lossy_channel()
    {
        byte[] original = MakeData(4096);
        
        var encoder = CfCodec.EncodeForSend(original);
        var decoder = CfCodec.PrepareToReceive();
        
        
        var recSize = 0;
        int i;
        for (i = 0; i < 900; i++)
        {
            if (decoder.IsComplete()) break;
            var packet = encoder.NextPacket();
            
            recSize += packet.Length;
            
            if (i % 3 == 0 || i % 5 == 0) continue; // packet lost
            var ok = decoder.Deliver(packet);
            Assert.That(ok, Is.True, "packet lost");
        }
        
        Assert.That(decoder.IsComplete(), Is.True, $"Failed to decode after {i} packets");
        Console.WriteLine($"Completed after {i} bundles: {recSize} bytes to transmit {original.Length}");
        
        var final = decoder.RecoverData(); // n^2 ?
        
        Assert.That(final.Length, Is.EqualTo(original.Length), "Data length is incorrect");

        for (i = 0; i < final.Length; i++)
        {
            Assert.That(final[i], Is.EqualTo(original[i]), $"Data corrupt at index {i}");
        }
    }
    
    
    [Test(Description = "Demonstrate we can encode and decode when the channel is ordered, has no loss; but corrupts some packets")]
    public void can_send_data_over_noisy_channel()
    {
        byte[] original = MakeData(4096);
        
        var encoder = CfCodec.EncodeForSend(original);
        var decoder = CfCodec.PrepareToReceive();
        
        var rnd = new Random();
        
        var recSize = 0;
        int i;
        for (i = 0; i < 900; i++)
        {
            if (decoder.IsComplete()) break;
            var packet = encoder.NextPacket();
            
            recSize += packet.Length;

            if (i % 3 == 0 || i % 5 == 0)
            {
                Console.WriteLine($"damage caused on packet {i}");
                packet[rnd.Next(0, packet.Length)] = (byte)rnd.Next(); // damage one byte at random
            }
            
            var ok = decoder.Deliver(packet);
            if (!ok) Console.WriteLine($"damage detected on packet {i}");
        }
        
        Assert.That(decoder.IsComplete(), Is.True, $"Failed to decode after {i} packets");
        Console.WriteLine($"Completed after {i} bundles: {recSize} bytes to transmit {original.Length}");
        
        var final = decoder.RecoverData(); // n^2 ?
        
        Assert.That(final.Length, Is.EqualTo(original.Length), "Data length is incorrect");

        for (i = 0; i < final.Length; i++)
        {
            Assert.That(final[i], Is.EqualTo(original[i]), $"Data corrupt at index {i}");
        }
    }
    
    [Test]
    public void can_send_data_over_disordered_channel()
    {
        byte[] original = MakeData(4096);
        
        var encoder = CfCodec.EncodeForSend(original);
        var decoder = CfCodec.PrepareToReceive();
        
        var inputPackets = new List<byte[]>();
        
        int i;
        for (i = 0; i < 50; i++)
        {
            inputPackets.Add(encoder.NextPacket());
        }
        
        Shuffle(inputPackets);
        
        for (i = 0; i < 50; i++)
        {
            if (decoder.IsComplete()) break;
            var packet = inputPackets[i];
            
            var ok = decoder.Deliver(packet);
            Assert.That(ok, Is.True, "packet lost");
        }
        
        Assert.That(decoder.IsComplete(), Is.True, $"Failed to decode after {i} packets");
        
        var final = decoder.RecoverData();
        
        Assert.That(final.Length, Is.EqualTo(original.Length), "Data length is incorrect");

        for (i = 0; i < final.Length; i++)
        {
            Assert.That(final[i], Is.EqualTo(original[i]), $"Data corrupt at index {i}");
        }
    }

    private static void Shuffle<T>(List<T> list)
    {
        var rnd = new Random();
        for (var dst = list.Count - 1; dst > 0; dst--)
        {
            var src = rnd.Next(0,dst);
            (list[dst], list[src]) = (list[src], list[dst]);
        }
    }

    private static byte[] MakeData(int size)
    {
        var rnd = new Random();
        var data = new byte[size];
        rnd.NextBytes(data);
        return data;
    }
}