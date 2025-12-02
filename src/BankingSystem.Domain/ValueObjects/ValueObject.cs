namespace BankingSystem.Domain.ValueObjects;

public abstract class ValueObject
{
    protected static bool EqualOperator(ValueObject? left, ValueObject? right)
    {
        if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
            return false;

        return ReferenceEquals(left, right) || left!.Equals(right);
    }

    protected static bool NotEqualOperator(ValueObject? left, ValueObject? right)
    {
        return !EqualOperator(left, right);
    }

    public override abstract bool Equals(object? obj);
    public abstract override int GetHashCode();
}
