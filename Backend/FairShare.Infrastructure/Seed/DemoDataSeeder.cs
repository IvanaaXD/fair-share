using FairShare.Application.Abstractions;
using FairShare.Application.Common.Notifications;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FairShare.Infrastructure.Seed;

/// <summary>
/// Demo data for presenting the application: eight users, four groups, personal and group
/// expenses over the last three months (e.g. August, September and October up to today),
/// budgets, settlements (paid by card, by QR and still open), saved cards, notifications and
/// audit log entries.
///
/// Program.cs calls it only when "DemoData:Enabled" is true (appsettings.Development.json).
/// It runs only once: if the main demo user already exists, nothing is changed. All demo accounts use the
/// password <see cref="DemoPassword"/>.
///
/// The numbers are generated with a fixed random seed, so every fresh database gets the same
/// data; dates are relative to the day the seeder runs.
/// </summary>
public static class DemoDataSeeder
{
    public const string DemoPassword = "Demo1234!";
    private const string MainEmail = "jelena@demo.fairshare.local";

    // Banja Luka (most expenses), Sarajevo and Kotor (summer holiday).
    private static readonly (double Lat, double Lng) BanjaLuka = (44.7722, 17.1910);
    private static readonly (double Lat, double Lng) Sarajevo = (43.8563, 18.4131);
    private static readonly (double Lat, double Lng) Kotor = (42.4247, 18.7712);

    public static async Task SeedAsync(IServiceProvider rootServices)
    {
        using var scope = rootServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FairShareDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await db.Set<User>().AnyAsync(u => u.Email == MainEmail))
            return;

        var categories = await db.Set<Category>().ToListAsync();
        if (categories.Count == 0)
            throw new InvalidOperationException(
                "Демо подаци: у бази нема категорија. Прво додај категорије, па поново покрени апликацију.");

        var builder = new DemoBuilder(db, hasher, categories, DateTime.UtcNow);
        builder.Build();

