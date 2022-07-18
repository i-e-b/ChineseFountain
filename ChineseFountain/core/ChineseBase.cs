namespace ChineseFountain.core;

/// <summary>
/// Simple helper classes
/// </summary>
public class ChineseBase
{
    protected const int SizeOfShort = 2;
    
    protected static void Assert(bool p0, Func<string> msg) { if (!p0) throw new Exception("assertion failed: "+msg()); }
    
    protected static int div_round_up(int num, int divisor) {
        if (num < 0) throw new Exception("num < 0");
        if (divisor < 1) throw new Exception("divisor < 1");
        return 0 | ((num - 1 + divisor) / divisor);
    }
}