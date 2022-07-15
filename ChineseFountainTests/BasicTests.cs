using ChineseFountain;
using NUnit.Framework;

namespace ChineseFountainTests;

[TestFixture]
public class BasicTests
{
    [Test]
    public void first_run()
    {
        byte[] randomData = MakeData();
        var subject = new Fountain(randomData, 64);

        for (int i = 0; i < 900; i++) // 900*64 -- generating 57600 bytes of transmit from 3072 bytes of source
        {
            var str = Convert.ToBase64String(subject.Generate(i));
            //Console.WriteLine(str);
        }
        
        Console.WriteLine($"Maximum 'big int' size: {Big.MaxSize} bits");
    }

    [Test]
    public void recovering_data_easy()
    {
        byte[] original = MakeData();
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
    
    [Test]
    public void recovering_data_simple_loss()
    {
        byte[] original = MakeData();
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
    
    [Test]
    public void recovering_data_simple_loss_heavy()
    {
        byte[] original = MakeData();
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

    private byte[] MakeData()
    {
        var rnd = new Random();
        var data = new byte[3072];
        rnd.NextBytes(data);
        return data;
    }
}