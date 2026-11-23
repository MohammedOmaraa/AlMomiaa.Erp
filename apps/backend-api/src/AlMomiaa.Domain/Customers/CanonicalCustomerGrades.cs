namespace AlMomiaa.Domain.Customers;

public static class CanonicalCustomerGrades
{
    public static readonly DateTime SeedTimestampUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly IReadOnlyList<CustomerGrade> All =
    [
        CustomerGrade.Create(1, "A", 60, true, SeedTimestampUtc),
        CustomerGrade.Create(2, "B", 45, true, SeedTimestampUtc),
        CustomerGrade.Create(3, "C", 30, true, SeedTimestampUtc),
        CustomerGrade.Create(4, "D", 0, false, SeedTimestampUtc)
    ];
}
