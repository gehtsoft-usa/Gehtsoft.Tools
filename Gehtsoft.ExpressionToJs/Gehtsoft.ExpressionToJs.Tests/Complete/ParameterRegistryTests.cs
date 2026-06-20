using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Gehtsoft.ExpressionToJs.Tests.Complete
{
    /// <summary>
    /// Tests for the parameter binding extension point (compiler.Parameters): rendering a rule's
    /// model parameter through a host-side reference() lookup without subclassing.
    /// </summary>
    public class ParameterRegistryTests
    {
        public class Address
        {
            public string PostalCode { get; set; }
        }

        public class Account
        {
            public string Password { get; set; }
            public string Confirm { get; set; }
            public Address Address { get; set; }
            public List<string> Roles { get; set; }
            public int[] Items { get; set; }
            public DateTime Created { get; set; }
        }

        public static class AccountRules
        {
            public static bool IsStrong(Account a) => a != null;
        }

        [Fact]
        public void Default_EmitsParameterVerbatim()
        {
            Expression<Func<Account, bool>> rule = m => m.Password == m.Confirm;
            Assert.Equal("jsv_equal(m.Password, m.Confirm)", new ExpressionCompiler(rule).JavaScriptExpression);
        }

        [Fact]
        public void Default_NoRegistration_LeavesNestedAccessVerbatim()
        {
            // the default (nothing registered) path: member access stays a plain dotted name
            Expression<Func<Account, bool>> rule = m => m.Address.PostalCode == "x";
            Assert.Equal("jsv_equal(m.Address.PostalCode, 'x')", new ExpressionCompiler(rule).JavaScriptExpression);
        }

        [Fact]
        public void MapReference_BareParameterRendersReference()
        {
            // the bare model parameter (not a member) uses the default reference() rendering
            Expression<Func<Account, bool>> rule = m => m == null;
            var compiler = new ExpressionCompiler(rule);
            compiler.Parameters.MapReference(t => t == typeof(Account));
            Assert.Equal("jsv_equal(reference(), null)", compiler.JavaScriptExpression);
        }

        [Fact]
        public void Registered_ButPredicateDoesNotMatch_FallsBackToDefault()
        {
            // a binding exists but does not match this parameter type -> verbatim, as if unregistered
            Expression<Func<Account, bool>> rule = m => m.Password == m.Confirm;
            var compiler = new ExpressionCompiler(rule);
            compiler.Parameters.MapReference(t => t == typeof(string));
            Assert.Equal("jsv_equal(m.Password, m.Confirm)", compiler.JavaScriptExpression);
        }

        [Fact]
        public void MapReference_BindsBareParameterAndMembers()
        {
            Expression<Func<Account, bool>> rule = m => m.Password == m.Confirm;
            var compiler = new ExpressionCompiler(rule);
            compiler.Parameters.MapReference(t => t == typeof(Account));
            Assert.Equal("jsv_equal(reference('Password'), reference('Confirm'))", compiler.JavaScriptExpression);
        }

        [Fact]
        public void MapReference_BuildsDottedPathForNestedMembers()
        {
            Expression<Func<Account, bool>> rule = m => m.Address.PostalCode.Length == 5;
            var compiler = new ExpressionCompiler(rule);
            compiler.Parameters.MapReference(t => t == typeof(Account));
            Assert.Equal("jsv_equal(jsv_length(reference('Address.PostalCode')), 5)", compiler.JavaScriptExpression);
        }

        [Fact]
        public void MapReference_HandlesArrayIndex()
        {
            Expression<Func<Account, bool>> rule = m => m.Items[0] == 5;
            var compiler = new ExpressionCompiler(rule);
            compiler.Parameters.MapReference(t => t == typeof(Account));
            Assert.Equal("jsv_equal(jsv_index(reference('Items'), 0), 5)", compiler.JavaScriptExpression);
        }

        [Fact]
        public void MapReference_DoesNotBindNestedLambdaParameters()
        {
            Expression<Func<Account, bool>> rule = m => m.Roles.Any(r => r == "admin");
            var compiler = new ExpressionCompiler(rule);
            compiler.Parameters.MapReference(t => t == typeof(Account));
            string js = compiler.JavaScriptExpression;
            // the collection roots in the model -> reference(); the lambda parameter stays itself
            Assert.Contains("jsv_any(reference('Roles')", js);
            Assert.Contains("function (r)", js);
            Assert.Contains("jsv_equal(r, 'admin')", js);
        }

        [Fact]
        public void Map_RendersValueParameterAndModelTogether()
        {
            Expression<Func<Account, string, bool>> rule = (m, value) => value == m.Password;
            var compiler = new ExpressionCompiler(rule);
            compiler.Parameters
                    .MapReference(t => t == typeof(Account))
                    .Map(t => t == typeof(string), p => "value", (e, p) => "value." + ExpressionCompiler.ParameterAccessPath(e));
            Assert.Equal("jsv_equal(value, reference('Password'))", compiler.JavaScriptExpression);
        }

        [Fact]
        public void Map_RendersAsObjectAccess()
        {
            // m.Password -> value.Password (object access instead of a reference() lookup)
            Expression<Func<Account, bool>> rule = m => m.Password == m.Confirm;
            var compiler = new ExpressionCompiler(rule);
            compiler.Parameters.Map(_ => true, p => "value", (e, p) => "value." + ExpressionCompiler.ParameterAccessPath(e));
            Assert.Equal("jsv_equal(value.Password, value.Confirm)", compiler.JavaScriptExpression);
        }

        [Fact]
        public void Map_CanQualifyReferenceWithType()
        {
            // emit reference('Account', 'Password') using the root parameter's type as a form name
            Expression<Func<Account, bool>> rule = m => m.Password == m.Confirm;
            var compiler = new ExpressionCompiler(rule);
            compiler.Parameters.Map(
                _ => true,
                p => $"reference('{p.Type.Name}')",
                (e, p) => $"reference('{p.Type.Name}', '{ExpressionCompiler.ParameterAccessPath(e)}')");
            Assert.Equal("jsv_equal(reference('Account', 'Password'), reference('Account', 'Confirm'))", compiler.JavaScriptExpression);
        }

        [Fact]
        public void CustomWholeModelMethod_UsesBareReference_FieldsUsePath()
        {
            // a custom whole-entity check takes the model whole -> reference();
            // a field access takes the member path -> reference('Password'). Both accessors at once.
            Expression<Func<Account, bool>> rule = m => AccountRules.IsStrong(m) && m.Password == m.Confirm;
            var compiler = new ExpressionCompiler(rule);
            compiler.Methods.MapMethod(typeof(AccountRules), nameof(AccountRules.IsStrong), "jsv_account_strong($0)");
            compiler.Parameters.MapReference(_ => true);
            Assert.Equal(
                "((jsv_account_strong(reference())) && (jsv_equal(reference('Password'), reference('Confirm'))))",
                compiler.JavaScriptExpression);
        }

        [Fact]
        public void ParameterAccessPath_BuildsDottedPath()
        {
            Expression<Func<Account, string>> nested = m => m.Address.PostalCode;
            // body is the member chain m.Address.PostalCode
            Assert.Equal("Address.PostalCode", ExpressionCompiler.ParameterAccessPath(((Expression<Func<Account, string>>)nested).Body));
        }

        [Fact]
        public void MapReference_WorksWithABroadPredicate()
        {
            // the usual real-world predicate: anything that looks like a model
            Expression<Func<Account, bool>> rule = m => m.Password == m.Confirm;
            var compiler = new ExpressionCompiler(rule);
            compiler.Parameters.MapReference(t => !t.IsPrimitive && t != typeof(string));
            Assert.Equal("jsv_equal(reference('Password'), reference('Confirm'))", compiler.JavaScriptExpression);
        }

        [Fact]
        public void MatchAll_DoesNotBreakBuiltinMemberAccessor()
        {
            // a "match everything" predicate must not shadow built-in member translators such as
            // DateTime.Year: the accessor still emits getFullYear(), applied to the reference path
            Expression<Func<Account, bool>> rule = m => m.Created.Year == 2000;
            var compiler = new ExpressionCompiler(rule);
            compiler.Parameters.MapReference(t => true);
            Assert.Equal("jsv_equal(reference('Created').getFullYear(), 2000)", compiler.JavaScriptExpression);
        }

        [Fact]
        public void MatchAll_ComposesWithRegisteredMemberAccessor()
        {
            // a registered member mapping on a nested value still fires and composes with the
            // reference path: the accessor applies to reference('Address'), not to a raw object
            Expression<Func<Account, bool>> rule = m => m.Address.PostalCode == "x";
            var compiler = new ExpressionCompiler(rule);
            compiler.Parameters.MapReference(t => true);
            compiler.Members.MapMember(typeof(Address), nameof(Address.PostalCode), "$obj.zip");
            Assert.Equal("jsv_equal(reference('Address').zip, 'x')", compiler.JavaScriptExpression);
        }

        [Fact]
        public void MapReference_NullPredicate_Throws()
        {
            var compiler = new ExpressionCompiler((Expression<Func<Account, bool>>)(m => m.Password == m.Confirm));
            Assert.Throws<ArgumentNullException>(() => compiler.Parameters.MapReference(null));
        }
    }
}
