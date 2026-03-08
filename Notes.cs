namespace PeacefulSudoku;

public struct Notes
{
    private int _bits; // 9 bits, bit N = number N+1 is pencilled in

    public void Toggle(int n) => _bits ^= 1 << (n - 1);
    public bool Has(int n)    => (_bits & 1 << (n - 1)) != 0;
    public void Clear()       => _bits = 0;
    public bool IsEmpty       => _bits == 0;
}