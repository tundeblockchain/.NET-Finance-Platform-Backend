using System.Collections.Concurrent;
using FinancePlatform.Models;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;
using CustomerEntity = FinancePlatform.Models.Entities.Customer;

namespace FinancePlatform.Services.Customer;

/// <summary>
/// In-memory customer directory: customers, addresses, component accounts, distribution agreements.
/// </summary>
public sealed class InMemoryCustomerDirectory : ICustomerDirectory
{
    private readonly object _gate = new();
    private int _nextCustomerId = 1;
    private readonly Dictionary<int, CustomerEntity> _customers = new();
    private readonly Dictionary<int, CustomerAddress> _addresses = new();
    private readonly Dictionary<Guid, CustomerAccount> _customerAccounts = new();
    private readonly Dictionary<(int CustomerId, string Currency), Guid> _customerAccountIndex = new();
    private readonly Dictionary<Guid, TradingAccount> _tradingAccounts = new();
    private readonly Dictionary<(int CustomerId, string Currency), Guid> _tradingAccountIndex = new();
    private readonly Dictionary<Guid, DistributionAgreement> _agreements = new();
    private readonly Dictionary<Guid, List<DistributionElement>> _elementsByAgreement = new();
    private readonly ConcurrentDictionary<string, byte> _mutationKeys = new(StringComparer.Ordinal);

    public CustomerProvisioningResult CreateCustomer(CreateCustomerRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FirstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LastName);

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? "GBP"
            : request.Currency.ToUpperInvariant();

        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            var customer = new CustomerEntity
            {
                Id = _nextCustomerId++,
                Email = request.Email.Trim(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                CreatedUtc = now,
                DateModified = now,
                ChangedBy = ChangeActors.System
            };
            _customers[customer.Id] = customer;

            CustomerAddress? address = null;
            if (request.Address is not null)
            {
                address = new CustomerAddress
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Line1 = request.Address.Line1,
                    Line2 = request.Address.Line2,
                    City = request.Address.City,
                    Region = request.Address.Region,
                    PostalCode = request.Address.PostalCode,
                    Country = request.Address.Country,
                    DateModified = now,
                    ChangedBy = ChangeActors.System
                };
                _addresses[customer.Id] = address;
            }

