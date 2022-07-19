namespace ChineseFountain.core;

public static class CoPrimes
{
    private const int MaxCoPrime16 = 65535;
    
    // Cache of calculated values
    private static readonly List<Big> _coPrimes16 = new() { new Big(MaxCoPrime16) };
    private static readonly Big _1 = new(1);

    public static Big CoPrime16(int num) {
        var cop = _coPrimes16[^1];
        while (num >= _coPrimes16.Count) {
            
            cop = cop.Sub(1);
            if (cop == _1) throw new Exception("no more co-primes");
            
            var failed = false;
            for (var i = 0; i < _coPrimes16.Count; i++) {
                var c = _coPrimes16[i];
                if (c.Gcd(cop) != _1) {
                    failed = true;
                    break;
                }
            }
            if (!failed) {
                _coPrimes16.Add(cop);
            }
        }
        return _coPrimes16[num];
    }

    /// <summary>
    /// Returns a list of pre-multiplied coefficients and the base, when supplied with a list of cops.
    /// </summary>
    public static BaseAndCoefficients CalculateCoefficients(List<Big> subsetCops) {
        var baseCoefficient = _1;
        for (var i = 0; i < subsetCops.Count; i++) {
            baseCoefficient = baseCoefficient.Mul(subsetCops[i]);
        }

        var numCops = subsetCops.Count;
        var coefficients = new Big[numCops];
        var preMultCoefficients = new Big[numCops];

        Big MultiplyBase(Big a, Big b) {
            var ret = a.Mul(b).Mod(baseCoefficient); // a*b % _base
            return ret;
        }

        // matrix solving
        for (var i = 0; i < numCops; i++) {
            var prod = _1;
            for (var j = 0; j < numCops; j++) {
                if (j == i) continue;
                prod = MultiplyBase(prod, subsetCops[j]);
            }
            var prodModCop = prod.Mod(subsetCops[i]);
            
            
            var k = prodModCop.ModInverse(subsetCops[i]);
            coefficients[i] = k;
            preMultCoefficients[i] = k.Mul(prod);
        }

        return new BaseAndCoefficients (preMultCoefficients, baseCoefficient);
    }
    
    /// <summary>
    /// Returns the smallest number which has a given set of remainders when divided by the cops.
    /// </summary>
    /// <remarks>
    /// This seems to be the main bottleneck of the data recovery.
    /// Most time is spent in `Mod`
    /// </remarks>
    public static Big Combine(List<Big> parts, List<Big> subsetCops, int limit) {
        var ob = CalculateCoefficients(subsetCops);
        var coefficients = ob.Coefficients;
        var baseCoefficient = ob.Base;

        if (parts.Count < limit || subsetCops.Count < limit) { throw new Exception("incorrect number of parts"); }
        var ret = new Big(0);

        for (var i = 0; i < limit; i++) {
            ret = ret.Add(coefficients[i].Mul(parts[i])).Mod(baseCoefficient);
        }

        return ret;
    }
    
    public static Big Combine(List<Big> parts, BaseAndCoefficients subsetCopsBc, int subsetCopsCount) {
        var coefficients = subsetCopsBc.Coefficients;
        var baseCoefficient = subsetCopsBc.Base;

        if (parts.Count != subsetCopsCount) { throw new Exception("incorrect number of parts"); }
        var ret = new Big(0);

        for (var i=0; i < subsetCopsCount; i++) {
            ret = ret.Add(coefficients[i].Mul(parts[i])).Mod(baseCoefficient);
        }

        return ret;
    }
}

public class BaseAndCoefficients
{
    public Big[] Coefficients { get; }
    public Big Base { get; }

    public BaseAndCoefficients(Big[] coefficients, Big @base)
    {
        Coefficients = coefficients;
        Base = @base;
    }
}