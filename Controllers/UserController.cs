using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace BankingAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private static List<User> users = new List<User>();

        [HttpPost]
        public ActionResult<CreateUserResponse> CreateUser([FromBody] CreateUserRequest createUserRequest)
        {
            ResMsg resMsg = new ResMsg();
            // Extracting the name from the payload
            string name = createUserRequest?.Name?.Trim();

            // Check if the name contains any numbers or special characters
            if (string.IsNullOrEmpty(name) || !IsValidName(name))
            {
                resMsg.Message = "Invalid user name. Name should not contain numbers or special characters (except spaces), and it should not be empty.";
                return BadRequest(resMsg);
            }

            var userId = GenerateUserId();
            var user = new User { Id = userId, Name = name, Accounts = new List<Account>() };

            // Creating a default account with $100 deposit
            var accountId = GenerateAccountId();
            var account = new Account { Id = accountId, Balance = 100 };
            user.Accounts.Add(account);

            users.Add(user);
            CreateUserResponse res = new CreateUserResponse();
            res.UserId = userId;
            res.AccountId = accountId;
            res.Message = $"User created successfully with User '{name}', account {accountId} and $100 deposit.";
            // Return 201 Created with the route to get the created resource
            return CreatedAtAction(nameof(GetUser), new { userId = userId }, res);

        }

        [HttpDelete("{userId}")]
        public ActionResult<ResMsg> DeleteUser([FromRoute] string userId)
        {
            ResMsg resMsg = new ResMsg();
            var user = users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                resMsg.Message = $"User with ID {userId} not found.";
                return NotFound(resMsg);
            }

            if (user.Accounts.Any())
            {
                resMsg.Message = "You have to delete all associated accounts first for the user.";
                return BadRequest(resMsg);
            }

            users.Remove(user);
            resMsg.Message = $"User with ID {userId} deleted successfully.";

            return Ok(resMsg);
        }

        [HttpPost("{userId}/accounts")]
        public ActionResult<CreateAccountResponse> CreateAccount([FromRoute] string userId)
        {
            ResMsg resMsg = new ResMsg();
            var user = users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                resMsg.Message = $"User with ID {userId} not found.";
                return NotFound(resMsg);
            }

            var accountId = GenerateAccountId();
            var account = new Account { Id = accountId, Balance = 100 };
            user.Accounts.Add(account);
            CreateAccountResponse accRes = new CreateAccountResponse();
            accRes.AccountId = accountId;
            accRes.Balance = account.Balance;
            accRes.Message = $"Account created successfully with $100 deposit.";
            // Return 201 Created with the route to get the created resource
            return CreatedAtAction(nameof(GetUserAccount), new { userId = userId, accountId = accountId }, accRes);
        }

        [HttpDelete("{userId}/accounts/{accountId}")]
        public ActionResult<ResMsg> DeleteAccount(string userId, string accountId)
        {
            ResMsg resMsg = new ResMsg();
            var user = users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                resMsg.Message = $"User with ID {userId} not found.";
                return NotFound(resMsg);
            }

            var account = user.Accounts.FirstOrDefault(a => a.Id == accountId);

            if (account == null)
            {
                resMsg.Message = $"Account with ID {accountId} not found for user {userId}.";
                return NotFound(resMsg);
            }

            user.Accounts.Remove(account);
            resMsg.Message = $"Account with ID {accountId} deleted successfully for user {userId}.";

            return Ok(resMsg);
        }

        [HttpPut("{userId}/accounts/{accountId}/deposit")]
        public ActionResult<ResMsg> Deposit(string userId, string accountId, [FromBody] UpdateRequest depositRequest)
        {
            ResMsg resMsg = new ResMsg();
            // Check if the amount is a valid numeric value
            if (!IsValidAmount(depositRequest.Amount))
            {
                resMsg.Message = "Invalid deposit amount. Amount should contain only numbers.";
                return BadRequest(resMsg);
            }

            var user = users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                resMsg.Message = $"User with ID {userId} not found.";
                return NotFound(resMsg);
            }

            var account = user.Accounts.FirstOrDefault(a => a.Id == accountId);

            if (account == null)
            {
                resMsg.Message = $"Account with ID {accountId} not found for user {userId}.";
                return BadRequest(resMsg);
            }

            decimal amount = depositRequest.Amount;

            if (amount <= 0 || amount > 10000)
            {
                resMsg.Message = "Invalid deposit amount. Deposit amount must be between 0 and $10,000.";
                return BadRequest(resMsg);
            }

            account.Balance += amount;
            return NoContent();
        }

        [HttpPut("{userId}/accounts/{accountId}/withdraw")]
        public ActionResult<ResMsg> Withdraw(string userId, string accountId, [FromBody] UpdateRequest withdrawRequest)
        {
            ResMsg resMsg = new ResMsg();
            // Check if the amount is a valid numeric value
            if (!IsValidAmount(withdrawRequest.Amount))
            {
                resMsg.Message = "Invalid withdrawal amount. Amount should contain only numbers.";
                return BadRequest(resMsg);
            }

            var user = users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                resMsg.Message = $"User with ID {userId} not found.";
                return NotFound(resMsg);
            }

            var account = user.Accounts.FirstOrDefault(a => a.Id == accountId);

            if (account == null)
            {
                resMsg.Message = $"Account with ID {accountId} not found for user {userId}.";
                return NotFound(resMsg);
            }

            decimal amount = withdrawRequest.Amount;

            if (amount <= 0 || amount > account.Balance || amount > (account.Balance * 0.9m) || (account.Balance - amount) < 100)
            {
                    resMsg.Message = $"Invalid withdrawal amount. The withdrawl can be up to {account.Balance * 0.9m} or should leave a balance >= 100";
                    return BadRequest(resMsg);
            }

            account.Balance -= amount;
            return NoContent();
        }

        [HttpGet]
        public IActionResult GetUsers()
        {
            return Ok(users.Select(user => new { Id = user.Id }));
        }

        [HttpGet("{userId}")]
        public IActionResult GetUser(string userId)
        {
            ResMsg resMsg = new ResMsg();
            var user = users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                resMsg.Message = $"User with ID {userId} not found.";
                return NotFound(resMsg);
            }

            return Ok(new
            {
                UserId = user.Id,
                UserName = user.Name,
                Accounts = user.Accounts.Select(a => new { AccountId = a.Id, Balance = a.Balance })
            });
        }

        [HttpGet("{userId}/accounts")]
        public IActionResult GetUserAccounts(string userId)
        {
            ResMsg resMsg = new ResMsg();
            var user = users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                resMsg.Message = $"User with ID {userId} not found.";
                return NotFound(resMsg);
            }

            return Ok(user.Accounts.Select(a => new { AccountId = a.Id, Balance = a.Balance }));
        }

        [HttpGet("{userId}/accounts/{accountId}")]
        public IActionResult GetUserAccount(string userId, string accountId)
        {
            var user = users.FirstOrDefault(u => u.Id == userId);
            ResMsg resMsg = new ResMsg();
            if (user == null)
            {
                resMsg.Message = $"User with ID {userId} not found.";
                return NotFound(resMsg);
            }

            var account = user.Accounts.FirstOrDefault(a => a.Id == accountId);

            if (account == null)
            {
                resMsg.Message = $"Account with ID {accountId} not found for user {userId}.";
                return BadRequest(resMsg);
            }

            return Ok(new
            {
                AccountId = account.Id,
                Balance = account.Balance
            });
        }

        private string GenerateAccountId()
        {
            Random random = new Random();
            int part1 = random.Next(10000000, 99999999);
            int part2 = random.Next(10000000, 99999999);

            return $"{part1}{part2}";
        }

        private string GenerateUserId() 
        {
            Guid guid = Guid.NewGuid();
            string base64Guid = Convert.ToBase64String(guid.ToByteArray());
            // Replace characters that are not alphanumeric
            string alphanumericString = Regex.Replace(base64Guid, "[^a-zA-Z0-9]", "");

            // Take the first 10 characters
            return alphanumericString.Substring(0, 10).ToUpper();
        }

        private bool IsValidName(string name)
        {
            // Check if the name contains any numbers or special characters (except spaces)
            return !Regex.IsMatch(name, "[^a-zA-Z ]");
        }

        private bool IsValidAmount(decimal amount)
        {
            // Check if the amount is a valid numeric value
            return decimal.TryParse(amount.ToString(), out _);
        }

        public class CreateUserRequest
        {
            public string Name { get; set; }
        }

        public class User
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public List<Account> Accounts { get; set; }
        }

        public class Account
        {
            public string Id { get; set; }
            public decimal Balance { get; set; }
        }

        public class UpdateRequest
        {
            public decimal Amount { get; set; }
        }

        public class ResMsg
        {
            public string Message { get; set; }
        }

        public class CreateUserResponse
        {
            public string UserId { get; set; }
            public string AccountId { get; set; }
            public string Message { get; set; }
        }

        public class CreateAccountResponse
        {
            public string AccountId { get; set; }
            public decimal Balance { get; set; }
            public string Message { get; set; }
        }
    }
}