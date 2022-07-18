namespace ChineseFountain;

/// <summary>
/// Helper wrapper for the BigInteger library
/// </summary>
public class Big
{
    private bool Equals(Big other)
    {
        return _val.Equals(other._val);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Big)obj);
    }

    public override int GetHashCode()
    {
        return _val.GetHashCode();
    }

    public static int MaxSize { get; private set; }
    
    private readonly BigInteger _val;
    
    public Big(int v)
    {
        _val = BigInteger.ValueOf(v);
    }

    public Big(BigInteger v) {
        _val = v;
        CheckSize();
    }

    private void CheckSize()
    {
        var size = _val.BitLength();
        if (size > MaxSize) MaxSize = size;
    }

    public static implicit operator BigInteger(Big b) => b._val;
    public static implicit operator Big(BigInteger z) => new(z);
    public static bool operator ==(Big x, Big y) => x._val.CompareTo(y._val) == 0;
    public static bool operator !=(Big x, Big y) => x._val.CompareTo(y._val) != 0;

    public Big Sub(int i)
    {
        var x = new Big(i);
        return _val.Subtract(x);
    }

    public Big ModInverse(Big b)
    {
        return _val.ModInverse(b);
    }

    public Big Gcd(Big cop)
    {
        return _val.Gcd(cop);
    }
    

    public Big Mul(Big y)
    {
        return _val.Multiply(y);
    }

    // A huge amount of the recovery time is spent here
    public Big Mod(Big d)
    {
        return _val.Mod(d);
    }

    public Big Add(Big b)
    {
        return _val.Add(b);
    }

    public static Big Pow(uint bse, uint exp)
    {
        return BigInteger.ValueOf(bse).Pow((int)exp);
    }

    public bool GT(Big t1)
    {
        return _val.CompareTo(t1) > 0;
    }

    public static Big FromBuffer(byte[] hunk)
    {
        return new Big(new BigInteger(1, hunk));
    }

    public byte[] ToBuffer()
    {
        return _val.MagnitudeBytes();
    }

    public static void ResetSize() { MaxSize = 0; }
}