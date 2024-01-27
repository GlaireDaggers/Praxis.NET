namespace Praxis.Core.ECS;

public ref struct SpanEnumerator<T>
{
    private Span<T> _span;
    private int _current;

    public SpanEnumerator<T> GetEnumerator() => this;

    public T Current => _span[_current];

    internal SpanEnumerator(Span<T> span)
    {
        _span = span;
        _current = -1;
    }

    public bool MoveNext()
    {
        if (_current < _span.Length - 1)
        {
            _current++;
            return true;
        }

        return false;
    }
}
