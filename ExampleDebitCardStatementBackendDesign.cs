public class DebitCardTransaction
{
    public string Merchant { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
}

public class DebitCardStatement
{
    public string AccountNumber { get; set; }
    public DateTime StatementDate { get; set; }
    public decimal BeginningBalance { get; set; }
    public decimal EndingBalance { get; set; }
    public List<DebitCardTransaction> Transactions { get; set; }

    public DebitCardStatement(
        string accountNumber,
        DateTime statementDate,
        decimal beginningBalance,
        decimal endingBalance,
        List<DebitCardTransaction> transactions)
    {
        AccountNumber = accountNumber;
        StatementDate = statementDate;
        BeginningBalance = beginningBalance;
        EndingBalance = endingBalance;
        Transactions = transactions;
    }
}

public class DebitCardStatementWithPagination
{
    public DebitCardStatement DebitCardStatement { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

// AccountID should be passed from some sort of JWT Token we can use to verify the identity of the user and allow access
[Authorize]
[HttpGet("/debit-card-statement")]
public IActionResult GetDebitCardStatement(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    [FromQuery] string order,
    [FromQuery] int limit,
    [FromQuery] int page)
{
    var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // Of course verify more beyond just the id being empty, we would check our db/systems to ensure this id exist
    if (string.IsNullOrEmpty(accountId))
    {
        return Unauthorized();
    }


    var transactions = _dbContext.Transactions
        .Where(t => t.AccountId == accountId && t.Date >= startDate && t.Date <= endDate)
        .OrderByDescending(t => t.Date)
        .Skip((page - 1) * limit)
        .Take(limit)
        .ToList();

    var totalTransactions = transactions.Count();

    if (!isAscending)
    {
        transactions.Reverse();
    }

    var debitCardStatement = new DebitCardStatement(accountId, startDate, endDate, transactions);
    var totalPages = (int)Math.Ceiling((double)totalTransactions / limit);
    var currentPage = page;

    var debitCardStatementWithPagination = new DebitCardStatementWithPagination(
        debitCardStatement,
        currentPage,
        totalPages,
        totalTransactions
    );

    return Ok(debitCardStatementWithPagination);
}

// Example webhook service that listens for these debit card transactions and records it  in our ledger
[HttpPost("/webhooks/transactions")]
public IActionResult HandleTransactionWebhook([FromBody] DebitCardTransaction transaction)
{
    // Add transaction to the ledger
    _ledger.AddTransaction(transaction);

    // Return HTTP status code 200 to acknowledge receipt of webhook
    return Ok();
}