namespace BankingSystem.Domain.Enums;

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer,
    BillPayment,
    InterestCredit,
    Fee,
    Refund,
    CardCharge       // Direct card payment via Stripe
}
