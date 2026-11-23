using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace AlMomiaa.Application.Finance;

public sealed record FinancialAccountDto(Guid Id, string AccountCode, string NameAr, string? NameEn, string AccountType, Guid? OwnerRepId, string? BankName, string? Iban, decimal CurrentBalance, bool IsActive);
public sealed record FiscalPeriodDto(int Id, int FiscalYear, int FiscalMonth, DateTime PeriodStartUtc, DateTime PeriodEndUtc, bool IsClosed, Guid? ClosedByUserId, DateTime? ClosedAtUtc);
public sealed record CollectionAllocationDto(Guid Id, Guid InvoiceId, decimal AllocatedAmount);
public sealed record CollectionDto(Guid Id, string ReceiptNumber, Guid CustomerId, Guid RepId, string PaymentMethod, decimal Amount, string Status, bool IsCustomerConfirmed, DateTime? CustomerConfirmedAtUtc, string? PaymentProofImageUrl, string? PaymentProofImageHash, string? BankReferenceNumber, Guid? TargetFinancialAccountId, Guid? VerifiedByAccountantId, DateTime? VerifiedAtUtc, DateTime OriginalValueDateUtc, DateTime CreatedAtUtc, IReadOnlyList<CollectionAllocationDto> Allocations);
public sealed record JournalLineDto(long Id, Guid FinancialAccountId, Guid? CustomerId, Guid? SupplierId, decimal DebitAmount, decimal CreditAmount);
public sealed record JournalEntryDto(Guid Id, string EntryNumber, int FiscalPeriodId, DateTime PostingDateUtc, DateTime OriginalValueDateUtc, string ReferenceType, Guid ReferenceId, string DescriptionAr, Guid CreatedByUserId, IReadOnlyList<JournalLineDto> Lines);

public sealed record GetFinancialAccountsQuery : IRequest<IReadOnlyList<FinancialAccountDto>>;
public sealed record CreateFinancialAccountRequest(string AccountCode, string NameAr, string? NameEn, string AccountType, Guid? OwnerRepId, string? BankName, string? Iban);
public sealed record CreateFinancialAccountCommand(CreateFinancialAccountRequest Request) : IRequest<FinancialAccountDto>;

public sealed record GetFiscalPeriodsQuery : IRequest<IReadOnlyList<FiscalPeriodDto>>;
public sealed record CreateFiscalPeriodRequest(int FiscalYear, int FiscalMonth);
public sealed record CreateFiscalPeriodCommand(CreateFiscalPeriodRequest Request) : IRequest<FiscalPeriodDto>;
public sealed record CloseFiscalPeriodCommand(int FiscalPeriodId) : IRequest<FiscalPeriodDto>;

public sealed record CollectionAllocationRequest(Guid InvoiceId, decimal AllocatedAmount);
public sealed record RecordCashCollectionRequest(Guid CustomerId, decimal Amount, CollectionAllocationRequest[]? Allocations);
public sealed record RecordCashCollectionCommand(RecordCashCollectionRequest Request, Guid IdempotencyKey) : IRequest<CollectionDto>;

public sealed record ConfirmCashCollectionCommand(Guid CollectionId) : IRequest<CollectionDto>;

public sealed record RecordTreasuryHandoverRequest(Guid CollectionId, Guid TargetTreasuryAccountId);
public sealed record RecordTreasuryHandoverCommand(RecordTreasuryHandoverRequest Request) : IRequest<CollectionDto>;

public sealed record RecordBankTransferRequest(
    Guid CustomerId,
    decimal Amount,
    string BankReferenceNumber,
    string PaymentProofImageUrl,
    string PaymentProofImageHash,
    DateTime? OriginalValueDateUtc,
    CollectionAllocationRequest[]? Allocations);
public sealed record RecordBankTransferCommand(RecordBankTransferRequest Request, Guid IdempotencyKey) : IRequest<CollectionDto>;

