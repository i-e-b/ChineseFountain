using System.Diagnostics;
using ChineseFountain.core;
using NUnit.Framework;

namespace ChineseFountainTests;

[TestFixture]
public class BasicTests
{
    // TODO: out of order; wrong sequence number; corrupted.
    // Will need to wrap the algo in an outer packet with x-sum and position?
    
    [Test]
    public void generating_many_bundles()
    {
        Big.ResetSize();
        byte[] randomData = MakeData(4096);
        var subject = new Fountain(randomData, 64);

        for (int i = 0; i < 900; i++) // 900*64 -- generating 57600 bytes of transmit from 3072 bytes of source
        { 
            _ = subject.Generate(i);
        }
        
        Console.WriteLine($"Maximum 'big int' size: {Big.MaxSize} bits ({Big.MaxSize / 8} bytes)");
    }
    
    
    [Test]
    public void giving_the_wrong_bundle_index_will_fail_to_recover()
    {
        Big.ResetSize();
        byte[] original = MakeData(4096);
        var source = new Fountain(original, 64);
        var target = new Bucket(original.Length, 64);

        int i;
        for (i = 0; i < 900; i++)
        {
            if (target.IsComplete()) break;
            target.Push(900 - i, source.Generate(i)); // Push is out-of-order from generate
        }
        
        var recSize = 64*(i-1);
        Console.WriteLine($"Completed after {i} bundles: {recSize} bytes to transmit {original.Length}");
        
        Assert.That(target.IsComplete(), Is.True, "Target did not complete");
        
        Assert.Throws(Is.Not.Null, () =>
        {
            _ = target.RecoverData();
        });

        Console.WriteLine($"Maximum 'big int' size: {Big.MaxSize} bits");
    }
    
    [Test, Ignore("not a feature!")]
    public void feeding_corrupted_data_will_fail()
    {
        Big.ResetSize();
        byte[] original = MakeData(4096);
        var source = new Fountain(original, 64);
        var target = new Bucket(original.Length, 64);

        int i;
        for (i = 0; i < 900; i++) // 900*64 -- generating 57600 bytes of transmit from 3072 bytes of source
        {
            if (target.IsComplete()) break;
            var bundle = source.Generate(i);
            
            if (i == 10) bundle[32] = 0x80;
            
            target.Push(i, bundle);
        }
        
        var recSize = 64*(i-1);
        Console.WriteLine($"Completed after {i} bundles: {recSize} bytes to transmit {original.Length}");
        
        Assert.That(target.IsComplete(), Is.True, "Target did not complete");
        
        var final = target.RecoverData();
        
        Assert.That(final.Length, Is.EqualTo(original.Length), "Data length is incorrect");

        for (i = 0; i < final.Length; i++)
        {
            Assert.That(final[i], Is.EqualTo(original[i]), $"Data corrupt at index {i}");
        }
        
        Console.WriteLine($"Maximum 'big int' size: {Big.MaxSize} bits");
    }

    [Test]
    public void recovering_data_easy()
    {
        Big.ResetSize();
        byte[] original = MakeData(4096);
        var source = new Fountain(original, 64);
        var target = new Bucket(original.Length, 64);

        int i;
        for (i = 0; i < 900; i++) // 900*64 -- generating 57600 bytes of transmit from 3072 bytes of source
        {
            if (target.IsComplete()) break;
            target.Push(i, source.Generate(i));
        }
        
        var recSize = 64*(i-1);
        Console.WriteLine($"Completed after {i} bundles: {recSize} bytes to transmit {original.Length}");
        
        Assert.That(target.IsComplete(), Is.True, "Target did not complete");
        
        var final = target.RecoverData();
        
        Assert.That(final.Length, Is.EqualTo(original.Length), "Data length is incorrect");

        for (i = 0; i < final.Length; i++)
        {
            Assert.That(final[i], Is.EqualTo(original[i]), $"Data corrupt at index {i}");
        }
        
        Console.WriteLine($"Maximum 'big int' size: {Big.MaxSize} bits");
    }