            var customerAccount = new CustomerAccount
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                Currency = currency,
                Settled = 0m,
                Reserved = 0m,
                CreatedUtc = now,
                DateModified = now,
                ChangedBy = ChangeActors.System
            };
            _customerAccounts[customerAccount.Id] = customerAccount;
            _customerAccountIndex[(customer.Id, currency)] = customerAccount.Id;

            var tradingAccount = new TradingAccount
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                Currency = currency,
                Settled = 0m,
                Reserved = 0m,
                CreatedUtc = now,
                DateModified = now,
                ChangedBy = ChangeActors.System
            };
            _tradingAccounts[tradingAccount.Id] = tradingAccount;
            _tradingAccountIndex[(customer.Id, currency)] = tradingAccount.Id;

            var agreement = new DistributionAgreement
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                OwnerComponent = ComponentType.Customer,
                OwnerAccountId = customerAccount.Id,
                Name = "Customer → Trading (park)",
                IsActive = true,
                CreatedUtc = now,
                DateModified = now,
                ChangedBy = ChangeActors.System
            };
            _agreements[agreement.Id] = agreement;

            var element = new DistributionElement
            {
                Id = Guid.NewGuid(),
                AgreementId = agreement.Id,
                TargetType = DistributionTargetType.TradingAccount,
                TargetAccountId = tradingAccount.Id,
                Percentage = 1m,
                Priority = 1,
                DateModified = now,
                ChangedBy = ChangeActors.System
            };
            _elementsByAgreement[agreement.Id] = [element];

            return new CustomerProvisioningResult
            {
                Customer = Clone(customer),
                Address = address is null ? null : Clone(address),
                CustomerAccount = Clone(customerAccount),
                TradingAccount = Clone(tradingAccount),
                DistributionAgreement = Clone(agreement)
            };
        }
    }

    public CustomerEntity? FindCustomer(int customerId)
    {
        lock (_gate)
        {
            return _customers.TryGetValue(customerId, out var c) ? Clone(c) : null;
        }
    }

    public CustomerAddress? FindAddress(int customerId)
    {
        lock (_gate)
        {
            return _addresses.TryGetValue(customerId, out var a) ? Clone(a) : null;
        }
    }

    public DistributionAgreement? FindAgreementByOwnerAccount(Guid ownerAccountId)
    {
        lock (_gate)
        {
            var agreement = _agreements.Values.FirstOrDefault(a =>
                a.OwnerAccountId == ownerAccountId && a.IsActive);
            return agreement is null ? null : Clone(agreement);
        }
    }

    public CustomerAccount? FindCustomerAccount(Guid customerAccountId)
    {
        lock (_gate)
        {
            return _customerAccounts.TryGetValue(customerAccountId, out var a) ? Clone(a) : null;
        }
    }

    public CustomerAccount? FindCustomerAccountByCustomer(int customerId, string currency)
    {
        lock (_gate)
        {
            var key = (customerId, currency.ToUpperInvariant());
            if (_customerAccountIndex.TryGetValue(key, out var id)
                && _customerAccounts.TryGetValue(id, out var account))
            {
                return Clone(account);
            }

            return null;
        }
    }

    public TradingAccount? FindTradingAccount(Guid tradingAccountId)
    {
        lock (_gate)
        {
            return _tradingAccounts.TryGetValue(tradingAccountId, out var a) ? Clone(a) : null;
        }
    }

    public TradingAccount? FindTradingAccountByCustomer(int customerId, string currency)
    {
        lock (_gate)
        {
            var key = (customerId, currency.ToUpperInvariant());
            if (_tradingAccountIndex.TryGetValue(key, out var id)
                && _tradingAccounts.TryGetValue(id, out var account))
            {
                return Clone(account);
            }

            return null;
        }
    }

    public IReadOnlyList<DistributionElement> GetActiveElements(Guid ownerAccountId)
    {
        lock (_gate)
        {
            var agreement = _agreements.Values.FirstOrDefault(a =>
                a.OwnerAccountId == ownerAccountId && a.IsActive);
            if (agreement is null
                || !_elementsByAgreement.TryGetValue(agreement.Id, out var elements))
            {
                return [];
            }

            return elements.OrderBy(e => e.Priority).Select(Clone).ToArray();
        }
    }

    public bool TryCreditCustomerAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (!_mutationKeys.TryAdd(idempotencyKey, 0))
        {
            return true;
        }

        lock (_gate)
        {
            if (!_customerAccounts.TryGetValue(accountId, out var account))
            {
                _mutationKeys.TryRemove(idempotencyKey, out _);
                return false;
            }

            account.Settled += amount;
            account.DateModified = DateTimeOffset.UtcNow;
            account.ChangedBy = ChangeActors.Broker;
            return true;
        }
    }

    public bool TryDebitCustomerAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (!_mutationKeys.TryAdd(idempotencyKey, 0))
        {
            return true;
        }

        lock (_gate)
        {
            if (!_customerAccounts.TryGetValue(accountId, out var account)
                || account.Available < amount)
            {
                _mutationKeys.TryRemove(idempotencyKey, out _);
                return false;
            }

            account.Settled -= amount;
            account.DateModified = DateTimeOffset.UtcNow;
            account.ChangedBy = ChangeActors.Broker;
            return true;
        }
    }

    public bool TryCreditTradingAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (!_mutationKeys.TryAdd(idempotencyKey, 0))
        {
            return true;
        }

        lock (_gate)
        {
            if (!_tradingAccounts.TryGetValue(accountId, out var account))
            {
                _mutationKeys.TryRemove(idempotencyKey, out _);
                return false;
            }

            account.Settled += amount;
            account.DateModified = DateTimeOffset.UtcNow;
            account.ChangedBy = ChangeActors.Broker;
            return true;
        }
    }

    public bool TryDebitTradingAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (!_mutationKeys.TryAdd(idempotencyKey, 0))
        {
            return true;
        }

        lock (_gate)
        {
            if (!_tradingAccounts.TryGetValue(accountId, out var account)
                || account.Available < amount)
            {
                _mutationKeys.TryRemove(idempotencyKey, out _);
                return false;
            }

            account.Settled -= amount;
            account.DateModified = DateTimeOffset.UtcNow;
            account.ChangedBy = ChangeActors.Broker;
            return true;
        }
    }

    public decimal GetCustomerSettled(Guid accountId)
    {
        lock (_gate)
        {
            return _customerAccounts.TryGetValue(accountId, out var a) ? a.Settled : 0m;
        }
    }

    public decimal GetTradingSettled(Guid accountId)
    {
        lock (_gate)
        {
            return _tradingAccounts.TryGetValue(accountId, out var a) ? a.Settled : 0m;
        }
    }

    private static CustomerEntity Clone(CustomerEntity c) => new()
    {
        Id = c.Id,
        Email = c.Email,
        FirstName = c.FirstName,
        LastName = c.LastName,
        CreatedUtc = c.CreatedUtc,
        DateModified = c.DateModified,
        ChangedBy = c.ChangedBy
    };

    private static CustomerAddress Clone(CustomerAddress a) => new()
    {
        Id = a.Id,
        CustomerId = a.CustomerId,
        Line1 = a.Line1,
        Line2 = a.Line2,
        City = a.City,
        Region = a.Region,
        PostalCode = a.PostalCode,
        Country = a.Country,
        DateModified = a.DateModified,
        ChangedBy = a.ChangedBy
    };

    private static CustomerAccount Clone(CustomerAccount a) => new()
    {
        Id = a.Id,
        CustomerId = a.CustomerId,
        Currency = a.Currency,
        Settled = a.Settled,
        Reserved = a.Reserved,
        IsLocked = a.IsLocked,
        LockedByTriggerId = a.LockedByTriggerId,
        LockExpiresUtc = a.LockExpiresUtc,
        CreatedUtc = a.CreatedUtc,
        DateModified = a.DateModified,
        ChangedBy = a.ChangedBy
    };

    private static TradingAccount Clone(TradingAccount a) => new()
    {
        Id = a.Id,
        CustomerId = a.CustomerId,
        Currency = a.Currency,
        Settled = a.Settled,
        Reserved = a.Reserved,
        IsLocked = a.IsLocked,
        LockedByTriggerId = a.LockedByTriggerId,
        LockExpiresUtc = a.LockExpiresUtc,
        CreatedUtc = a.CreatedUtc,
        DateModified = a.DateModified,
        ChangedBy = a.ChangedBy
    };

    private static DistributionAgreement Clone(DistributionAgreement a) => new()
    {
        Id = a.Id,
        CustomerId = a.CustomerId,
        OwnerComponent = a.OwnerComponent,
        OwnerAccountId = a.OwnerAccountId,
        Name = a.Name,
        IsActive = a.IsActive,
        CreatedUtc = a.CreatedUtc,
        DateModified = a.DateModified,
        ChangedBy = a.ChangedBy
    };

    private static DistributionElement Clone(DistributionElement e) => new()
    {
        Id = e.Id,
        AgreementId = e.AgreementId,
        TargetType = e.TargetType,
        TargetAccountId = e.TargetAccountId,
        Percentage = e.Percentage,
        Priority = e.Priority,
        DateModified = e.DateModified,
        ChangedBy = e.ChangedBy
    };
}
