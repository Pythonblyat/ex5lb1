using System;
using System.Collections.Generic;

namespace BankingDemo
{
    public class Transaction
    {
        public DateTime Date;
        public string Type;
        public decimal Amount;

        public Transaction(DateTime date, string type, decimal amount)
        {
            Date = date;
            Type = type;
            Amount = amount;
        }
    }

    public class BankAccount
    {
        private decimal balance;
        private List<Transaction> transactions = new List<Transaction>();
        private List<AuditEntry> audit = new List<AuditEntry>();
        private static int counter = 0;

        public string AccountId { get; private set; }

        public decimal Balance
        {
            get { return balance; }
        }

        public IReadOnlyList<Transaction> Transactions
        {
            get { return transactions; }
        }

        public decimal OverdraftLimit { get; protected set; }

        public BankAccount()
        {
            counter++;
            AccountId = "ACC-" + counter;
        }

        public void Deposit(decimal amount)
        {
            if (!Validate(amount))
            {
                throw new ArgumentException("Сума має бути більше нуля");
            }

            balance = balance + amount;
            transactions.Add(new Transaction(DateTime.Now, "Deposit", amount));
            WriteAudit("Deposit " + amount);
        }

        public void Withdraw(decimal amount)
        {
            if (!Validate(amount))
            {
                throw new ArgumentException("Сума має бути більше нуля");
            }

            decimal fee = ApplyFee(amount);
            decimal total = amount + fee;

            if (balance - total < -OverdraftLimit)
            {
                throw new InvalidOperationException("Перевищено ліміт овердрафту");
            }

            balance = balance - total;
            transactions.Add(new Transaction(DateTime.Now, "Withdraw", amount));
            if (fee > 0)
            {
                transactions.Add(new Transaction(DateTime.Now, "Fee", fee));
            }
            WriteAudit("Withdraw " + amount);
        }

        protected virtual decimal ApplyFee(decimal amount)
        {
            return 1m;
        }

        internal void ReloadFromStorage(decimal newBalance, List<Transaction> history)
        {
            balance = newBalance;
            transactions.Clear();
            transactions.AddRange(history);
            WriteAudit("Reloaded from storage");
        }

        private bool Validate(decimal amount)
        {
            return amount > 0;
        }

        private void WriteAudit(string message)
        {
            audit.Add(new AuditEntry(DateTime.Now, message));
        }

        private class AuditEntry
        {
            public DateTime Timestamp;
            public string Message;

            public AuditEntry(DateTime timestamp, string message)
            {
                Timestamp = timestamp;
                Message = message;
            }
        }
    }

    public class PremiumAccount : BankAccount
    {
        public PremiumAccount()
        {
            OverdraftLimit = 500m;
        }

        protected override decimal ApplyFee(decimal amount)
        {
            return 0m; 
        }
    }


    internal class AccountRepository
    {
        public void Load(BankAccount account)
        {
            List<Transaction> history = new List<Transaction>();
            history.Add(new Transaction(DateTime.Now, "Deposit", 100m));
            account.ReloadFromStorage(100m, history);
        }
    }

    public class Program
    {
        public static void Main()
        {
            BankAccount acc = new BankAccount();
            acc.Deposit(200m);
            acc.Withdraw(50m);
            Console.WriteLine(acc.AccountId + ": " + acc.Balance);

            PremiumAccount premium = new PremiumAccount();
            premium.Withdraw(300m);
            Console.WriteLine(premium.AccountId + ": " + premium.Balance);

        }
    }
}