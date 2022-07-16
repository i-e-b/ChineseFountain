#undef USE_MPZ

#if USE_MPZ
using System.Runtime.InteropServices;
using MathGmp.Native;
#endif

namespace ChineseFountain;

/// <summary>
/// Helper wrapper for mpz_t and the Math.Gmp.Native.NET package.
/// TODO: Replace this with a special purpose pure C# library once the tests are green
/// </summary>
public class Big : IBigInt
{
    public static int MaxSize { get; private set; } = 0;
    
    #if USE_MPZ
    private readonly mpz_t _val;
    #else
    private readonly BigInteger _val;
    #endif
    
    public Big()
    {
#if USE_MPZ
        _val = new mpz_t();
        gmp_lib.mpz_init(_val);
#else
        _val = BigInteger.ZERO;
#endif
    }
    
    public Big(int v)
    {
#if USE_MPZ
        _val = new mpz_t();
        gmp_lib.mpz_init_set_si(_val, v);
#else
        _val = BigInteger.valueOf(v);
#endif
    }

#if USE_MPZ
    public Big(mpz_t v) {
        _val = v;
        CheckSize();
    }
#else
    public Big(BigInteger v) {
        _val = v;
        CheckSize();
    }
#endif

    private void CheckSize()
    {
#if USE_MPZ
        var size = (int)gmp_lib.mpz_sizeinbase(_val, 2);
#else
        var size = _val.bitLength();
#endif
        if (size > MaxSize) MaxSize = size;
    }

    ~Big()
    {
#if USE_MPZ
        gmp_lib.mpz_clears(_val);
#endif
    }

#if USE_MPZ
    public static implicit operator mpz_t(Big b) => b._val;
    public static implicit operator Big(mpz_t z) => new(z);
    public static bool operator ==(Big x, Big y) => gmp_lib.mpz_cmp(x,y) == 0;
    public static bool operator !=(Big x, Big y) => gmp_lib.mpz_cmp(x, y) != 0;
#else
    public static implicit operator BigInteger(Big b) => b._val;
    public static implicit operator Big(BigInteger z) => new(z);
    public static bool operator ==(Big x, Big y) => x._val.compareTo(y._val) == 0;
    public static bool operator !=(Big x, Big y) => x._val.compareTo(y._val) != 0;
#endif


    public Big sub(int i)
    {
#if USE_MPZ
        var x = new Big(i);
        var r = new Big();
        gmp_lib.mpz_sub(r, _val, x);
        r.CheckSize();
        return r;
#else
        var x = new Big(i);
        return _val.subtract(x);
#endif
    }

    public Big invertm(Big b)
    {
#if USE_MPZ
        var r = new Big();
        gmp_lib.mpz_invert(r, _val, b);
        r.CheckSize();
        return r;
#else
        return _val.modInverse(b);
#endif
    }

    public Big gcd(Big cop)
    {
#if USE_MPZ
        var r = new Big();
        gmp_lib.mpz_gcd(r, _val, cop);
        r.CheckSize();
        return r;
#else
        return _val.gcd(cop);
#endif
    }
    

    public Big mul(Big y)
    {
#if USE_MPZ
        var r = new Big();
        gmp_lib.mpz_mul(r, _val, y);
        r.CheckSize();
        return r;
#else
        return _val.multiply(y);
#endif
    }

    public Big mod(Big d)
    {
#if USE_MPZ
        var r = new Big();
        gmp_lib.mpz_mod(r, _val, d);
        r.CheckSize();
        return r;
#else
        return _val.remainder(d);
#endif
    }

    public Big add(Big b)
    {
#if USE_MPZ
        var r = new Big();
        gmp_lib.mpz_add(r, _val, b);
        r.CheckSize();
        return r;
#else
        return _val.add(b);
#endif
    }

    public static Big pow(uint bse, uint exp)
    {
#if USE_MPZ
        var r = new Big();
        gmp_lib.mpz_ui_pow_ui(r, bse, exp);
        r.CheckSize();
        return r;
#else
        return BigInteger.valueOf(bse).pow((int)exp);
#endif
    }

    public bool gt(Big t1)
    {
#if USE_MPZ
        return gmp_lib.mpz_cmp(_val, t1) > 0;
#else
        return _val.compareTo(t1) > 0;
#endif
    }

    public static Big FromBuffer(byte[] hunk)
    {
#if USE_MPZ
        var r = new Big();
        
        var len = (size_t)hunk.Length;
        var oneByte = (size_t)1;
        void_ptr data = gmp_lib.allocate((size_t)hunk.Length);
        Marshal.Copy(hunk, 0, data.ToIntPtr(), hunk.Length);
        
        gmp_lib.mpz_import(r._val, len, 1, oneByte, -1, 0, data);
        
        gmp_lib.free(data);
        return r;
#else
        //return new Big(new BigInteger(hunk));
        return new Big(new BigInteger(1, hunk));
#endif
    }

    public byte[] ToBuffer()
    {
#if USE_MPZ
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
#else
        //return _val.toByteArray();
        return _val.magnitudeBytes();
#endif
    }

    public static void ResetSize() { MaxSize = 0; }
}