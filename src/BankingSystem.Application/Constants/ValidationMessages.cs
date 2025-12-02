namespace BankingSystem.Application.Constants;

/// <summary>
/// Centralized validation and error messages for consistent error handling
/// </summary>
public static class ValidationMessages
{
    // Field Validation Messages
    public const string RequiredField = "{PropertyName} is required";
    public const string InvalidEmail = "{PropertyName} must be a valid email address";
    public const string MaximumLength = "{PropertyName} must not exceed {MaxLength} characters";
    public const string MinimumLength = "{PropertyName} must be at least {MinLength} characters";
    public const string InvalidPhoneNumber = "{PropertyName} must be a valid phone number";
    public const string InvalidAmount = "Amount must be greater than 0";
    
    // Entity Type Validation
    public const string InvalidAccountType = "Invalid account type";
    public const string InvalidTransactionType = "Invalid transaction type";
    
    // Not Found Messages
    public const string AccountNotFound = "Account with id '{0}' was not found";
    public const string UserNotFound = "User with id '{0}' was not found";
    public const string BillNotFound = "Bill with id '{0}' was not found";
    public const string CardNotFound = "Card with id '{0}' was not found";
    public const string NotificationNotFound = "Notification with id '{0}' was not found";
    public const string TransactionNotFound = "Transaction with id '{0}' was not found";
    
    // Business Logic Validation
    public const string InsufficientFunds = "Insufficient funds for this transaction";
    public const string InsufficientFundsDetail = "Insufficient funds. Available: {0}, Required: {1}";
    public const string CannotTransferToSameAccount = "Cannot transfer to the same account";
    public const string CannotCloseAccountWithBalance = "Cannot close account with remaining balance";
    public const string BillAlreadyPaid = "Bill is already paid";
    public const string AccountNotActive = "Account is not active";
    public const string CardBlocked = "Card is blocked";
    
    // Authentication & Authorization
    public const string InvalidCredentials = "Invalid email or password";
    public const string UserAccountNotActive = "User account is not active";
    public const string UserAccountLocked = "Your account has been locked due to multiple failed login attempts. Please try again after {0} minutes.";
    public const string EmailAlreadyRegistered = "Email is already registered";
    public const string CurrentPasswordIncorrect = "Current password is incorrect";
    public const string PasswordMismatch = "New password and confirm password do not match";
    public const string NewPasswordSameAsCurrent = "New password must be different from current password";
}

public static class ErrorMessages
{
    public const string UnexpectedError = "An unexpected error occurred";
    public const string DatabaseError = "A database error occurred";
    public const string ValidationError = "One or more validation errors occurred";
    public const string InternalServerError = "An internal server error occurred. Please try again later";
}
