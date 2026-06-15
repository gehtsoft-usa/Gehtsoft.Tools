using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Gehtsoft.ExpressionToJs.Tests;
using Jint;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>
    /// End-to-end "realistic validation" scenario for a rich entity with every data category:
    /// strings, integers, nullable integers, doubles, decimals, booleans, enums, arrays, dates,
    /// and <b>folded (nested) entities</b> (a postal <see cref="Address"/> and a
    /// <see cref="CreditCard"/> with number / holder / expiration).
    /// <para>
    /// Each rule is the kind of predicate a form-validation layer carries: the same C# lambda runs
    /// server-side (compiled delegate) and client-side (emitted JS). We compile every rule through
    /// <see cref="ValidationExpressionCompiler"/> - which maps the entity parameter onto
    /// <c>reference('path')</c> and the property value onto <c>value</c> - and then evaluate it in
    /// Jint using the same <c>reference()</c> / <c>value</c> / <c>index</c> runtime the real consumer
    /// (Gehtsoft.EF.Toolbox) binds in the browser. Every case is checked three ways: the C# delegate
    /// against the expected verdict, the emitted JS against the expected verdict, and the two against
    /// each other.
    /// </para>
    /// </summary>
    public class ProfileValidationScenarioTests
    {
        // ----------------------------------------------------------------- entity model

        public enum AccountTier
        {
            Free = 0,
            Standard = 1,
            Premium = 2,
        }

        public sealed class Address
        {
            public string Street { get; set; }
            public string City { get; set; }
            public string Country { get; set; }      // ISO-3166 alpha-2
            public string PostalCode { get; set; }
        }

        public sealed class CreditCard
        {
            public string Number { get; set; }
            public string HolderName { get; set; }
            public DateTime Expiration { get; set; }  // last day the card is valid
        }

        public sealed class UserProfile
        {
            // strings
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Bio { get; set; }

            // dates
            public DateTime BirthDate { get; set; }
            public DateTime RegisteredAt { get; set; }
            public DateTime? LastLoginAt { get; set; }

            // numbers
            public int LoginCount { get; set; }
            public int? ReferrerId { get; set; }
            public double AccountBalance { get; set; }
            public decimal CreditLimit { get; set; }

            // booleans
            public bool AcceptedTerms { get; set; }
            public bool IsActive { get; set; }

            // enum
            public AccountTier Tier { get; set; }

            // collections (arrays - the stub exposes .length over CLR arrays)
            public string[] Roles { get; set; }
            public int[] FavoriteNumbers { get; set; }

            // folded (nested) entities
            public Address Address { get; set; }
            public CreditCard Card { get; set; }
        }

        /// <summary>A profile that satisfies every rule in this file.</summary>
        private static UserProfile ValidProfile() => new UserProfile
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Phone = "+1 555 0100",
            Bio = "Mathematician.",
            BirthDate = new DateTime(1990, 6, 15),
            RegisteredAt = new DateTime(2020, 1, 10, 9, 30, 0),
            LastLoginAt = new DateTime(2024, 3, 1, 8, 0, 0),
            LoginCount = 42,
            ReferrerId = 7,
            AccountBalance = 125.50,
            CreditLimit = 5000m,
            AcceptedTerms = true,
            IsActive = true,
            Tier = AccountTier.Premium,
            Roles = new[] { "user", "editor" },
            FavoriteNumbers = new[] { 3, 7, 21 },
            Address = new Address { Street = "1 Analytical Way", City = "London", Country = "GB", PostalCode = "12345" },
            Card = new CreditCard { Number = "4444333322221111", HolderName = "ADA LOVELACE", Expiration = new DateTime(2099, 12, 31) },
        };

        // ----------------------------------------------------------------- JS harness

        // The browser-side runtime the generated expressions rely on: reference('path') walks the
        // bound model, value is the property under test, index/element_at support per-element rules.
        // Mirrors Gehtsoft.EF.Toolbox's JsRuleExecutor.
        private const string ReferenceRuntime = @"
