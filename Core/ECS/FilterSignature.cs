namespace Praxis.Core.ECS;

internal struct FilterSignature : IEquatable<FilterSignature>
{
    public readonly IndexableSet<uint> Included;
    public readonly IndexableSet<uint> Excluded;

    public FilterSignature(IndexableSet<uint> included, IndexableSet<uint> excluded)
    {
        Included = included;
        Excluded = excluded;
    }

    public override bool Equals(object? obj)
	{
		return obj is FilterSignature signature && Equals(signature);
	}

	public bool Equals(FilterSignature other)
	{
		foreach (var included in Included.AsSpan)
		{
			if (!other.Included.Contains(included))
			{
				return false;
			}
		}

		foreach (var excluded in Excluded.AsSpan)
		{
			if (!other.Excluded.Contains(excluded))
			{
				return false;
			}
		}

		return true;
	}

    public override int GetHashCode()
	{
		var hashcode = 1;

		foreach (var type in Included.AsSpan)
		{
			hashcode = HashCode.Combine(hashcode, type);
		}

		foreach (var type in Excluded.AsSpan)
		{
			hashcode = HashCode.Combine(hashcode, type);
		}

		return hashcode;
	}

	public static bool operator ==(FilterSignature left, FilterSignature right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(FilterSignature left, FilterSignature right)
	{
		return !(left == right);
	}
}
