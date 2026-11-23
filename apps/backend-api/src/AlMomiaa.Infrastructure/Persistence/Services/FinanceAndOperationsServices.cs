using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Finance;
using AlMomiaa.Application.Operations;
using AlMomiaa.Domain.Common;
using AlMomiaa.Domain.Finance;
using AlMomiaa.Domain.SystemOps;
using Microsoft.EntityFrameworkCore;

namespace AlMomiaa.Infrastructure.Persistence.Services;

public sealed class FinanceModuleService(
    AlMomiaaDbContext db,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : IFinanceModuleService
{
    public async Task<IReadOnlyList<FinancialAccountDto>> GetFinancialAccountsAsync(CancellationToken ct)
    {
        return await db.FinancialAccounts.AsNoTracking()
            .OrderBy(a => a.AccountCode)
            .Select(a => new FinancialAccountDto(a.Id, a.AccountCode, a.NameAr, a.NameEn, a.AccountType, a.OwnerRepId, a.BankName, a.Iban, a.CurrentBalance, a.IsActive))
            .ToListAsync(ct);
    }

    public async Task<FinancialAccountDto> CreateFinancialAccountAsync(CreateFinancialAccountCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var code = req.AccountCode.Trim().ToUpperInvariant();
        if (await db.FinancialAccounts.AnyAsync(a => a.AccountCode == code, ct))
        {
            throw new ConflictException($"Financial account code '{code}' already exists.");
        }

        if (req.OwnerRepId.HasValue && !await db.Users.AnyAsync(u => u.Id == req.OwnerRepId.Value, ct))
        {
            throw new NotFoundException("User", req.OwnerRepId.Value);
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var account = FinancialAccount.Create(code, req.NameAr, req.NameEn, req.AccountType, req.OwnerRepId, req.BankName, req.Iban);
        db.FinancialAccounts.Add(account);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CreateFinancialAccount", "FinancialAccount", account.Id.ToString(), null, new { account.AccountCode, account.AccountType }, utcNow);
        await db.SaveChangesAsync(ct);

        return new FinancialAccountDto(account.Id, account.AccountCode, account.NameAr, account.NameEn, account.AccountType, account.OwnerRepId, account.BankName, account.Iban, account.CurrentBalance, account.IsActive);
    }

    public async Task<IReadOnlyList<FiscalPeriodDto>> GetFiscalPeriodsAsync(CancellationToken ct)
    {
        return await db.FiscalPeriods.AsNoTracking()
            .OrderByDescending(p => p.FiscalYear)
            .ThenByDescending(p => p.FiscalMonth)
            .Select(p => new FiscalPeriodDto(p.Id, p.FiscalYear, p.FiscalMonth, p.PeriodStartUtc, p.PeriodEndUtc, p.IsClosed, p.ClosedByUserId, p.ClosedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<FiscalPeriodDto> CreateFiscalPeriodAsync(CreateFiscalPeriodCommand command, CancellationToken ct)
    {
        var req = command.Request;
        if (await db.FiscalPeriods.AnyAsync(p => p.FiscalYear == req.FiscalYear && p.FiscalMonth == req.FiscalMonth, ct))
        {
            throw new ConflictException($"Fiscal period {req.FiscalYear}-{req.FiscalMonth:00} already exists.");
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var period = FiscalPeriod.Create(req.FiscalYear, req.FiscalMonth);
        db.FiscalPeriods.Add(period);
        await db.SaveChangesAsync(ct);

        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CreateFiscalPeriod", "FiscalPeriod", period.Id.ToString(), null, new { period.FiscalYear, period.FiscalMonth }, utcNow);
        await db.SaveChangesAsync(ct);

        return new FiscalPeriodDto(period.Id, period.FiscalYear, period.FiscalMonth, period.PeriodStartUtc, period.PeriodEndUtc, period.IsClosed, period.ClosedByUserId, period.ClosedAtUtc);
    }

    public async Task<FiscalPeriodDto> CloseFiscalPeriodAsync(CloseFiscalPeriodCommand command, CancellationToken ct)
    {
        var period = await db.FiscalPeriods.FirstOrDefaultAsync(p => p.Id == command.FiscalPeriodId, ct)
            ?? throw new NotFoundException("FiscalPeriod", command.FiscalPeriodId);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        period.Close(currentUser.UserId, utcNow);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CloseFiscalPeriod", "FiscalPeriod", period.Id.ToString(), null, new { period.FiscalYear, period.FiscalMonth, period.IsClosed }, utcNow);
        await db.SaveChangesAsync(ct);

        return new FiscalPeriodDto(period.Id, period.FiscalYear, period.FiscalMonth, period.PeriodStartUtc, period.PeriodEndUtc, period.IsClosed, period.ClosedByUserId, period.ClosedAtUtc);
    }

    public async Task<CollectionDto> RecordCashCollectionAsync(RecordCashCollectionCommand command, CancellationToken ct)
    {
        var existing = await db.Collections.AsNoTracking()
            .Include(c => c.Allocations)
            .FirstOrDefaultAsync(c => c.IdempotencyKey == command.IdempotencyKey, ct);
        if (existing is not null)
        {
            return MapCollection(existing);
        }

        var req = command.Request;
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == req.CustomerId, ct)
            ?? throw new NotFoundException("Customer", req.CustomerId);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var fiscalPeriod = await ResolveOpenFiscalPeriodAsync(utcNow, ct);
        var repWallet = await EnsureSystemAccountAsync("RepCustodyWallet", currentUser.UserId, ct);
        var arAccount = await EnsureSystemAccountAsync("AccountsReceivable", null, ct);

        var receiptNumber = $"RCV-CASH-{utcNow:yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[^8..].ToUpperInvariant()}";
        var collection = Collection.CreateCash(receiptNumber, customer.Id, currentUser.UserId, req.Amount, repWallet.Id, command.IdempotencyKey, utcNow);
        db.Collections.Add(collection);

        await ApplyAllocationsAsync(collection, customer.Id, req.Allocations, applyToInvoicesNow: true, ct);

        repWallet.AdjustBalance(collection.Amount);
        arAccount.AdjustBalance(-collection.Amount);
        customer.Credit(collection.Amount);

        var entryNumber = $"JE-{utcNow:yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[^8..].ToUpperInvariant()}";
        var journal = JournalEntry.PostBalanced(
            entryNumber,
            fiscalPeriod,
            utcNow,
            collection.OriginalValueDateUtc,
            "Collection",
            collection.Id,
            $"تحصيل نقدي في عهدة المندوب - سند {collection.ReceiptNumber}",
            currentUser.UserId,
            [
                (repWallet.Id, null, null, collection.Amount, 0m),
                (arAccount.Id, customer.Id, null, 0m, collection.Amount)
            ]);
        db.JournalEntries.Add(journal);

        AuditAndOutboxHelper.AppendAudit(db, currentUser, "RecordCashCollection", "Collection", collection.Id.ToString(), null, new { collection.ReceiptNumber, collection.Amount, collection.Status }, utcNow);
        await db.SaveChangesAsync(ct);

        return MapCollection(collection);
    }

    public async Task<CollectionDto> ConfirmCashCollectionAsync(ConfirmCashCollectionCommand command, CancellationToken ct)
    {
        var collection = await db.Collections
            .Include(c => c.Allocations)
            .FirstOrDefaultAsync(c => c.Id == command.CollectionId, ct)
            ?? throw new NotFoundException("Collection", command.CollectionId);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        collection.ConfirmByCustomer(utcNow);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "ConfirmCashCollection", "Collection", collection.Id.ToString(), null, new { collection.IsCustomerConfirmed, collection.CustomerConfirmedAtUtc }, utcNow);
        await db.SaveChangesAsync(ct);

        return MapCollection(collection);
    }

    public async Task<CollectionDto> RecordTreasuryHandoverAsync(RecordTreasuryHandoverCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var collection = await db.Collections
            .Include(c => c.Allocations)
            .FirstOrDefaultAsync(c => c.Id == req.CollectionId, ct)
            ?? throw new NotFoundException("Collection", req.CollectionId);

        var treasury = await db.FinancialAccounts.FirstOrDefaultAsync(a => a.Id == req.TargetTreasuryAccountId, ct)
            ?? throw new NotFoundException("FinancialAccount", req.TargetTreasuryAccountId);

        if (treasury.AccountType != "MainTreasurySafe")
        {
            throw new BusinessRuleException("Target account for treasury handover must have AccountType 'MainTreasurySafe'.");
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var fiscalPeriod = await ResolveOpenFiscalPeriodAsync(utcNow, ct);
        var repWallet = collection.TargetFinancialAccountId.HasValue
            ? await db.FinancialAccounts.FirstOrDefaultAsync(a => a.Id == collection.TargetFinancialAccountId.Value, ct)
            : null;
        repWallet ??= await EnsureSystemAccountAsync("RepCustodyWallet", collection.RepId, ct);

        collection.DepositToTreasury(treasury.Id, currentUser.UserId, utcNow);
        treasury.AdjustBalance(collection.Amount);
        repWallet.AdjustBalance(-collection.Amount);

        var entryNumber = $"JE-{utcNow:yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[^8..].ToUpperInvariant()}";
        var journal = JournalEntry.PostBalanced(
            entryNumber,
            fiscalPeriod,
            utcNow,
            collection.OriginalValueDateUtc,
            "Handover",
            collection.Id,
            $"توريد عهدة مندوب إلى الخزينة الرئيسية - سند {collection.ReceiptNumber}",
            currentUser.UserId,
            [
                (treasury.Id, null, null, collection.Amount, 0m),
                (repWallet.Id, null, null, 0m, collection.Amount)
            ]);
        db.JournalEntries.Add(journal);

        AuditAndOutboxHelper.EnqueueOutbox(db, "FinancialKpiUpdated", new { Event = "TreasuryHandover", collection.Id, collection.Amount, TreasuryAccountId = treasury.Id }, utcNow);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "RecordTreasuryHandover", "Collection", collection.Id.ToString(), null, new { collection.Status, treasury.AccountCode, collection.Amount }, utcNow);
        await db.SaveChangesAsync(ct);

        return MapCollection(collection);
    }

    public async Task<CollectionDto> RecordBankTransferAsync(RecordBankTransferCommand command, CancellationToken ct)
    {
        var existing = await db.Collections.AsNoTracking()
            .Include(c => c.Allocations)
            .FirstOrDefaultAsync(c => c.IdempotencyKey == command.IdempotencyKey, ct);
        if (existing is not null)
        {
            return MapCollection(existing);
        }

        var req = command.Request;
        if (!await db.Customers.AnyAsync(c => c.Id == req.CustomerId, ct))
        {
            throw new NotFoundException("Customer", req.CustomerId);
        }

        var normalizedHash = req.PaymentProofImageHash.Trim().ToLowerInvariant();
        var duplicateProof = await db.Collections.AsNoTracking()
            .AnyAsync(c => c.PaymentProofImageHash == normalizedHash && c.Status != "Rejected", ct);
        if (duplicateProof)
        {
            throw new ConflictException("Duplicate bank transfer payment proof image hash detected. This receipt was already submitted.");
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var originalValueDate = req.OriginalValueDateUtc?.ToUniversalTime() ?? utcNow;
        var receiptNumber = $"RCV-BNK-{utcNow:yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[^8..].ToUpperInvariant()}";

        var collection = Collection.CreateBankTransfer(
            receiptNumber,
            req.CustomerId,
            currentUser.UserId,
            req.Amount,
            req.BankReferenceNumber,
            req.PaymentProofImageUrl,
            normalizedHash,
            originalValueDate,
            command.IdempotencyKey,
            utcNow);

        db.Collections.Add(collection);
        await ApplyAllocationsAsync(collection, req.CustomerId, req.Allocations, applyToInvoicesNow: false, ct);

        AuditAndOutboxHelper.AppendAudit(db, currentUser, "RecordBankTransfer", "Collection", collection.Id.ToString(), null, new { collection.ReceiptNumber, collection.Amount, collection.BankReferenceNumber, collection.Status }, utcNow);
        await db.SaveChangesAsync(ct);

        return MapCollection(collection);
    }

    public async Task<CollectionDto> VerifyBankTransferAsync(VerifyBankTransferCommand command, CancellationToken ct)
    {
        var collection = await db.Collections
            .Include(c => c.Customer)
            .Include(c => c.Allocations)
            .FirstOrDefaultAsync(c => c.Id == command.CollectionId, ct)
            ?? throw new NotFoundException("Collection", command.CollectionId);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var req = command.Request;

        if (!req.Approve)
        {
            collection.VerifyBankTransfer(false, Guid.Empty, currentUser.UserId, utcNow);
            AuditAndOutboxHelper.AppendAudit(db, currentUser, "RejectBankTransfer", "Collection", collection.Id.ToString(), null, new { collection.Status }, utcNow);
            await db.SaveChangesAsync(ct);
            return MapCollection(collection);
        }

        var bankAccount = await db.FinancialAccounts.FirstOrDefaultAsync(a => a.Id == req.TargetBankAccountId, ct)
            ?? throw new NotFoundException("FinancialAccount", req.TargetBankAccountId);

        if (bankAccount.AccountType != "BankAccount")
        {
            throw new BusinessRuleException("Target account for bank transfer verification must have AccountType 'BankAccount'.");
        }

        var fiscalPeriod = await ResolveOpenFiscalPeriodAsync(utcNow, ct);
        var arAccount = await EnsureSystemAccountAsync("AccountsReceivable", null, ct);

        collection.VerifyBankTransfer(true, bankAccount.Id, currentUser.UserId, utcNow);

        foreach (var alloc in collection.Allocations)
        {
            var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == alloc.InvoiceId, ct)
                ?? throw new NotFoundException("Invoice", alloc.InvoiceId);
            invoice.ApplyPayment(alloc.AllocatedAmount);
        }

        bankAccount.AdjustBalance(collection.Amount);
        arAccount.AdjustBalance(-collection.Amount);
        collection.Customer.Credit(collection.Amount);

        var entryNumber = $"JE-{utcNow:yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[^8..].ToUpperInvariant()}";
        var journal = JournalEntry.PostBalanced(
            entryNumber,
            fiscalPeriod,
            utcNow,
            collection.OriginalValueDateUtc,
            "Collection",
            collection.Id,
            $"تأكيد حوالة بنكية للعميل - سند {collection.ReceiptNumber}",
            currentUser.UserId,
            [
                (bankAccount.Id, null, null, collection.Amount, 0m),
                (arAccount.Id, collection.CustomerId, null, 0m, collection.Amount)
            ]);
        db.JournalEntries.Add(journal);

        AuditAndOutboxHelper.EnqueueOutbox(db, "FinancialKpiUpdated", new { Event = "BankTransferVerified", collection.Id, collection.Amount, BankAccountId = bankAccount.Id }, utcNow);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "VerifyBankTransfer", "Collection", collection.Id.ToString(), null, new { collection.Status, bankAccount.AccountCode, collection.Amount, collection.OriginalValueDateUtc }, utcNow);
        await db.SaveChangesAsync(ct);

        return MapCollection(collection);
    }

    public async Task<PagedResult<JournalEntryDto>> GetJournalEntriesAsync(GetJournalEntriesQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var q = db.JournalEntries.AsNoTracking().Include(j => j.Lines);

        var total = await q.CountAsync(ct);
        var entries = await q
            .OrderByDescending(j => j.PostingDateUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = entries.Select(j => new JournalEntryDto(
            j.Id, j.EntryNumber, j.FiscalPeriodId, j.PostingDateUtc, j.OriginalValueDateUtc,
            j.ReferenceType, j.ReferenceId, j.DescriptionAr, j.CreatedByUserId,
            j.Lines.Select(l => new JournalLineDto(l.Id, l.FinancialAccountId, l.CustomerId, l.SupplierId, l.DebitAmount, l.CreditAmount)).ToList())).ToList();

        var nextCursor = (page * pageSize) < total ? (page + 1).ToString() : null;
        return new PagedResult<JournalEntryDto>(items, total, nextCursor);
    }

    private async Task<FiscalPeriod> ResolveOpenFiscalPeriodAsync(DateTime postingDateUtc, CancellationToken ct)
    {
        var period = await db.FiscalPeriods.FirstOrDefaultAsync(
            p => p.FiscalYear == postingDateUtc.Year && p.FiscalMonth == postingDateUtc.Month, ct);

        if (period is null)
        {
            throw new BusinessRuleException($"No fiscal period is open for {postingDateUtc.Year}-{postingDateUtc.Month:00}. Create an open fiscal period via POST /api/v1/finance/fiscal-periods first.");
        }

        period.EnsureOpen();
        return period;
    }

    private async Task<FinancialAccount> EnsureSystemAccountAsync(string accountType, Guid? ownerRepId, CancellationToken ct)
    {
        var existing = await db.FinancialAccounts.FirstOrDefaultAsync(
            a => a.AccountType == accountType && a.OwnerRepId == ownerRepId && a.IsActive, ct);
        if (existing is not null)
        {
            return existing;
        }

        var suffix = ownerRepId.HasValue ? ownerRepId.Value.ToString("N")[^8..].ToUpperInvariant() : "MAIN";
        var code = accountType == "RepCustodyWallet" ? $"CUSTODY-{suffix}" : "AR-1200";
        var nameAr = accountType == "RepCustodyWallet" ? $"عهدة مندوب {suffix}" : "الذمم المدينة - العملاء";
        var nameEn = accountType == "RepCustodyWallet" ? $"Rep Custody Wallet {suffix}" : "Accounts Receivable";

        var created = FinancialAccount.Create(code, nameAr, nameEn, accountType, ownerRepId);
        db.FinancialAccounts.Add(created);
        return created;
    }

    private async Task ApplyAllocationsAsync(
        Collection collection,
        Guid customerId,
        CollectionAllocationRequest[]? allocations,
        bool applyToInvoicesNow,
        CancellationToken ct)
    {
        if (allocations is null || allocations.Length == 0)
        {
            return;
        }

        foreach (var alloc in allocations)
        {
            var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == alloc.InvoiceId, ct)
                ?? throw new NotFoundException("Invoice", alloc.InvoiceId);
            if (invoice.CustomerId != customerId)
            {
                throw new BusinessRuleException($"Invoice '{invoice.InvoiceNumber}' does not belong to customer '{customerId}'.");
            }

            collection.Allocations.Add(CollectionAllocation.Create(collection.Id, invoice.Id, alloc.AllocatedAmount));
            if (applyToInvoicesNow)
            {
                invoice.ApplyPayment(alloc.AllocatedAmount);
            }
        }
    }

    private static CollectionDto MapCollection(Collection c) =>
        new(
            c.Id, c.ReceiptNumber, c.CustomerId, c.RepId, c.PaymentMethod, c.Amount, c.Status,
            c.IsCustomerConfirmed, c.CustomerConfirmedAtUtc, c.PaymentProofImageUrl, c.PaymentProofImageHash,
            c.BankReferenceNumber, c.TargetFinancialAccountId, c.VerifiedByAccountantId, c.VerifiedAtUtc,
            c.OriginalValueDateUtc, c.CreatedAtUtc,
            c.Allocations.Select(a => new CollectionAllocationDto(a.Id, a.InvoiceId, a.AllocatedAmount)).ToList());
}

public sealed class OperationsModuleService(
    AlMomiaaDbContext db,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : IOperationsModuleService
{
    public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(CancellationToken ct)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var accounts = await db.FinancialAccounts.AsNoTracking().Where(a => a.IsActive).ToListAsync(ct);

        var mainSafe = accounts.Where(a => a.AccountType == "MainTreasurySafe").Sum(a => a.CurrentBalance);
        var repCustody = accounts.Where(a => a.AccountType == "RepCustodyWallet").Sum(a => a.CurrentBalance);
        var bankBalance = accounts.Where(a => a.AccountType == "BankAccount").Sum(a => a.CurrentBalance);

        var receivables = await db.Customers.AsNoTracking().SumAsync(c => c.CurrentBalance, ct);
        var payables = await db.Suppliers.AsNoTracking().SumAsync(s => s.CurrentPayableBalance, ct);

        var stocks = await db.WarehouseStocks.AsNoTracking()
            .Include(s => s.MasterProduct)
            .ToListAsync(ct);
        var inventoryValuation = Math.Round(stocks.Sum(s => s.PhysicalQty * s.MasterProduct.WeightedAvgCost), 2, MidpointRounding.AwayFromZero);
        var lowStockCount = stocks.Count(s => (s.PhysicalQty - s.ReservedQty) <= s.MinReorderQty);
        var pendingOrdersCount = await db.SalesOrders.AsNoTracking().CountAsync(o => o.Status == "PendingApproval", ct);

        return new ExecutiveDashboardDto(
            DateOnly.FromDateTime(utcNow),
            mainSafe,
            repCustody,
            bankBalance,
            receivables,
            payables,
            inventoryValuation,
            pendingOrdersCount,
            lowStockCount);
    }

    public async Task<RoleDashboardDto> GetRoleDashboardAsync(string roleView, CancellationToken ct)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var normalized = roleView.Trim().ToLowerInvariant();
        var metrics = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        switch (normalized)
        {
            case "sales":
                metrics["pendingApprovalOrders"] = await db.SalesOrders.AsNoTracking().CountAsync(o => o.Status == "PendingApproval", ct);
                metrics["approvedOrders"] = await db.SalesOrders.AsNoTracking().CountAsync(o => o.Status == "Approved", ct);
                metrics["totalInvoicedSAR"] = await db.Invoices.AsNoTracking().SumAsync(i => i.TotalAmount, ct);
                metrics["activeCustomers"] = await db.Customers.AsNoTracking().CountAsync(c => c.IsActive, ct);
                break;

            case "inventory":
            case "warehouse":
                var stocks = await db.WarehouseStocks.AsNoTracking().ToListAsync(ct);
                metrics["totalPhysicalUnits"] = stocks.Sum(s => s.PhysicalQty);
                metrics["totalReservedUnits"] = stocks.Sum(s => s.ReservedQty);
                metrics["totalIncomingUnits"] = stocks.Sum(s => s.IncomingQty);
                metrics["lowStockSkuCount"] = stocks.Count(s => (s.PhysicalQty - s.ReservedQty) <= s.MinReorderQty);
                metrics["inTransitTransfers"] = await db.StockTransfers.AsNoTracking().CountAsync(t => t.Status == "InTransit", ct);
                break;

            case "finance":
                metrics["pendingBankTransfers"] = await db.Collections.AsNoTracking().CountAsync(c => c.Status == "PendingBankVerification", ct);
                metrics["cashInRepCustodySAR"] = await db.FinancialAccounts.AsNoTracking().Where(a => a.AccountType == "RepCustodyWallet").SumAsync(a => a.CurrentBalance, ct);
                metrics["mainSafeSAR"] = await db.FinancialAccounts.AsNoTracking().Where(a => a.AccountType == "MainTreasurySafe").SumAsync(a => a.CurrentBalance, ct);
                metrics["totalReceivablesSAR"] = await db.Customers.AsNoTracking().SumAsync(c => c.CurrentBalance, ct);
                metrics["journalEntriesCount"] = await db.JournalEntries.AsNoTracking().CountAsync(ct);
                break;

            case "rep":
                metrics["myPendingOrders"] = await db.SalesOrders.AsNoTracking().CountAsync(o => o.RepId == currentUser.UserId && o.Status == "PendingApproval", ct);
                metrics["myCustomersCount"] = await db.Customers.AsNoTracking().CountAsync(c => c.AssignedRepId == currentUser.UserId, ct);
                metrics["myCustodyCashSAR"] = await db.FinancialAccounts.AsNoTracking().Where(a => a.AccountType == "RepCustodyWallet" && a.OwnerRepId == currentUser.UserId).SumAsync(a => a.CurrentBalance, ct);
                break;

            default:
                throw new BusinessRuleException($"Unsupported dashboard role view '{roleView}'. Supported views: sales, inventory, warehouse, finance, rep.");
        }

        return new RoleDashboardDto(normalized, metrics, utcNow);
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(GetAuditLogsQuery query, CancellationToken ct)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 250);
        var q = db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            var entity = query.EntityName.Trim();
            q = q.Where(a => a.EntityName == entity);
        }

        if (query.UserId.HasValue)
        {
            q = q.Where(a => a.UserId == query.UserId.Value);
        }

        var total = await q.CountAsync(ct);

        if (!string.IsNullOrWhiteSpace(query.Cursor) && long.TryParse(query.Cursor, out var cursorId))
        {
            q = q.Where(a => a.Id < cursorId);
        }

        var items = await q
            .OrderByDescending(a => a.Id)
            .Take(pageSize)
            .Select(a => new AuditLogDto(a.Id, a.UserId, a.UserRole, a.ActionName, a.EntityName, a.EntityId, a.OldValuesJson, a.NewValuesJson, a.IpAddress, a.TimestampUtc))
            .ToListAsync(ct);

        var nextCursor = items.Count == pageSize ? items[^1].Id.ToString() : null;
        return new PagedResult<AuditLogDto>(items, total, nextCursor);
    }

    public async Task<int> RecordGpsPingsAsync(RecordGpsPingsCommand command, CancellationToken ct)
    {
        foreach (var ping in command.Request.Pings)
        {
            db.GpsRouteHistories.Add(GpsRouteHistory.Record(
                currentUser.UserId,
                ping.Latitude,
                ping.Longitude,
                ping.AccuracyMeters,
                ping.NearCustomerId,
                ping.RecordedAtUtc.ToUniversalTime()));
        }

        await db.SaveChangesAsync(ct);
        return command.Request.Pings.Length;
    }
}
