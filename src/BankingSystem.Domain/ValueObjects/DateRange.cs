namespace BankingSystem.Domain.ValueObjects;

public class DateRange : ValueObject
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date");

        StartDate = startDate;
        EndDate = endDate;
    }

    public bool Contains(DateTime date)
    {
        return date >= StartDate && date <= EndDate;
    }

    public int GetDayCount()
    {
        return (EndDate - StartDate).Days + 1;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not DateRange other)
            return false;

        return StartDate == other.StartDate && EndDate == other.EndDate;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StartDate, EndDate);
    }

    public override string ToString()
    {
        return $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}";
    }
}