public sealed record VerifyBankTransferRequest(Guid TargetBankAccountId, bool Approve);
public sealed record VerifyBankTransferCommand(Guid CollectionId, VerifyBankTransferRequest Request) : IRequest<CollectionDto>;

public sealed record GetJournalEntriesQuery(int Page = 1, int PageSize = 50) : IRequest<PagedResult<JournalEntryDto>>;

public sealed class CreateFinancialAccountCommandValidator : AbstractValidator<CreateFinancialAccountCommand>
{
    private static readonly string[] ValidTypes =
    [
        "MainTreasurySafe", "BankAccount", "RepCustodyWallet",
        "AccountsReceivable", "AccountsPayable", "SalesRevenue",
        "VatOutput", "InventoryAsset", "COGS"
    ];

    public CreateFinancialAccountCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.AccountCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.NameEn).MaximumLength(150);
        RuleFor(x => x.Request.AccountType)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithMessage("AccountType must be a valid canonical ERP financial account type.");
        RuleFor(x => x.Request.BankName).MaximumLength(100);
        RuleFor(x => x.Request.Iban).MaximumLength(50);
    }
}

public sealed class CreateFiscalPeriodCommandValidator : AbstractValidator<CreateFiscalPeriodCommand>
{
    public CreateFiscalPeriodCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.FiscalYear).InclusiveBetween(2020, 2100);
        RuleFor(x => x.Request.FiscalMonth).InclusiveBetween(1, 12);
    }
}

public sealed class RecordCashCollectionCommandValidator : AbstractValidator<RecordCashCollectionCommand>
{
    public RecordCashCollectionCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("X-Idempotency-Key header with a valid GUID is required.");
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.CustomerId).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThan(0m);
        RuleForEach(x => x.Request.Allocations).ChildRules(a =>
        {
            a.RuleFor(i => i.InvoiceId).NotEmpty();
            a.RuleFor(i => i.AllocatedAmount).GreaterThan(0m);
        }).When(x => x.Request.Allocations is not null);
        RuleFor(x => x.Request)
            .Must(r => r.Allocations is null || r.Allocations.Sum(a => a.AllocatedAmount) <= r.Amount)
            .WithMessage("Total allocated amount cannot exceed collection amount.");
    }
}

public sealed class RecordTreasuryHandoverCommandValidator : AbstractValidator<RecordTreasuryHandoverCommand>
{
    public RecordTreasuryHandoverCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.CollectionId).NotEmpty();
        RuleFor(x => x.Request.TargetTreasuryAccountId).NotEmpty();
    }
}

public sealed class RecordBankTransferCommandValidator : AbstractValidator<RecordBankTransferCommand>
{
    public RecordBankTransferCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("X-Idempotency-Key header with a valid GUID is required.");
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.CustomerId).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThan(0m);
        RuleFor(x => x.Request.BankReferenceNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.PaymentProofImageUrl).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.PaymentProofImageHash)
            .NotEmpty()
            .Matches("^[a-fA-F0-9]{64}$")
            .WithMessage("PaymentProofImageHash must be a valid 64-character SHA-256 hex digest.");
        RuleForEach(x => x.Request.Allocations).ChildRules(a =>
        {
            a.RuleFor(i => i.InvoiceId).NotEmpty();
            a.RuleFor(i => i.AllocatedAmount).GreaterThan(0m);
        }).When(x => x.Request.Allocations is not null);
        RuleFor(x => x.Request)
            .Must(r => r.Allocations is null || r.Allocations.Sum(a => a.AllocatedAmount) <= r.Amount)
            .WithMessage("Total allocated amount cannot exceed collection amount.");
    }
}

public sealed class VerifyBankTransferCommandValidator : AbstractValidator<VerifyBankTransferCommand>
{
    public VerifyBankTransferCommandValidator()
    {
        RuleFor(x => x.CollectionId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.TargetBankAccountId).NotEmpty().When(x => x.Request.Approve);
    }
}