        // Everything is saved in one transaction: either the whole demo exists or nothing does.
        await db.SaveChangesAsync();
    }

    /// <summary>Builds the whole data set in memory; the seeder saves it at the end.</summary>
    private sealed class DemoBuilder
    {
        private readonly FairShareDbContext _db;
        private readonly IPasswordHasher _hasher;
        private readonly Random _random = new(20261015);
        private readonly DateTime _now;
        private readonly DateTime _today;

        /// <summary>First days of the three months: two months ago, last month, this month.</summary>
        private readonly DateTime[] _months;

        private readonly Category _food, _coffee, _transport, _rent, _utilities, _fun, _health, _other;

        private User _jelena = null!, _marko = null!, _ana = null!, _nikola = null!,
            _tamara = null!, _vuk = null!, _milica = null!, _ognjen = null!;

        private readonly List<Expense> _jelenaExpenses = new();

        public DemoBuilder(FairShareDbContext db, IPasswordHasher hasher, List<Category> categories, DateTime nowUtc)
        {
            _db = db;
            _hasher = hasher;
            _now = nowUtc;
            _today = DateTime.SpecifyKind(nowUtc.Date, DateTimeKind.Utc);
            var thisMonth = new DateTime(_today.Year, _today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            _months = new[] { thisMonth.AddMonths(-2), thisMonth.AddMonths(-1), thisMonth };

            var other = Find(categories, "остал", "ostal", "other", "разно", "razno") ?? categories[0];
            _other = other;
            _food = Find(categories, "хран", "hran", "food") ?? other;
            _coffee = Find(categories, "каф", "kaf", "пић", "pić", "pic", "coffee") ?? _food;
            _transport = Find(categories, "превоз", "prevoz", "транспорт", "transport") ?? other;
            _utilities = Find(categories, "режиј", "režij", "rezij", "рачун", "račun", "racun", "utilit") ?? other;
            _rent = Find(categories, "станар", "stanar", "кириј", "kirij", "rent") ?? _utilities;
            _fun = Find(categories, "забав", "zabav", "entertain") ?? other;
            _health = Find(categories, "здрав", "zdrav", "апотек", "health") ?? other;
        }

        public void Build()
        {
            CreateUsers();
            CreateCards();
            CreatePersonalExpenses();
            CreateBudgets();
            CreateFlatGroup();
            CreateHolidayGroup();
            CreateTaraGroup();
            CreateBirthdayGroup();
            CreateLoginHistory();
        }

        // ---------- users ----------

        private void CreateUsers()
        {
            var hash = _hasher.HashPassword(DemoPassword);
            User NewUser(string first, string last, string email, int daysBeforeStart, string? account)
            {
                var user = new User
                {
                    FirstName = first,
                    LastName = last,
                    Email = User.NormalizeEmail(email),
                    PasswordHash = hash,
                    DefaultCurrency = "BAM",
                    BankAccountNumber = account,
                    Role = UserRole.Customer,
                    CreatedAt = _months[0].AddDays(-daysBeforeStart).AddHours(10)
                };
                _db.Add(user);
                return user;
            }

            _jelena = NewUser("Јелена", "Савић", MainEmail, 14, BankAccount("55100000123456"));
            _marko = NewUser("Марко", "Јовић", "marko@demo.fairshare.local", 12, BankAccount("56200000234567"));
            _ana = NewUser("Ана", "Петровић", "ana@demo.fairshare.local", 10, BankAccount("13800000345678"));
            _nikola = NewUser("Никола", "Ђурић", "nikola@demo.fairshare.local", 3, BankAccount("55400000456789"));
            _tamara = NewUser("Тамара", "Кос", "tamara@demo.fairshare.local", 1, BankAccount("56700000567890"));
            _vuk = NewUser("Вук", "Марић", "vuk@demo.fairshare.local", -20, BankAccount("15400000678901"));
            _milica = NewUser("Милица", "Ристић", "milica@demo.fairshare.local", -40, null);
            _ognjen = NewUser("Огњен", "Лазић", "ognjen@demo.fairshare.local", -25, null);
            _ognjen.IsBlocked = true;
        }

        /// <summary>
        /// 16-digit account number in the BiH format: 14 digits + two control digits
        /// (ISO 7064, MOD 97-10), so the QR code and any account check accept it.
        /// </summary>
        private static string BankAccount(string first14)
        {
            var remainder = 0;
            foreach (var digit in first14 + "00")
                remainder = (remainder * 10 + (digit - '0')) % 97;
            return first14 + (98 - remainder).ToString("00");
        }

        // ---------- saved cards (simulator tokens) ----------

        private PaymentCard _jelenaVisa = null!, _tamaraVisa = null!, _markoMastercard = null!;

        private void CreateCards()
        {
            PaymentCard Card(User owner, string outcome, string brand, string last4)
            {
                var card = new PaymentCard
                {
                    UserId = owner.Id,
                    // Sandbox test token of the payment simulator ("tok_test_<outcome>_<random>"):
                    // the simulator registers it on the first charge, so no card number is needed.
                    Token = $"tok_test_{outcome}_{Guid.NewGuid():N}"[..(10 + outcome.Length + 24)],
                    Brand = brand,
                    Last4Digits = last4,
                    ExpiryDate = new DateTime(_today.Year + 2, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    CreatedAt = owner.CreatedAt.AddDays(2)
                };
                _db.Add(card);
                return card;
            }

            _jelenaVisa = Card(_jelena, "ok", "Visa", "4242");
            // A second card of the main user that the simulator declines - to show a failed payment.
            Card(_jelena, "insufficient_funds", "Visa", "9995");
            _tamaraVisa = Card(_tamara, "ok", "Visa", "4242");
            _markoMastercard = Card(_marko, "ok", "Mastercard", "4444");
        }

        // ---------- personal expenses ----------

        private void CreatePersonalExpenses()
        {
            for (var m = 0; m < _months.Length; m++)
            {
                var month = _months[m];

                // Rent and internet are recurring: the first month is the template, later months
                // are the copies the background job would have made.
                AddRecurring(_jelena, _rent, 350m, "Станарина, Пајсијева 7", month, 1, m == 0);
                AddRecurring(_jelena, _utilities, 39.90m, "Интернет", month, 6, m == 0);

                AddFixed(_jelena, _utilities, Amount(45, 78), "Струја", month, 10);
                AddFixed(_jelena, _utilities, Amount(14, 24), "Вода", month, 15);
                AddFixed(_jelena, _transport, 40m, "Мјесечна карта", month, 2);

                Habit(_jelena, _food, new[] { "Намирнице, Бинго", "Намирнице, Конзум", "Пијаца" }, 24, 85, 6, month, 0.5, BanjaLuka);
                Habit(_jelena, _food, new[] { "Пекара на углу", "Пекара" }, 2, 6, 5, month, 0.4, BanjaLuka);
                Habit(_jelena, _food, new[] { "Ручак са колегама", "Пица петком", "Вечера у граду" }, 15, 45, 3, month, 0.8, BanjaLuka);
                Habit(_jelena, _coffee, new[] { "Кафа", "Кафа и колач", "Кафа са Аном" }, 2.5, 7.5, 9, month, 0.6, BanjaLuka);
                Habit(_jelena, _transport, new[] { "Такси" }, 6, 16, 2, month, 0.7, BanjaLuka);
                Habit(_jelena, _fun, new[] { "Биоскоп", "Концерт", "Књига", "Позориште" }, 8, 40, 2, month, 0.6, BanjaLuka);
                Habit(_jelena, _health, new[] { "Апотека" }, 8, 35, 1, month, 0.5, BanjaLuka);
                Habit(_jelena, _other, new[] { "Одјећа", "Фризер", "Поклон за маму" }, 20, 80, 1, month, 0.3, BanjaLuka);

                // A conference in Sarajevo last month.
                if (m == 1)
                {
                    AddFixed(_jelena, _transport, 38m, "Аутобус до Сарајева", month, 18, Sarajevo);
                    AddFixed(_jelena, _food, 27.5m, "Ћевапи на Башчаршији", month, 18, Sarajevo);
                    AddFixed(_jelena, _coffee, 6m, "Кафа у Сарајеву", month, 19, Sarajevo);
                }

                // Summer holiday: personal expenses in euros (they are not added to KM amounts).
                if (m == 0)
                {
                    AddFixed(_jelena, _food, 24.5m, "Ручак у Котору", month, 7, Kotor, "EUR");
                    AddFixed(_jelena, _coffee, 4m, "Сладолед и кафа", month, 8, Kotor, "EUR");
                    AddFixed(_jelena, _other, 35m, "Сувенири", month, 10, Kotor, "EUR");
                }

                // Lighter history for two other users, so the admin statistics have more than one user.
                Habit(_marko, _food, new[] { "Намирнице", "Ручак" }, 15, 60, 5, month, 0.4, BanjaLuka);
                Habit(_marko, _coffee, new[] { "Кафа" }, 2.5, 5, 5, month, 0.4, BanjaLuka);
                Habit(_marko, _transport, new[] { "Гориво" }, 40, 90, 2, month, 0.5, BanjaLuka);
                Habit(_ana, _food, new[] { "Намирнице", "Пекара" }, 5, 55, 5, month, 0.4, BanjaLuka);
                Habit(_ana, _fun, new[] { "Теретана", "Биоскоп" }, 10, 45, 2, month, 0.3, BanjaLuka);
                Habit(_ana, _health, new[] { "Апотека" }, 6, 30, 1, month, 0.3, BanjaLuka);
            }
        }

        private void AddRecurring(User user, Category category, decimal amount, string description,
            DateTime month, int day, bool isTemplate)
        {
            var expense = AddFixed(user, category, amount, description, month, day);
            if (expense is null || !isTemplate)
                return;

            expense.IsRecurring = true;
            expense.RecurrenceInterval = RecurrenceInterval.Monthly;
            // The next copy is due next month, so the background job does not add anything now.
            var next = new DateTime(_today.Year, _today.Month, Math.Min(day, 28), 0, 0, 0, DateTimeKind.Utc);
            expense.NextOccurrenceDate = next > _today ? next : next.AddMonths(1);
        }

        private Expense? AddFixed(User user, Category category, decimal amount, string description,
            DateTime month, int day, (double Lat, double Lng)? place = null, string currency = "BAM")
        {
            var date = month.AddDays(day - 1);
            if (date > _today)
                return null;

            var location = place is { } p ? Jitter(p) : ((double, double)?)null;
            return AddExpense(user, category, amount, description, date, location, currency);
        }

        /// <summary>Something done several times a month on random days, with or without a location.</summary>
        private void Habit(User user, Category category, string[] descriptions, double min, double max,
            int timesPerMonth, DateTime month, double locationChance, (double Lat, double Lng) place)
        {
            var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
            for (var i = 0; i < timesPerMonth; i++)
            {
                var date = month.AddDays(_random.Next(daysInMonth));
                if (date > _today)
                    continue;

                var location = _random.NextDouble() < locationChance ? Jitter(place) : ((double, double)?)null;
                AddExpense(user, category, Amount(min, max), descriptions[_random.Next(descriptions.Length)],
                    date, location, "BAM");
            }
        }

        private Expense AddExpense(User user, Category category, decimal amount, string description,
            DateTime date, (double Lat, double Lng)? location, string currency)
        {
            var expense = new Expense
            {
                UserId = user.Id,
                CategoryId = category.Id,
                Amount = amount,
                Currency = currency,
                Date = date,
                Description = description,
                Latitude = location?.Lat,
                Longitude = location?.Lng,
                IsRecurring = false,
                RecurrenceInterval = RecurrenceInterval.None,
                CreatedAt = Moment(date)
            };
            _db.Add(expense);
            Audit(user, "Expense.Create", "Expense", expense.Id, expense.CreatedAt);
            if (user == _jelena)
                _jelenaExpenses.Add(expense);
            return expense;
        }

        // ---------- budgets (main user) ----------

        private void CreateBudgets()
        {
            decimal Spent(Category category, DateTime month) => _jelenaExpenses
                .Where(e => e.CategoryId == category.Id && e.Currency == "BAM"
                            && e.Date.Year == month.Year && e.Date.Month == month.Month)
                .Sum(e => e.Amount);

            void Budget(Category category, DateTime month, decimal limit)
            {
                _db.Add(new Budget
                {
                    UserId = _jelena.Id,
                    CategoryId = category.Id,
                    Month = month.ToString("yyyy-MM"),
                    MonthlyLimit = Math.Max(10m, limit),
                    CreatedAt = month.AddHours(9)
                });
            }

            static decimal Up10(decimal value) => Math.Ceiling(value / 10m) * 10m;
            static decimal Down10(decimal value) => Math.Floor(value / 10m) * 10m;

            var current = _months[2];
            var budgeted = new[] { _food, _coffee, _transport, _utilities, _fun }.Distinct().ToList();

            // Previous months: mostly within the limit, coffee slightly over in the first month.
            foreach (var month in _months.Take(2))
            {
                foreach (var category in budgeted)
                {
                    var factor = category == _coffee && month == _months[0] ? 0.9m : 1.12m;
                    Budget(category, month, Up10(Spent(category, month) * factor));
                }
            }

            // This month: food close to the limit, coffee over it, the rest comfortably within.
            var foodLimit = Up10(Spent(_food, current) / 0.86m);
            var coffeeLimit = Down10(Spent(_coffee, current) * 0.85m);
            foreach (var category in budgeted)
            {
                var limit = category == _food ? foodLimit
                    : category == _coffee ? coffeeLimit
                    : Up10(Spent(category, current) / 0.55m + 20m);
                Budget(category, current, limit);
            }

            var period = NotificationText.Month(current);
            Notify(_jelena, NotificationType.BudgetExceeded,
                $"Потрошено је 80% буџета за категорију {NotificationText.Quote(_food.Name)} за {period} (лимит {NotificationText.Money(foodLimit, "BAM")}).",
                _today.AddHours(-30), isRead: true);
            Notify(_jelena, NotificationType.BudgetExceeded,
                $"Буџет за категорију {NotificationText.Quote(_coffee.Name)} за {period} је потрошен: достигнуто је 100% лимита од {NotificationText.Money(Math.Max(10m, coffeeLimit), "BAM")}.",
                _today.AddHours(-5), isRead: false);

            var rent = _jelenaExpenses.FirstOrDefault(e => e.Date == current && e.CategoryId == _rent.Id);
            if (rent is not null)
            {
                Notify(_jelena, NotificationType.RecurringExpenseCreated,
                    $"Аутоматски је додат понављајући трошак {NotificationText.Quote(rent.Description!)}: {NotificationText.Money(rent.Amount, "BAM")} за {NotificationText.Date(rent.Date)}.",
                    rent.Date.AddHours(3), isRead: true);
            }
        }

        // ---------- groups ----------

        /// <summary>Shared flat: utilities and groceries every month; settled at the start of this month.</summary>
        private void CreateFlatGroup()
        {
            var group = NewGroup("Стан Пајсијева 7", "Станарина, режије и намирнице за стан.", "BAM",
                _months[0].AddDays(-10), _jelena, _marko, _ana);
            var members = new[] { _jelena, _marko, _ana };

            for (var m = 0; m < _months.Length; m++)
            {
                var month = _months[m];
                // This month the others pay more often, so the main user owes something to show.
                var payers = m == 2 ? new[] { _marko, _ana, _marko } : new[] { _jelena, _marko, _ana };

                AddGroupExpense(group, payers[0], _food, Amount(35, 70), "Намирнице за стан", month, 3, members);
                AddGroupExpense(group, payers[1], _food, Amount(30, 65), "Намирнице за стан", month, 12, members);
                AddGroupExpense(group, payers[2], _food, Amount(25, 60), "Намирнице за стан", month, 21, members);
                AddGroupExpense(group, _marko, _utilities, Amount(60, 95), "Струја за стан", month, 9, members);
                AddGroupExpense(group, _ana, _utilities, 39.90m, "Интернет за стан", month, 6, members);
                AddGroupExpense(group, _jelena, _other, Amount(14, 26), "Средства за чишћење", month, 16, members);
                // Water by agreed shares: the main user has the largest room.
                AddGroupExpense(group, payers[1], _utilities, 30m, "Вода", month, 15, members,
                    SplitType.Percentage, percentages: new[] { 40.0, 30.0, 30.0 });

                // Everything up to the end of last month was settled on the 2nd of this month.
                if (m == 1)
                    SettleAll(group, members, _months[2].AddDays(1), cardPayments: 1);
            }
        }

        /// <summary>Summer holiday in Kotor, in euros; fully settled.</summary>
        private void CreateHolidayGroup()
        {
            var start = _months[0];
            var group = NewGroup("Љетовање Котор", "Седам дана на мору.", "EUR", start.AddDays(1), _tamara, _jelena, _marko);
            var members = new[] { _jelena, _marko, _tamara };

            AddGroupExpense(group, _tamara, _other, 540m, "Апартман у Котору", start, 5, members);
            AddGroupExpense(group, _marko, _transport, 82.40m, "Гориво и путарина", start, 5, members);
            AddGroupExpense(group, _jelena, _food, 96m, "Вечера у Старом граду", start, 7, members);
            AddGroupExpense(group, _marko, _food, 63.40m, "Намирнице", start, 8, members);
            AddGroupExpense(group, _tamara, _fun, 45m, "Брод до Пераста", start, 9, members);
            AddGroupExpense(group, _jelena, _fun, 24m, "Улазнице за тврђаву", start, 10, members,
                SplitType.ExactAmount, exactAmounts: new[] { 8m, 8m, 8m });

            SettleAll(group, members, start.AddDays(19), cardPayments: 1);
        }

        /// <summary>Weekend on Tara last month: others have settled, the main user still owes.</summary>
        private void CreateTaraGroup()
        {
            var start = _months[1];
            var group = NewGroup("Викенд на Тари", "Брвнара, рафтинг и шетња до Бање стијене.", "BAM",
                start.AddDays(5), _nikola, _jelena, _tamara, _vuk, _ana);
            var members = new[] { _nikola, _jelena, _tamara, _vuk, _ana };

            AddGroupExpense(group, _nikola, _other, 420m, "Смјештај, брвнара", start, 12, members);
            AddGroupExpense(group, _vuk, _transport, 110m, "Гориво", start, 12, members);
            AddGroupExpense(group, _tamara, _food, 96.50m, "Намирнице", start, 12, members);
            AddGroupExpense(group, _nikola, _food, 185m, "Вечера у ресторану", start, 13, members);
            // Only four of them went rafting.
            AddGroupExpense(group, _ana, _fun, 300m, "Рафтинг", start, 13, new[] { _nikola, _jelena, _tamara, _ana },
                SplitType.ExactAmount, exactAmounts: new[] { 75m, 75m, 75m, 75m });
            AddGroupExpense(group, _jelena, _food, 18m, "Сладолед", start, 14, members);

            // The suggestion is calculated a few days later. Everyone except the main user has
            // already paid; their part stays open so the card / QR payment can be shown.
            var proposedAt = start.AddDays(17);
            var transfers = Simplify(Balances(group, members));
            var settledAny = false;
            foreach (var (debtor, creditor, amount) in transfers)
            {
                var settle = debtor != _jelena;
                var transaction = AddSettlement(group, debtor, creditor, amount, proposedAt, settle);
                if (settle && !settledAny && debtor == _tamara)
                {
                    AddCardPayment(transaction, _tamaraVisa, proposedAt.AddDays(1));
                    settledAny = true;
                }

                if (!settle)
                {
                    Notify(_jelena, NotificationType.SettlementSuggested,
                        $"Нови приједлог поравнања у групи {NotificationText.Quote(group.Name)}: треба да уплатиш {NotificationText.Money(amount, group.Currency)}, прима {NotificationText.FullName(creditor)}.",
                        proposedAt.AddHours(1), isRead: false);
                }
            }
        }

        /// <summary>Birthday present, this month: still open, no suggestion yet.</summary>
        private void CreateBirthdayGroup()
        {
            var start = _months[2];
            var group = NewGroup("Рођендан за Милицу", "Поклон и торта.", "BAM", start.AddDays(2), _jelena, _ana, _vuk);
            var members = new[] { _jelena, _ana, _vuk };

            AddGroupExpense(group, _jelena, _other, 120m, "Поклон, наруквица", start, 4, members);
            AddGroupExpense(group, _ana, _food, 45m, "Торта", start, 6, members);
            AddGroupExpense(group, _jelena, _other, 22m, "Балони и украси", start, 6, members);
        }

        private Group NewGroup(string name, string description, string currency, DateTime createdAt,
            User owner, params User[] others)
        {
            var group = new Group
            {
                Name = name,
                Description = description,
                Currency = currency,
                CreatedAt = Moment(createdAt)
            };
            _db.Add(group);
            Audit(owner, "Group.CreateGroup", "Group", group.Id, group.CreatedAt);

            var joined = group.CreatedAt;
            AddMember(group, owner, GroupRole.Owner, joined);
            foreach (var user in others.Where(u => u != owner))
            {
                joined = joined.AddHours(2);
                AddMember(group, user, GroupRole.Member, joined);
                Notify(user, NotificationType.GroupInvite,
                    $"Сада си члан групе {NotificationText.Quote(name)} (позив од: {NotificationText.FullName(owner)}).",
                    joined, isRead: true);
            }

            return group;
        }

        private void AddMember(Group group, User user, GroupRole role, DateTime joinedAt)
        {
            var member = new GroupMember
            {
                GroupId = group.Id,
                Group = group,
                UserId = user.Id,
                User = user,
                Role = role,
                JoinedAt = joinedAt,
                CreatedAt = joinedAt
            };
            group.Members.Add(member);
        }

        private void AddGroupExpense(Group group, User payer, Category category, decimal amount, string description,
            DateTime month, int day, User[] participants, SplitType splitType = SplitType.Equal,
            double[]? percentages = null, decimal[]? exactAmounts = null)
        {
            var date = month.AddDays(day - 1);
            if (date > _today)
                return;

            var expense = new GroupExpense
            {
                GroupId = group.Id,
                Group = group,
                CategoryId = category.Id,
                PaidByUserId = payer.Id,
                PaidByUser = payer,
                Amount = amount,
                Description = description,
                Date = date,
                SplitType = splitType,
                CreatedAt = Moment(date)
            };

            var shares = splitType switch
            {
                SplitType.Percentage => Shares(amount, percentages!.Select(p => (decimal)p / 100m).ToArray()),
                SplitType.ExactAmount => exactAmounts!,
                _ => Shares(amount, Enumerable.Repeat(1m / participants.Length, participants.Length).ToArray())
            };

            for (var i = 0; i < participants.Length; i++)
            {
                expense.Splits.Add(new ExpenseSplit
                {
                    GroupExpenseId = expense.Id,
                    GroupExpense = expense,
                    UserId = participants[i].Id,
                    User = participants[i],
                    Amount = shares[i],
                    Percentage = splitType == SplitType.Percentage ? percentages![i] : null,
                    CreatedAt = expense.CreatedAt
                });
            }

            group.Expenses.Add(expense);
            Audit(payer, "GroupExpense.Create", "GroupExpense", expense.Id, expense.CreatedAt);

            // Recent expenses of others appear as (partly unread) notifications of the main user.
            var jelenaShare = expense.Splits.FirstOrDefault(s => s.UserId == _jelena.Id);
            if (payer != _jelena && jelenaShare is not null && date >= _today.AddDays(-12))
            {
                Notify(_jelena, NotificationType.GroupExpenseAdded,
                    $"Нови трошак у групи {NotificationText.Quote(group.Name)}: {NotificationText.Quote(description)}, укупно {NotificationText.Money(amount, group.Currency)}. Твој дио је {NotificationText.Money(jelenaShare.Amount, group.Currency)}.",
                    expense.CreatedAt, isRead: date < _today.AddDays(-4));
            }
        }

        /// <summary>Splits an amount by weights to the cent; the remainder goes to the first shares.</summary>
        private static decimal[] Shares(decimal amount, decimal[] weights)
        {
            var cents = (long)Math.Round(amount * 100m);
            var parts = weights.Select(w => (long)Math.Floor(cents * w)).ToArray();
            var remainder = cents - parts.Sum();
            for (var i = 0; remainder > 0; i = (i + 1) % parts.Length, remainder--)
                parts[i]++;
            return parts.Select(p => p / 100m).ToArray();
        }

        // ---------- settlements ----------

        /// <summary>Net balance of every member: paid minus own shares, corrected by settled transfers.</summary>
        private static Dictionary<User, decimal> Balances(Group group, User[] members)
        {
            var balances = members.ToDictionary(m => m, _ => 0m);
            foreach (var expense in group.Expenses)
            {
                balances[expense.PaidByUser] += expense.Amount;
                foreach (var split in expense.Splits)
                    balances[split.User] -= split.Amount;
            }

            foreach (var settlement in group.Settlements.Where(s => s.Status == SettlementStatus.Settled))
            {
                balances[settlement.DebtorUser] += settlement.Amount;
                balances[settlement.CreditorUser] -= settlement.Amount;
            }

            return balances;
        }

        /// <summary>Greedy debt simplification (largest debtor pays largest creditor), as in the app.</summary>
        private static List<(User Debtor, User Creditor, decimal Amount)> Simplify(Dictionary<User, decimal> balances)
        {
            var debtors = balances.Where(b => b.Value < -0.005m).Select(b => (Person: b.Key, Left: -b.Value))
                .OrderByDescending(b => b.Left).ToList();
            var creditors = balances.Where(b => b.Value > 0.005m).Select(b => (Person: b.Key, Left: b.Value))
                .OrderByDescending(b => b.Left).ToList();

            var result = new List<(User Debtor, User Creditor, decimal Amount)>();
            int d = 0, c = 0;
            while (d < debtors.Count && c < creditors.Count)
            {
                var amount = Math.Round(Math.Min(debtors[d].Left, creditors[c].Left), 2);
                if (amount > 0)
                    result.Add((debtors[d].Person, creditors[c].Person, amount));

                debtors[d] = (debtors[d].Person, debtors[d].Left - amount);
                creditors[c] = (creditors[c].Person, creditors[c].Left - amount);
                if (debtors[d].Left < 0.005m) d++;
                if (creditors[c].Left < 0.005m) c++;
            }

            return result;
        }

        /// <summary>Calculates the transfers and marks all of them settled; the first few paid by card.</summary>
        private void SettleAll(Group group, User[] members, DateTime settledAt, int cardPayments)
        {
            if (settledAt > _now)
                settledAt = _now.AddHours(-1);

            foreach (var (debtor, creditor, amount) in Simplify(Balances(group, members)))
            {
                var transaction = AddSettlement(group, debtor, creditor, amount, settledAt.AddDays(-2), settled: true);

                var card = debtor == _jelena ? _jelenaVisa : debtor == _tamara ? _tamaraVisa : debtor == _marko ? _markoMastercard : null;
                if (cardPayments > 0 && card is not null)
                {
                    AddCardPayment(transaction, card, settledAt);
                    cardPayments--;
                }
                else
                {
                    Audit(creditor, "Settlement.MarkSettled", "Settlement", transaction.Id, settledAt.AddHours(2));
                }

                if (creditor == _jelena)
                {
                    Notify(_jelena, NotificationType.SettlementCompleted,
                        $"Поравнање у групи {NotificationText.Quote(group.Name)} је означено као измирено: {NotificationText.FullName(debtor)} → {NotificationText.FullName(creditor)}, {NotificationText.Money(amount, group.Currency)}.",
                        settledAt.AddHours(2), isRead: true);
                }
            }
        }

        private SettlementTransaction AddSettlement(Group group, User debtor, User creditor, decimal amount,
            DateTime createdAt, bool settled)
        {
            var transaction = new SettlementTransaction
            {
                GroupId = group.Id,
                Group = group,
                DebtorUserId = debtor.Id,
                DebtorUser = debtor,
                CreditorUserId = creditor.Id,
                CreditorUser = creditor,
                Amount = amount,
                Status = settled ? SettlementStatus.Settled : SettlementStatus.Proposed,
                CreatedAt = createdAt
            };
            transaction.GenerateQrCode();
            group.Settlements.Add(transaction);
            Audit(debtor, "Settlement.Generate", "Settlement", transaction.Id, createdAt);
            return transaction;
        }

        private void AddCardPayment(SettlementTransaction transaction, PaymentCard card, DateTime paidAt)
        {
            transaction.Payment = new Payment
            {
                SettlementTransactionId = transaction.Id,
                SettlementTransaction = transaction,
                PaymentCardId = card.Id,
                PaymentCard = card,
                IdempotencyKey = $"demo-{Guid.NewGuid():N}",
                Status = PaymentStatus.Succeeded,
                CreatedAt = paidAt,
                SubmittedAt = paidAt,
                CompletedAt = paidAt.AddSeconds(3)
            };
            Audit(transaction.DebtorUser, "Payment.PayWithCard", "Payment", transaction.Id, paidAt);

            var amount = NotificationText.Money(transaction.Amount, transaction.Group.Currency);
            var groupName = NotificationText.Quote(transaction.Group.Name);
            Notify(transaction.CreditorUser, NotificationType.PaymentConfirmation,
                $"Стигла је уплата картицом у групи {groupName}: {NotificationText.FullName(transaction.DebtorUser)}, {amount}. Поравнање је измирено.",
                paidAt, isRead: true);
            Notify(transaction.DebtorUser, NotificationType.PaymentConfirmation,
                $"Плаћање картицом {card.Brand} •••• {card.Last4Digits} је успјело: {amount} за {NotificationText.FullName(transaction.CreditorUser)} у групи {groupName}.",
                paidAt, isRead: true);
        }

        // ---------- notifications and audit log ----------

        private void Notify(User user, NotificationType type, string message, DateTime createdAt, bool isRead)
        {
            if (createdAt > _now)
                createdAt = _now.AddMinutes(-_random.Next(5, 120));

            _db.Add(new Notification
            {
                UserId = user.Id,
                Type = type,
                Message = message,
                IsRead = isRead,
                CreatedAt = createdAt
            });
        }

        private void Audit(User user, string action, string entityType, Guid entityId, DateTime timestamp)
        {
            _db.Add(new AuditLog
            {
                UserId = user.Id,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Timestamp = timestamp > _now ? _now : timestamp
            });
        }

        /// <summary>A few sign-ins per week for the active users (seen in the admin audit log).</summary>
        private void CreateLoginHistory()
        {
            foreach (var user in new[] { _jelena, _marko, _ana, _nikola, _tamara, _vuk, _milica })
            {
                var day = user.CreatedAt.Date;
                while (day <= _today)
                {
                    Audit(user, "Auth.Login", "User", user.Id, Moment(day));
                    day = day.AddDays(_random.Next(2, user == _jelena ? 4 : 8));
                }
            }
        }

        // ---------- small helpers ----------

        private decimal Amount(double min, double max)
            => Math.Round((decimal)(min + _random.NextDouble() * (max - min)), 1);

        /// <summary>A time during the day (8-21 h), never in the future.</summary>
        private DateTime Moment(DateTime date)
        {
            var moment = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc)
                .AddHours(8 + _random.Next(13)).AddMinutes(_random.Next(60));
            return moment > _now ? _now.AddMinutes(-_random.Next(5, 90)) : moment;
        }

        /// <summary>A point within about a kilometre of the place, so the map shows a spread.</summary>
        private (double Lat, double Lng) Jitter((double Lat, double Lng) place)
            => (Math.Round(place.Lat + (_random.NextDouble() - 0.5) * 0.02, 5),
                Math.Round(place.Lng + (_random.NextDouble() - 0.5) * 0.03, 5));

        private static Category? Find(List<Category> categories, params string[] fragments)
            => categories.FirstOrDefault(c => fragments.Any(f => c.Name.ToLowerInvariant().Contains(f)));
    }
}
