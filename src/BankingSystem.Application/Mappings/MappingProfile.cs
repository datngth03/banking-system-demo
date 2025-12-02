namespace BankingSystem.Application.Mappings;

using AutoMapper;
using BankingSystem.Domain.Entities;
using BankingSystem.Application.DTOs.Users;
using BankingSystem.Application.DTOs.Accounts;
using BankingSystem.Application.DTOs.Transactions;
using BankingSystem.Application.DTOs.Bills;
using BankingSystem.Application.DTOs.Cards;
using BankingSystem.Application.DTOs.Notifications;
using BankingSystem.Application.DTOs.AuditLogs;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address != null ? src.Address.Street : null))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address != null ? src.Address.City : null))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Address != null ? src.Address.State : null))
            .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.Address != null ? src.Address.PostalCode : null))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Address != null ? src.Address.Country : null));

        // Account mappings
        CreateMap<Account, AccountDto>()
            .ForMember(dest => dest.AccountType, opt => opt.MapFrom(src => src.AccountType.ToString()))
            .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.Balance.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Balance.Currency));

        // Transaction mappings
        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => src.TransactionType.ToString()))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Amount.Currency))
            .ForMember(dest => dest.BalanceAfter, opt => opt.MapFrom(src => src.BalanceAfter.Amount));

        // Bill mappings
        CreateMap<Bill, BillDto>()
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Amount.Currency));

        // Card mappings
        CreateMap<Card, CardDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        // Notification mappings
        CreateMap<Notification, NotificationDto>();

        // AuditLog mappings
        CreateMap<AuditLog, AuditLogDto>();
    }
}
