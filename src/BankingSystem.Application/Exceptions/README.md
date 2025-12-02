# Exception Handling Architecture

## Overview
This document describes the exception handling strategy used in the BankingSystem application following Clean Architecture principles.

## Exception Hierarchy

```
System.Exception
?
??? BankingApplicationException (Application Layer)
?   ??? NotFoundException
?   ??? ValidationFailureException
?   ??? UnauthorizedException
?   ??? ForbiddenException
?
??? DomainException (Domain Layer)
    ??? InsufficientFundsException
    ??? InvalidAccountException
    ??? InvalidCardException
```

## Exception Types

### Application Layer Exceptions (`BankingSystem.Application.Exceptions`)

#### 1. **BankingApplicationException** (Base)
- **Purpose**: Base exception for all application-level errors
- **HTTP Status**: 400 Bad Request
- **Usage**: General application logic errors

```csharp
throw new BankingApplicationException("Cannot close account with remaining balance");
```

#### 2. **NotFoundException**
- **Purpose**: Entity not found errors
- **HTTP Status**: 404 Not Found
- **Usage**: When queried entity doesn't exist

```csharp
throw new NotFoundException(string.Format(ValidationMessages.AccountNotFound, accountId));
```

#### 3. **ValidationFailureException**
- **Purpose**: Input validation errors
- **HTTP Status**: 400 Bad Request
- **Usage**: Business rule validation failures

```csharp
throw new ValidationFailureException(ValidationMessages.EmailAlreadyRegistered);
```

#### 4. **UnauthorizedException**
- **Purpose**: Authentication failures
- **HTTP Status**: 401 Unauthorized
- **Usage**: Invalid credentials or authentication required

```csharp
throw new UnauthorizedException(ValidationMessages.InvalidCredentials);
```

#### 5. **ForbiddenException**
- **Purpose**: Authorization failures
- **HTTP Status**: 403 Forbidden
- **Usage**: User lacks permission for the operation

```csharp
throw new ForbiddenException("You don't have permission to access this resource");
```

### Domain Layer Exceptions (`BankingSystem.Domain.Exceptions`)

#### 1. **DomainException** (Base)
- **Purpose**: Base exception for domain logic errors
- **HTTP Status**: 400 Bad Request

#### 2. **InsufficientFundsException**
- **Purpose**: Account balance insufficient for operation
- **HTTP Status**: 400 Bad Request

```csharp
throw new InsufficientFundsException(ValidationMessages.InsufficientFunds);
```

#### 3. **InvalidAccountException**
- **Purpose**: Account state invalid for operation
- **HTTP Status**: 400 Bad Request

#### 4. **InvalidCardException**
- **Purpose**: Card state invalid for operation
- **HTTP Status**: 400 Bad Request

## Best Practices

### ? DO

1. **Use centralized constants for messages**
```csharp
// Good
throw new NotFoundException(string.Format(ValidationMessages.AccountNotFound, accountId));

// Bad
throw new NotFoundException($"Account with id '{accountId}' was not found");
```

2. **Use appropriate exception types**
```csharp
// Good - Entity not found
throw new NotFoundException(string.Format(ValidationMessages.UserNotFound, userId));

// Bad - Wrong exception type
throw new BankingApplicationException("User not found");
```

3. **Use custom exceptions instead of System exceptions**
```csharp
// Good
throw new NotFoundException(string.Format(ValidationMessages.AccountNotFound, id));

// Bad
throw new KeyNotFoundException($"Account with ID {id} not found");
```

4. **Domain exceptions for business rules**
```csharp
// Good - Domain business rule
if (account.Balance.IsLessThan(amount))
    throw new InsufficientFundsException(ValidationMessages.InsufficientFunds);
```

### ? DON'T

1. **Don't use System.ApplicationException** (deprecated)
2. **Don't hardcode error messages** - use `ValidationMessages` constants
3. **Don't mix System exceptions with custom exceptions** for the same scenarios
4. **Don't throw generic exceptions** - use specific exception types

## Validation Messages

All error messages are centralized in:
```
BankingSystem.Application.Constants.ValidationMessages
```

### Categories:
- **Field Validation**: RequiredField, InvalidEmail, etc.
- **Entity Type Validation**: InvalidAccountType, InvalidTransactionType
- **Not Found Messages**: AccountNotFound, UserNotFound, etc.
- **Business Logic**: InsufficientFunds, CannotCloseAccountWithBalance, etc.
- **Authentication**: InvalidCredentials, UserAccountNotActive, etc.

## Global Exception Handler

All exceptions are caught and handled by `GlobalExceptionHandlerMiddleware` which:
1. Logs the exception
2. Maps to appropriate HTTP status code
3. Returns RFC 7807 Problem Details response
4. Includes stack trace in Development mode

## Usage Examples

### Command Handlers

```csharp
public class CloseAccountHandler : IRequestHandler<CloseAccountCommand, Unit>
{
    public async Task<Unit> Handle(CloseAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId);
        
        // Not Found
        if (account == null)
            throw new NotFoundException(string.Format(ValidationMessages.AccountNotFound, request.AccountId));

        // Business Rule Validation
        if (account.Balance.Amount > 0)
            throw new BankingApplicationException(ValidationMessages.CannotCloseAccountWithBalance);

        // Process...
    }
}
```

### Authentication

```csharp
public class LoginHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        
        if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
            throw new UnauthorizedException(ValidationMessages.InvalidCredentials);

        if (!user.IsActive)
            throw new UnauthorizedException(ValidationMessages.UserAccountNotActive);
            
        // Generate tokens...
    }
}
```

### Domain Business Logic

```csharp
public class TransferFundsHandler : IRequestHandler<TransferFundsCommand, Unit>
{
    public async Task<Unit> Handle(TransferFundsCommand request, CancellationToken cancellationToken)
    {
        // Business rule
        if (request.FromAccountId == request.ToAccountId)
            throw new BankingApplicationException(ValidationMessages.CannotTransferToSameAccount);

        // Domain business rule
        if (fromAccount.Balance.IsLessThan(transferAmount))
            throw new InsufficientFundsException(ValidationMessages.InsufficientFunds);
            
        // Process transfer...
    }
}
```

## Migration Notes

### Removed Components
- ? **ExceptionHelper.cs** - Not used, redundant abstraction layer

### Changes from Previous Implementation
1. Renamed `ApplicationException` ? `BankingApplicationException` (avoid System.ApplicationException conflict)
2. All handlers now use custom exceptions instead of System exceptions
3. All error messages use `ValidationMessages` constants
4. Consistent exception handling across all layers

## Related Files
- `src/BankingSystem.Application/Exceptions/` - Exception definitions
- `src/BankingSystem.Application/Constants/ValidationMessages.cs` - Error message constants
- `src/BankingSystem.API/Middleware/GlobalExceptionHandlerMiddleware.cs` - Exception handler
