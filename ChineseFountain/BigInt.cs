using System.Runtime.InteropServices;
using MathGmp.Native;

namespace ChineseFountain;

/// <summary>
/// Helper wrapper for mpz_t and the Math.Gmp.Native.NET package.
/// TODO: Replace this with a special purpose pure C# library once the tests are green
/// </summary>
public class Big
{
    private readonly mpz_t _val;
    
    public Big()
    {
        _val = new mpz_t();
        gmp_lib.mpz_init(_val);
    }
    
    public Big(int v)
    {
        _val = new mpz_t();
        gmp_lib.mpz_init_set_si(_val, v);
    }

    public Big(mpz_t v) { _val = v; }

    ~Big()
    {
        gmp_lib.mpz_clears(_val);
    }

    public static implicit operator mpz_t(Big b) => b._val;
    public static implicit operator Big(mpz_t z) => new(z);
    public static bool operator ==(Big x, Big y) => gmp_lib.mpz_cmp(x,y) == 0;
    public static bool operator !=(Big x, Big y) => gmp_lib.mpz_cmp(x, y) != 0;


    public Big sub(int i)
    {
        var x = new Big(i);
        var r = new Big();
        gmp_lib.mpz_sub(r, _val, x);
        return r;
    }

    public Big invertm(Big b)
    {
        var r = new Big();
        gmp_lib.mpz_invert(r, _val, b);
        return r;
    }

    public Big gcd(Big cop)
    {
        var r = new Big();
        gmp_lib.mpz_gcd(r, _val, cop);
        return r;
    }
    

    public Big mul(Big y)
    {
        var r = new Big();
        gmp_lib.mpz_mul(r, _val, y);
        return r;
    }

    public Big mod(Big d)
    {
        var r = new Big();
        gmp_lib.mpz_mod(r, _val, d);
        return r;
    }

    public Big add(Big b)
    {
        var r = new Big();
        gmp_lib.mpz_add(r, _val, b);
        return r;
    }

    public static Big FromBuffer(byte[] hunk)
    {
        var r = new Big();
        
        var len = (size_t)hunk.Length;
        var oneByte = (size_t)1;
        void_ptr data = gmp_lib.allocate((size_t)hunk.Length);
        Marshal.Copy(hunk, 0, data.ToIntPtr(), hunk.Length);
        
        gmp_lib.mpz_import(r._val, len, 1, oneByte, -1, 0, data);
        
        gmp_lib.free(data);
        return r;
    }

    public byte[] ToBuffer()
    {
        var expectedSize = gmp_lib.mpz_sizeinbase(_val, 2);
        var allocSize = (size_t)((long)expectedSize * 2);
        
        var data = gmp_lib.allocate(allocSize);
        
        var size = (size_t)0;
        var oneByte = (size_t)1;
        gmp_lib.mpz_export(data, ref size, 1, oneByte, -1, 0, _val);
        
        var realSize = (int)size;
        var result = new byte[realSize];
        Marshal.Copy(data.ToIntPtr(), result, 0, realSize);
        
        gmp_lib.free(data);
        return result;
    }
}