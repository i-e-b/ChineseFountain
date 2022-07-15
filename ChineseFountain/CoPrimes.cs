namespace ChineseFountain;

public static class CoPrimes
{
    const int MAX_COPRIME_16 = 65535;
    
    // Cache of calculated values
    private static readonly List<Big> COPRIMES16 = new() { new Big(MAX_COPRIME_16) };
    private static Big _1 = new(1);

    public static List<Big> coprimes(int num, Big max) {
        var cop = max;
        var cops = new List<Big> { cop };
        
        while (cops.Count < num) {
            cop = cop.sub(1);
            var failed = false;
            for (var i = 0; i < cops.Count; i++) {
                var c = cops[i];
                if (c.gcd(cop) != _1) {
                    failed = true;
                    break;
                }
            }
            if (!failed) {
                cops.Add(cop);
            }
        }
        return cops;
    }
    
    public static Big coprime16(int num) {
        var cop = COPRIMES16[^1];
        while (num >= COPRIMES16.Count) {
            
            cop = cop.sub(1);
            if (cop == _1) throw new Exception("no more co-primes");
            
            var failed = false;
            for (var i = 0; i < COPRIMES16.Count; i++) {
                var c = COPRIMES16[i];
                if (c.gcd(cop) != _1) {
                    failed = true;
                    break;
                }
            }
            if (!failed) {
                COPRIMES16.Add(cop);
            }
        }
        return COPRIMES16[num];
    }

    // Returns a list of pre-multiplied coefficients and the base, when supplied with a list of cops.
    public static BaseAndCoefficients calc_coeffs(List<Big> subset_cops) {
        var _base = _1;
        for (var i = 0; i < subset_cops.Count; i++) {
            _base = _base.mul(subset_cops[i]);
        }

        var num_cops = subset_cops.Count;
        var coeffs = new Big[num_cops];
        var pre_mult_coeffs = new Big[num_cops];

        Big multiply_base(Big a, Big b) {
            var ret = a.mul(b).mod(_base); // a*b % _base
            return ret;
        }

        for (var i = 0; i < num_cops; i++) {
            var prod = _1;
            for (var j = 0; j < num_cops; j++) {
                if (j == i) continue;
                prod = multiply_base(prod, subset_cops[j]);
            }
            var prod_mod_cop = prod.mod(subset_cops[i]);
            
            
            var k = prod_mod_cop.invertm(subset_cops[i]);
            coeffs[i] = k;
            pre_mult_coeffs[i] = k.mul(prod);
        }

        return new BaseAndCoefficients (pre_mult_coeffs, _base);
    }
    
    public static List<Big> split(Big num, List<Big> cops) {
        var parts = new List<Big>();
        for (var i = 0; i < cops.Count; i++) {
            parts.Add(num.mod(cops[i]));
        }
        return parts;
    }
    
    // Returns the smallest number which has a given set of remainders when divided by the cops.
    public static Big combine(List<Big> parts, List<Big> subset_cops) {
        var ob = calc_coeffs(subset_cops);
        var coeffs = ob.Coeffs;//ob.pre_mult;
        var _base = ob.Base;//ob.base;

        if (parts.Count != subset_cops.Count) { throw new Exception("incorrect number of parts"); }
        var ret = new Big(0);
        var num_out = new Big(0);

        for (var i=0; i < subset_cops.Count; i++) {
            var tmp = coeffs[i].mul(parts[i]);
            var tmp2 = num_out.add(tmp);
            num_out = tmp2.mod(_base);
            ret = ret.add(coeffs[i].mul(parts[i])).mod(_base);
        }

        return ret;
    }
}

public class BaseAndCoefficients
{
    public Big[] Coeffs { get; }
    public Big Base { get; }

    public BaseAndCoefficients(Big[] coeffs, Big @base)
    {
        Coeffs = coeffs;
        Base = @base;
    }
}