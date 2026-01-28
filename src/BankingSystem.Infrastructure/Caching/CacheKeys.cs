namespace BankingSystem.Infrastructure.Caching;

/// <summary>
/// Cache key constants for Redis caching
/// </summary>
public static class CacheKeys
{
    private const string AccountPrefix = "account";
    private const string UserAccountsPrefix = "user:accounts";
    private const string BalancePrefix = "balance";
    private const string TransactionPrefix = "transaction";
    private const string NotificationPrefix = "notification";
    private const string BillPrefix = "bill";

    // Account Cache Keys
    public static string GetAccountKey(Guid accountId) => $"{AccountPrefix}:{accountId}";
    public static string GetUserAccountsKey(Guid userId) => $"{UserAccountsPrefix}:{userId}";
    public static string GetAccountBalanceKey(Guid accountId) => $"{BalancePrefix}:{accountId}";

    // Transaction Cache Keys
    public static string GetTransactionKey(Guid transactionId) => $"{TransactionPrefix}:{transactionId}";
    public static string GetUserTransactionsKey(Guid userId) => $"{TransactionPrefix}:user:{userId}";

    // Notification Cache Keys
    public static string GetUnreadNotificationsKey(Guid userId) => $"{NotificationPrefix}:unread:{userId}";
    public static string GetNotificationKey(Guid notificationId) => $"{NotificationPrefix}:{notificationId}";

    // Bill Cache Keys
    public static string GetBillKey(Guid billId) => $"{BillPrefix}:{billId}";
    public static string GetUserBillsKey(Guid userId) => $"{BillPrefix}:user:{userId}";
    public static string GetPendingBillsKey(Guid accountId) => $"{BillPrefix}:pending:{accountId}";

    // Batch invalidation patterns
    public static string GetAccountPattern(Guid accountId) => $"{AccountPrefix}:{accountId}:*";
    public static string GetUserPattern(Guid userId) => $"{UserAccountsPrefix}:{userId}:*";
}