var index = 0;
var value = null;
function reference(path) {
    if (path === undefined || path === null || path === '')
        return __model;
    var parts = path.split('.');
    var current = __model;
    for (var i = 0; i < parts.length; i++) {
        if (current === null || current === undefined)
            return current;
        current = current[parts[i]];
    }
    return current;
}";

        private static Engine NewEngine(UserProfile model)
        {
            var engine = new Engine();
            engine.Execute(ExpressionToJsStubAccessor.GetJsIncludesAsString());
            engine.SetValue("__model", model);
            engine.Execute(ReferenceRuntime);
            return engine;
        }

        /// <summary>
        /// Differential check of an entity-level rule (<c>p =&gt; condition(p)</c>): the parameter is
        /// the whole entity, so member access becomes <c>reference('Member.Sub')</c>.
        /// </summary>
        private static void AssertEntityRule(Expression<Func<UserProfile, bool>> rule, UserProfile model, bool expected)
        {
            bool csharp;
            try { csharp = rule.Compile()(model); }
            catch (Exception ex) { Assert.Fail($"C# evaluation threw {ex.GetType().Name}: {ex.Message}"); return; }
            Assert.True(csharp == expected, $"C#: expected {expected} but got {csharp} for `{rule}`");

            string js;
            try { js = new ValidationExpressionCompiler(rule, entityParameterIndex: 0).JavaScriptExpression; }
            catch (Exception ex) { Assert.Fail($"JS compilation threw {ex.GetType().Name}: {ex.Message} for `{rule}`"); return; }

            bool jsResult = EvalBool(js, model, $"entity rule `{rule}` -> `{js}`");
            Assert.True(jsResult == expected, $"JS `{js}`: expected {expected} but got {jsResult}");
            Assert.True(csharp == jsResult, $"C#/JS divergence for `{rule}` -> `{js}`: C#={csharp}, JS={jsResult}");
        }

        /// <summary>
        /// Differential check of a property-level rule (<c>v =&gt; condition(v)</c>) targeting
        /// <paramref name="targetPath"/>: the parameter is the property value, so it becomes the
        /// ambient <c>value</c> the host binds before evaluating the rule.
        /// </summary>
        private static void AssertValueRule<TValue>(string targetPath, Expression<Func<TValue, bool>> rule, Func<UserProfile, TValue> selector, UserProfile model, bool expected)
        {
            bool csharp;
            try { csharp = rule.Compile()(selector(model)); }
            catch (Exception ex) { Assert.Fail($"C# evaluation threw {ex.GetType().Name}: {ex.Message}"); return; }
            Assert.True(csharp == expected, $"C#: expected {expected} but got {csharp} for `{rule}`");

            string js;
            try { js = new ValidationExpressionCompiler(rule, valueParameterIndex: 0).JavaScriptExpression; }
            catch (Exception ex) { Assert.Fail($"JS compilation threw {ex.GetType().Name}: {ex.Message} for `{rule}`"); return; }

            var engine = NewEngine(model);
            engine.Execute($"value = reference('{targetPath}')");
            bool jsResult = EvalBoolOn(engine, js, $"value rule `{rule}` -> `{js}`");
            Assert.True(jsResult == expected, $"JS `{js}`: expected {expected} but got {jsResult}");
            Assert.True(csharp == jsResult, $"C#/JS divergence for `{rule}` -> `{js}`: C#={csharp}, JS={jsResult}");
        }

        private static bool EvalBool(string js, UserProfile model, string context)
            => EvalBoolOn(NewEngine(model), js, context);

        private static bool EvalBoolOn(Engine engine, string js, string context)
        {
            try { return Convert.ToBoolean(engine.Evaluate($"!!({js})").ToObject()); }
            catch (Exception ex) { Assert.Fail($"{context}: JavaScript failed to evaluate - {ex.GetType().Name}: {ex.Message}"); return false; }
        }

        // ----------------------------------------------------------------- string rules

        [Fact]
        public void FirstName_RequiredAndBounded()
        {
            Expression<Func<UserProfile, bool>> rule =
                p => !string.IsNullOrWhiteSpace(p.FirstName) && p.FirstName.Length <= 50;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile blank = ValidProfile(); blank.FirstName = "   ";
            AssertEntityRule(rule, blank, false);

            UserProfile nil = ValidProfile(); nil.FirstName = null;
            AssertEntityRule(rule, nil, false);

            UserProfile tooLong = ValidProfile(); tooLong.FirstName = new string('x', 51);
            AssertEntityRule(rule, tooLong, false);
        }

        [Fact]
        public void Email_MatchesPattern()
        {
            Expression<Func<UserProfile, bool>> rule =
                p => Regex.IsMatch(p.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile bad = ValidProfile(); bad.Email = "not-an-email";
            AssertEntityRule(rule, bad, false);

            UserProfile noDomain = ValidProfile(); noDomain.Email = "ada@localhost";
            AssertEntityRule(rule, noDomain, false);
        }

        [Fact]
        public void Bio_OptionalButBounded()
        {
            // optional: empty is fine, but if present it must be short enough
            Expression<Func<UserProfile, bool>> rule =
                p => string.IsNullOrEmpty(p.Bio) || p.Bio.Length <= 200;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile empty = ValidProfile(); empty.Bio = "";
            AssertEntityRule(rule, empty, true);

            UserProfile huge = ValidProfile(); huge.Bio = new string('b', 201);
            AssertEntityRule(rule, huge, false);
        }

        // ----------------------------------------------------------------- numeric rules

        [Fact]
        public void LoginCount_NonNegative()
        {
            Expression<Func<UserProfile, bool>> rule = p => p.LoginCount >= 0;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile negative = ValidProfile(); negative.LoginCount = -1;
            AssertEntityRule(rule, negative, false);
        }

        [Fact]
        public void ReferrerId_OptionalButPositive()
        {
            Expression<Func<UserProfile, bool>> rule =
                p => !p.ReferrerId.HasValue || p.ReferrerId.Value > 0;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile none = ValidProfile(); none.ReferrerId = null;
            AssertEntityRule(rule, none, true);

            UserProfile bad = ValidProfile(); bad.ReferrerId = 0;
            AssertEntityRule(rule, bad, false);
        }

        [Fact]
        public void AccountBalance_NotOverdrawn()
        {
            Expression<Func<UserProfile, bool>> rule = p => p.AccountBalance >= 0.0;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile overdrawn = ValidProfile(); overdrawn.AccountBalance = -0.01;
            AssertEntityRule(rule, overdrawn, false);
        }

        [Fact]
        public void CreditLimit_WithinDecimalRange()
        {
            Expression<Func<UserProfile, bool>> rule =
                p => p.CreditLimit >= 0m && p.CreditLimit <= 1_000_000m;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile negative = ValidProfile(); negative.CreditLimit = -1m;
            AssertEntityRule(rule, negative, false);

            UserProfile excessive = ValidProfile(); excessive.CreditLimit = 1_000_001m;
            AssertEntityRule(rule, excessive, false);
        }

        // ----------------------------------------------------------------- boolean rules

        [Fact]
        public void AcceptedTerms_MustBeTrue()
        {
            Expression<Func<UserProfile, bool>> rule = p => p.AcceptedTerms;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile declined = ValidProfile(); declined.AcceptedTerms = false;
            AssertEntityRule(rule, declined, false);
        }

        [Fact]
        public void ActiveAccount_RequiresAcceptedTerms()
        {
            // cross-field boolean rule: an inactive account need not have accepted terms
            Expression<Func<UserProfile, bool>> rule = p => !p.IsActive || p.AcceptedTerms;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile activeNoTerms = ValidProfile();
            activeNoTerms.IsActive = true; activeNoTerms.AcceptedTerms = false;
            AssertEntityRule(rule, activeNoTerms, false);

            UserProfile inactiveNoTerms = ValidProfile();
            inactiveNoTerms.IsActive = false; inactiveNoTerms.AcceptedTerms = false;
            AssertEntityRule(rule, inactiveNoTerms, true);
        }

        // ----------------------------------------------------------------- date rules

        [Fact]
        public void BirthDate_AtLeast21YearsOld()
        {
            // Functions.YearsSince(today, birth) -> age; mirrored by jsv_yearssince.
            Expression<Func<UserProfile, bool>> rule =
                p => Functions.YearsSince(DateTime.Today, p.BirthDate) >= 21;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile child = ValidProfile();
            child.BirthDate = DateTime.Today.AddYears(-10);
            AssertEntityRule(rule, child, false);
        }

        [Fact]
        public void RegisteredAt_NotInTheFuture()
        {
            Expression<Func<UserProfile, bool>> rule = p => p.RegisteredAt <= DateTime.Now;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile future = ValidProfile();
            future.RegisteredAt = DateTime.Now.AddYears(5);
            AssertEntityRule(rule, future, false);
        }

        [Fact]
        public void LastLogin_OptionalButAfterRegistration()
        {
            Expression<Func<UserProfile, bool>> rule =
                p => !p.LastLoginAt.HasValue || p.LastLoginAt.Value >= p.RegisteredAt;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile never = ValidProfile(); never.LastLoginAt = null;
            AssertEntityRule(rule, never, true);

            UserProfile beforeReg = ValidProfile();
            beforeReg.LastLoginAt = beforeReg.RegisteredAt.AddDays(-1);
            AssertEntityRule(rule, beforeReg, false);
        }

        // ----------------------------------------------------------------- enum rules

        [Fact]
        public void Tier_IsADefinedValue()
        {
            Expression<Func<UserProfile, bool>> rule =
                p => p.Tier == AccountTier.Free || p.Tier == AccountTier.Standard || p.Tier == AccountTier.Premium;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile bogus = ValidProfile(); bogus.Tier = (AccountTier)99;
            AssertEntityRule(rule, bogus, false);
        }

        [Fact]
        public void PremiumTier_RequiresACard()
        {
            Expression<Func<UserProfile, bool>> rule =
                p => p.Tier != AccountTier.Premium || p.Card != null;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile premiumNoCard = ValidProfile();
            premiumNoCard.Tier = AccountTier.Premium; premiumNoCard.Card = null;
            AssertEntityRule(rule, premiumNoCard, false);

            UserProfile freeNoCard = ValidProfile();
            freeNoCard.Tier = AccountTier.Free; freeNoCard.Card = null;
            AssertEntityRule(rule, freeNoCard, true);
        }

        // ----------------------------------------------------------------- collection rules

        [Fact]
        public void Roles_BetweenOneAndFive()
        {
            Expression<Func<UserProfile, bool>> rule =
                p => p.Roles != null && p.Roles.Length >= 1 && p.Roles.Length <= 5;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile none = ValidProfile(); none.Roles = new string[0];
            AssertEntityRule(rule, none, false);

            UserProfile many = ValidProfile();
            many.Roles = new[] { "a", "b", "c", "d", "e", "f" };
            AssertEntityRule(rule, many, false);
        }

        [Fact]
        public void Roles_AllNonEmpty()
        {
            Expression<Func<UserProfile, bool>> rule =
                p => p.Roles.All(r => !string.IsNullOrWhiteSpace(r));

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile hasBlank = ValidProfile();
            hasBlank.Roles = new[] { "user", "  " };
            AssertEntityRule(rule, hasBlank, false);
        }

        [Fact]
        public void Roles_ContainUser()
        {
            Expression<Func<UserProfile, bool>> rule = p => p.Roles.Contains("user");

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile noUser = ValidProfile();
            noUser.Roles = new[] { "editor", "admin" };
            AssertEntityRule(rule, noUser, false);
        }

        [Fact]
        public void FavoriteNumbers_AllPositive()
        {
            Expression<Func<UserProfile, bool>> rule =
                p => p.FavoriteNumbers.All(n => n > 0);

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile hasZero = ValidProfile();
            hasZero.FavoriteNumbers = new[] { 1, 0, 5 };
            AssertEntityRule(rule, hasZero, false);
        }

        // ----------------------------------------------------------------- nested (folded) entity rules

        [Fact]
        public void Address_PostalCodeIsFiveDigits()
        {
            Expression<Func<UserProfile, bool>> rule =
                p => Regex.IsMatch(p.Address.PostalCode, @"^\d{5}$");

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile bad = ValidProfile();
            bad.Address.PostalCode = "ABC";
            AssertEntityRule(rule, bad, false);
        }

        [Fact]
        public void Address_CountryIsTwoLetterCode()
        {
            Expression<Func<UserProfile, bool>> rule = p => p.Address.Country.Length == 2;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile bad = ValidProfile();
            bad.Address.Country = "United Kingdom";
            AssertEntityRule(rule, bad, false);
        }

        [Fact]
        public void Card_NumberPassesLuhnCheck()
        {
            Expression<Func<UserProfile, bool>> rule =
                p => Functions.IsCreditCardNumberCorrect(p.Card.Number);

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile bad = ValidProfile();
            bad.Card.Number = "4444333322221112";   // last digit broken
            AssertEntityRule(rule, bad, false);
        }

        [Fact]
        public void Card_HolderNameRequired()
        {
            Expression<Func<UserProfile, bool>> rule =
                p => !string.IsNullOrWhiteSpace(p.Card.HolderName);

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile blank = ValidProfile();
            blank.Card.HolderName = "   ";
            AssertEntityRule(rule, blank, false);
        }

        [Fact]
        public void Card_NotExpired()
        {
            Expression<Func<UserProfile, bool>> rule = p => p.Card.Expiration >= DateTime.Today;

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile expired = ValidProfile();
            expired.Card.Expiration = DateTime.Today.AddYears(-1);
            AssertEntityRule(rule, expired, false);
        }

        // ----------------------------------------------------------------- composite rule

        [Fact]
        public void PremiumCardHolder_FullCrossFieldRule()
        {
            // A premium account must carry a valid, unexpired card whose holder is named.
            Expression<Func<UserProfile, bool>> rule = p =>
                p.Tier != AccountTier.Premium ||
                (p.Card != null
                    && Functions.IsCreditCardNumberCorrect(p.Card.Number)
                    && !string.IsNullOrWhiteSpace(p.Card.HolderName)
                    && p.Card.Expiration >= DateTime.Today);

            AssertEntityRule(rule, ValidProfile(), true);

            UserProfile badCard = ValidProfile();
            badCard.Card.Number = "1234";
            AssertEntityRule(rule, badCard, false);

            UserProfile downgraded = ValidProfile();
            downgraded.Tier = AccountTier.Free;
            downgraded.Card.Number = "1234";        // irrelevant once not premium
            AssertEntityRule(rule, downgraded, true);
        }

        // ----------------------------------------------------------------- value-binding rules

        [Fact]
        public void Email_AsValueRule()
        {
            // property-level rule: the parameter is the value of Email, bound by the host as `value`.
            Expression<Func<string, bool>> rule =
                v => Regex.IsMatch(v, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

            AssertValueRule("Email", rule, p => p.Email, ValidProfile(), true);

            UserProfile bad = ValidProfile(); bad.Email = "broken@";
            AssertValueRule("Email", rule, p => p.Email, bad, false);
        }

        [Fact]
        public void Phone_AsOptionalValueRule()
        {
            Expression<Func<string, bool>> rule =
                v => string.IsNullOrEmpty(v) || Regex.IsMatch(v, @"^[+0-9 ]{6,}$");

            AssertValueRule("Phone", rule, p => p.Phone, ValidProfile(), true);

            UserProfile empty = ValidProfile(); empty.Phone = "";
            AssertValueRule("Phone", rule, p => p.Phone, empty, true);

            UserProfile bad = ValidProfile(); bad.Phone = "call-me";
            AssertValueRule("Phone", rule, p => p.Phone, bad, false);
        }

        [Fact]
        public void CardNumber_AsValueRule()
        {
            // value rule targeting a folded entity's property
            Expression<Func<string, bool>> rule = v => Functions.IsCreditCardNumberCorrect(v);

            AssertValueRule("Card.Number", rule, p => p.Card.Number, ValidProfile(), true);

            UserProfile bad = ValidProfile(); bad.Card.Number = "4444333322221112";
            AssertValueRule("Card.Number", rule, p => p.Card.Number, bad, false);
        }
    }
}