    [Test, Explicit("should work, but it's slow")]
    public void recovering_data_large()
    {
        // Only one mb, but the growth of the Recover function is rapid
        Big.ResetSize();
        byte[] original = MakeData(1024*1024);
        var bundleSize = original.Length / 100;
        bundleSize += bundleSize % 2;
        Console.WriteLine($"Bundle size = {bundleSize}");
        var source = new Fountain(original, bundleSize);
        var target = new Bucket(original.Length, bundleSize);

        var sw = new Stopwatch();
        
        sw.Start();
        int i;
        for (i = 0; i < 900; i++)
        {
            if (target.IsComplete()) break;
            target.Push(i, source.Generate(i));
        }
        sw.Stop();
        
        var recSize = bundleSize*(i-1);
        Console.WriteLine($"Completed after {i} bundles in {sw.Elapsed}: {recSize} bytes to transmit {original.Length}");
        
        Assert.That(target.IsComplete(), Is.True, "Target did not complete");
        
        sw.Restart();
        var final = target.RecoverData(); // n^2 ?
        sw.Stop();
        Console.WriteLine($"Data recovered in {sw.Elapsed}");
        
        Assert.That(final.Length, Is.EqualTo(original.Length), "Data length is incorrect");

        for (i = 0; i < final.Length; i++)
        {
            Assert.That(final[i], Is.EqualTo(original[i]), $"Data corrupt at index {i}");
        }
        
        Console.WriteLine($"Maximum 'big int' size: {Big.MaxSize} bits");
    }
    
    [Test]
    public void recovering_data_all_zeros()
    {
        Big.ResetSize();
        byte[] original = new byte[2048];
        var source = new Fountain(original, 64);
        var target = new Bucket(original.Length, 64);

        int i;
        for (i = 0; i < 900; i++)
        {
            if (target.IsComplete()) break;
            target.Push(i, source.Generate(i));
        }
        
        var recSize = 64*(i-1);
        Console.WriteLine($"Completed after {i} bundles: {recSize} bytes to transmit {original.Length}");
        
        Assert.That(target.IsComplete(), Is.True, "Target did not complete");
        
        var final = target.RecoverData();
        
        Assert.That(final.Length, Is.EqualTo(original.Length), "Data length is incorrect");

        for (i = 0; i < final.Length; i++)
        {
            Assert.That(final[i], Is.EqualTo(original[i]), $"Data corrupt at index {i}");
        }
        
        Console.WriteLine($"Maximum 'big int' size: {Big.MaxSize} bits");
    }
    
    [Test]
    public void recovering_data_all_ones()
    {
        Big.ResetSize();
        byte[] original = new byte[2048];
        for (int j = 0; j < original.Length; j++) { original[j] = 0xFF; }
        var source = new Fountain(original, 64);
        var target = new Bucket(original.Length, 64);

        int i;
        for (i = 0; i < 900; i++)
        {
            if (target.IsComplete()) break;
            target.Push(i, source.Generate(i));
        }
        
        var recSize = 64*(i-1);
        Console.WriteLine($"Completed after {i} bundles: {recSize} bytes to transmit {original.Length}");
        
        Assert.That(target.IsComplete(), Is.True, "Target did not complete");
        
        var final = target.RecoverData();
        
        Assert.That(final.Length, Is.EqualTo(original.Length), "Data length is incorrect");

        for (i = 0; i < final.Length; i++)
        {
            Assert.That(final[i], Is.EqualTo(original[i]), $"Data corrupt at index {i}");
        }
        
        Console.WriteLine($"Maximum 'big int' size: {Big.MaxSize} bits");
    }
    
    [Test]
    public void recovering_data_simple_loss()
    {
        Big.ResetSize();
        byte[] original = MakeData(4096);
        var source = new Fountain(original, 64);
        var target = new Bucket(original.Length, 64);

        int i;
        for (i = 0; i < 900; i++) // 900*64 -- generating 57600 bytes of transmit from 3072 bytes of source
        {
            if (target.IsComplete()) break;
            if (i % 5 == 0) continue;
            target.Push(i, source.Generate(i));
        }
        
        var recSize = 64*(i-1);
        Console.WriteLine($"Completed after {i} bundles: {recSize} bytes to transmit {original.Length}");
        
        Assert.That(target.IsComplete(), Is.True, "Target did not complete");
        
        var final = target.RecoverData();
        
        Assert.That(final.Length, Is.EqualTo(original.Length), "Data length is incorrect");

        for (i = 0; i < final.Length; i++)
        {
            Assert.That(final[i], Is.EqualTo(original[i]), $"Data corrupt at index {i}");
        }
        
        Console.WriteLine($"Maximum 'big int' size: {Big.MaxSize} bits");
    }
    
