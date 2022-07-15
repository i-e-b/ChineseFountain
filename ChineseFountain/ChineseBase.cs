namespace ChineseFountain;

/// <summary>
/// Simple helper classes
/// </summary>
public class ChineseBase
{
    protected const int SIZE_OF_SHORT = 2;
    
    protected static void assert(bool p0) { if (!p0) throw new Exception("assertion failed"); }
    
    protected static int div_round_up(int num, int divisor) {
        if (num < 0) throw new Exception("num < 0");
        if (divisor < 1) throw new Exception("divisor < 1");
        return 0 | ((num - 1 + divisor) / divisor);
    }
}