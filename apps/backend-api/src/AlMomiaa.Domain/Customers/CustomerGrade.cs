namespace AlMomiaa.Domain.Customers;

public sealed class CustomerGrade
{
    private CustomerGrade() { }

    public int Id { get; private set; }
    public string GradeCode { get; private set; } = string.Empty;
    public int DefaultPaymentDays { get; private set; }
    public bool AllowCredit { get; private set; } = true;
    public DateTime UpdatedAtUtc { get; private set; }

    public static CustomerGrade Create(int id, string gradeCode, int defaultPaymentDays, bool allowCredit, DateTime updatedAtUtc) =>
        new()
        {
            Id = id,
            GradeCode = gradeCode,
            DefaultPaymentDays = defaultPaymentDays,
            AllowCredit = allowCredit,
            UpdatedAtUtc = updatedAtUtc
        };

    public void UpdatePolicy(int defaultPaymentDays, bool allowCredit, DateTime updatedAtUtc)
    {
        DefaultPaymentDays = defaultPaymentDays;
        AllowCredit = allowCredit;
        UpdatedAtUtc = updatedAtUtc;
    }
}