    [Test] // Note: when this fails, we are always expecting a zero, but get a non-zero value.
    public void recovering_data_simple_loss_and_large_bundles()
    {
        const int bundleSize = 256;
        Big.ResetSize();
        byte[] original = MakeData(4096);
        var source = new Fountain(original, bundleSize);
        var target = new Bucket(original.Length, bundleSize);

        int i;
        for (i = 0; i < 900; i++)
        {
            if (target.IsComplete()) break;
            if (i % 5 == 0) continue;
            target.Push(i, source.Generate(i));
        }
        
        var recSize = bundleSize*(i-1);
        Console.WriteLine($"Completed after {i} bundles: {recSize} bytes to transmit {original.Length}");
        
        Assert.That(target.IsComplete(), Is.True, "Target did not complete");
        
        var final = target.RecoverData();
        
        Assert.That(final.Length, Is.EqualTo(original.Length), "Data length is incorrect");

        for (i = 0; i < final.Length; i++)
        {
            Assert.That(final[i], Is.EqualTo(original[i]), $"Data corrupt at index {i} of {final.Length-1}; Original = {original[i]}");
        }
        
        Console.WriteLine($"Maximum 'big int' size: {Big.MaxSize} bits");
    }
    
    [Test]
    public void recovering_data_simple_loss_heavy()
    {
        Big.ResetSize();
        byte[] original = MakeData(4096);
        var source = new Fountain(original, 64);
        var target = new Bucket(original.Length, 64);

        int i;
        for (i = 0; i < 900; i++) // 900*64 -- generating 57600 bytes of transmit from 3072 bytes of source
        {
            if (target.IsComplete()) break;
            if (i % 5 == 0 || i % 7 == 0)
            {
                target.Push(i, source.Generate(i));
            }
        }
        
        var recSize = 64*(i-1);
        Console.WriteLine($"Completed after {i} bundles: {recSize} bytes to transmit {original.Length}");
        
        Assert.That(target.IsComplete(), Is.True, "Target did not complete");
        
        var final = target.RecoverData();
        
        Assert.That(final.Length, Is.EqualTo(original.Length), "Data length is incorrect");

        for (i = 0; i < final.Length; i++)
        {
            Assert.That(final[i], Is.EqualTo(original[i]), $"Data corrupt at index {i}");
        }
        
        Console.WriteLine($"Maximum 'big int' size: {Big.MaxSize} bits");
    }
    
    [Test]
    public void recovering_data_heavy_losses_and_small_bundles()
    {
        Big.ResetSize();
        byte[] original = MakeData(1600);
        var source = new Fountain(original, 16);
        var target = new Bucket(original.Length, 16);

        int i;
        for (i = 0; i < 900; i++) // 900*64 -- generating 57600 bytes of transmit from 3072 bytes of source
        {
            if (target.IsComplete()) break;
            if (i % 5 == 0 || i % 7 == 0)
            {
                target.Push(i, source.Generate(i));
            }
        }
        
        var recSize = 16*(i-1);
        Console.WriteLine($"Completed after {i} bundles: {recSize} bytes to transmit {original.Length}");
        
        Assert.That(target.IsComplete(), Is.True, "Target did not complete");
        
        var final = target.RecoverData();
        
        Assert.That(final.Length, Is.EqualTo(original.Length), "Data length is incorrect");

        for (i = 0; i < final.Length; i++)
        {
            Assert.That(final[i], Is.EqualTo(original[i]), $"Data corrupt at index {i}");
        }
        
        Console.WriteLine($"Maximum 'big int' size: {Big.MaxSize} bits");
    }

    private byte[] MakeData(int size)
    {
        var rnd = new Random();
        var data = new byte[size];
        rnd.NextBytes(data);
        return data;
    }
}