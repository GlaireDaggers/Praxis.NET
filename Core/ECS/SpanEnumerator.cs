namespace Praxis.Core.ECS;

public ref struct ReverseSpanEnumerator<T>
{
    private Span<T> _span;
    private int _current;

    public ReverseSpanEnumerator<T> GetEnumerator() => this;

    public T Current => _span[_current];

    internal ReverseSpanEnumerator(Span<T> span)
    {
        _span = span;
        _current = _span.Length;
    }

    public bool MoveNext()
    {
        if (_current > 0)
        {
            _current--;
            return true;
        }

        return false;
    }
}
