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
            var str = Convert.ToBase64String(subject.ReadBundle(i));
            Console.WriteLine(str);
        }
    }

    private byte[] MakeData()
    {
        var rnd = new Random();
        var data = new byte[3072];
        rnd.NextBytes(data);
        return data;
    }
}