namespace ChineseFountain;

public interface IBigInt
{
    Big sub(int i);
    Big invertm(Big b);
    Big gcd(Big cop);
    Big mul(Big y);
    Big mod(Big d);
    Big add(Big b);
    bool gt(Big t1);
    byte[] ToBuffer();
}