public sealed class GetFinancialAccountsHandler(IFinanceModuleService service) : IRequestHandler<GetFinancialAccountsQuery, IReadOnlyList<FinancialAccountDto>>
{
    public Task<IReadOnlyList<FinancialAccountDto>> Handle(GetFinancialAccountsQuery request, CancellationToken ct) => service.GetFinancialAccountsAsync(ct);
}
public sealed class CreateFinancialAccountHandler(IFinanceModuleService service) : IRequestHandler<CreateFinancialAccountCommand, FinancialAccountDto>
{
    public Task<FinancialAccountDto> Handle(CreateFinancialAccountCommand request, CancellationToken ct) => service.CreateFinancialAccountAsync(request, ct);
}
public sealed class GetFiscalPeriodsHandler(IFinanceModuleService service) : IRequestHandler<GetFiscalPeriodsQuery, IReadOnlyList<FiscalPeriodDto>>
{
    public Task<IReadOnlyList<FiscalPeriodDto>> Handle(GetFiscalPeriodsQuery request, CancellationToken ct) => service.GetFiscalPeriodsAsync(ct);
}
public sealed class CreateFiscalPeriodHandler(IFinanceModuleService service) : IRequestHandler<CreateFiscalPeriodCommand, FiscalPeriodDto>
{
    public Task<FiscalPeriodDto> Handle(CreateFiscalPeriodCommand request, CancellationToken ct) => service.CreateFiscalPeriodAsync(request, ct);
}
public sealed class CloseFiscalPeriodHandler(IFinanceModuleService service) : IRequestHandler<CloseFiscalPeriodCommand, FiscalPeriodDto>
{
    public Task<FiscalPeriodDto> Handle(CloseFiscalPeriodCommand request, CancellationToken ct) => service.CloseFiscalPeriodAsync(request, ct);
}
public sealed class RecordCashCollectionHandler(IFinanceModuleService service) : IRequestHandler<RecordCashCollectionCommand, CollectionDto>
{
    public Task<CollectionDto> Handle(RecordCashCollectionCommand request, CancellationToken ct) => service.RecordCashCollectionAsync(request, ct);
}
public sealed class ConfirmCashCollectionHandler(IFinanceModuleService service) : IRequestHandler<ConfirmCashCollectionCommand, CollectionDto>
{
    public Task<CollectionDto> Handle(ConfirmCashCollectionCommand request, CancellationToken ct) => service.ConfirmCashCollectionAsync(request, ct);
}
public sealed class RecordTreasuryHandoverHandler(IFinanceModuleService service) : IRequestHandler<RecordTreasuryHandoverCommand, CollectionDto>
{
    public Task<CollectionDto> Handle(RecordTreasuryHandoverCommand request, CancellationToken ct) => service.RecordTreasuryHandoverAsync(request, ct);
}
public sealed class RecordBankTransferHandler(IFinanceModuleService service) : IRequestHandler<RecordBankTransferCommand, CollectionDto>
{
    public Task<CollectionDto> Handle(RecordBankTransferCommand request, CancellationToken ct) => service.RecordBankTransferAsync(request, ct);
}
public sealed class VerifyBankTransferHandler(IFinanceModuleService service) : IRequestHandler<VerifyBankTransferCommand, CollectionDto>
{
    public Task<CollectionDto> Handle(VerifyBankTransferCommand request, CancellationToken ct) => service.VerifyBankTransferAsync(request, ct);
}
public sealed class GetJournalEntriesHandler(IFinanceModuleService service) : IRequestHandler<GetJournalEntriesQuery, PagedResult<JournalEntryDto>>
{
    public Task<PagedResult<JournalEntryDto>> Handle(GetJournalEntriesQuery request, CancellationToken ct) => service.GetJournalEntriesAsync(request, ct);
}
