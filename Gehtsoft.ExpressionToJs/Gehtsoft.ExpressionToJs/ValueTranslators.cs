using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Gehtsoft.ExpressionToJs
{
    // Constant and member-access translation. Like method translators, these are keyed by value
    // type / member (type + name): the built-ins are disjoint, so at most one matches any node and
    // their order is irrelevant. The one deliberate exception is the parameter-access member
    // fallback, which is a terminal catch-all (a typed member such as DateTime.Year intentionally
    // wins over it). Two translators matching the same node at the same tier is a caller logic error.

    /// <summary>Translates a constant value into a JavaScript literal.</summary>
    public interface IConstantTranslator
    {
        /// <summary>
        /// Implement this to render constants of a type the built-ins don't handle.
        ///
        /// Inspect <paramref name="value"/>, and return [c]false[/c] for values that aren't yours so
        /// another translator (or the built-ins) gets a turn.
        /// </summary>
        /// <param name="value">The constant to render; test its type to decide whether to claim it.</param>
        /// <param name="context">Available for escaping strings and reading the date mode while building the literal.</param>
        /// <param name="js">Receives the JavaScript literal when you return [c]true[/c]; otherwise leave it null.</param>
        /// <returns>[c]true[/c] if this translator produced a literal; [c]false[/c] to defer.</returns>
        bool TryTranslate(object value, IExpressionEmitContext context, out string js);
    }

    /// <summary>Additive registry for custom constant types (Guid, enums, value structs, ...).</summary>
    public interface IJsConstantRegistry
    {
        /// <summary>
        /// Register a fully custom constant translator.
        ///
        /// Use this overload when rendering depends on more than the value's static type (e.g.
        /// emitting different literals for different values); for the common "this type always
        /// renders this way" case prefer <see cref="MapConstant{T}"/>.
        /// </summary>
        /// <param name="translator">The translator to consult before the built-ins.</param>
        /// <returns>This registry, for fluent chaining.</returns>
        IJsConstantRegistry AddTranslator(IConstantTranslator translator);

        /// <summary>
        /// Register a constant type by giving the function that renders it.
        ///
        /// The concise way to add a type the built-ins don't cover (a <see cref="Guid"/>, a domain
        /// value), or to override how one is emitted (e.g. an enum by name instead of by number).
        /// </summary>
        /// <typeparam name="T">The constant type this mapping applies to (and its subtypes).</typeparam>
        /// <param name="emit">Produces the JavaScript literal for a value of <typeparamref name="T"/>; it owns any required escaping.</param>
        /// <returns>This registry, for fluent chaining.</returns>
        IJsConstantRegistry MapConstant<T>(Func<T, string> emit);
    }

    /// <summary>Translates a member/property access into JavaScript.</summary>
    public interface IMemberTranslator
    {
        /// <summary>
        /// Implement this to emit a property/field read the built-ins don't cover.
        ///
        /// Check the member's declaring type and name, and return [c]false[/c] for reads that aren't
        /// yours.
        /// </summary>
        /// <param name="member">The member access to inspect; its [c]Member[/c] and [c]Expression[/c] tell you what is being read and from what.</param>
        /// <param name="context">Use it to emit the target expression ([c]member.Expression[/c]) and read the date mode.</param>
        /// <param name="js">Receives the emitted JavaScript when you return [c]true[/c]; otherwise leave it null.</param>
        /// <returns>[c]true[/c] if this translator handled the access; [c]false[/c] to defer.</returns>
        bool TryTranslate(MemberExpression member, IExpressionEmitContext context, out string js);
    }

    /// <summary>Additive registry for custom member/property access.</summary>
    public interface IJsMemberRegistry
    {
        /// <summary>
        /// Register a fully custom member translator.
        ///
        /// Use this overload when the emitted form needs logic a fixed template can't express;
        /// otherwise prefer <see cref="MapMember"/>.
        /// </summary>
        /// <param name="translator">The translator to consult before the built-ins.</param>
        /// <returns>This registry, for fluent chaining.</returns>
        IJsMemberRegistry AddTranslator(IMemberTranslator translator);

        /// <summary>
        /// Map a property (by declaring type + name) to a JavaScript template.
        ///
        /// The concise way to teach the compiler a property read it doesn't already know.
        /// </summary>
        /// <param name="declaringType">The type that declares the property to match.</param>
        /// <param name="name">The property name to match, exactly.</param>
        /// <param name="template">The JavaScript template; [c]$obj[/c] is replaced by the emitted target the property is read from.</param>
        /// <returns>This registry, for fluent chaining.</returns>
        IJsMemberRegistry MapMember(Type declaringType, string name, string template);
    }

    /// <summary>
    /// Registry for binding a parameter type to a host-side lookup instead of emitting it verbatim.
    ///
    /// By default a lambda parameter emits as itself - [c]m => m.Age[/c] becomes [c]m.Age[/c] - which
    /// assumes the client already has an object shaped like the parameter. For form validation you
    /// usually want a model parameter to resolve against the page instead, through a host-provided
    /// [c]reference('path')[/c] hook. Register the model type here (typically once, for the whole
    /// application) and every rule over that type emits against the form, with no subclassing.
    ///
    /// Register before reading <see cref="ExpressionCompiler.JavaScriptExpression"/>.
    /// </summary>
    public interface IJsParameterRegistry
    {
        /// <summary>
        /// Bind the matching rule parameters to the built-in [c]reference()[/c] convention.
        ///
        /// The bare parameter emits as [c]reference()[/c] and a member chain rooted in it emits as
        /// [c]reference('Path.To.Field')[/c] (array indexing becomes [c]jsv_index(...)[/c]) - the
        /// page supplies a [c]reference(path)[/c] function that returns the field's current value.
        /// This is the common case; use <see cref="Map"/> for any other shape.
        ///
        /// The match runs on the parameter's type, but usually the exact type does not matter - a
        /// predicate such as [c]t => !t.IsPrimitive &amp;&amp; t != typeof(string)[/c] (anything that
        /// looks like a model) is typical. Only the rule's own (top-level) parameters are matched;
        /// parameters introduced by a nested LINQ lambda always keep their own name.
        /// </summary>
        /// <param name="matches">Tells whether a rule parameter of the given type should bind to a reference.</param>
        /// <returns>This registry, for fluent chaining.</returns>
        IJsParameterRegistry MapReference(Func<Type, bool> matches);

        /// <summary>
        /// Bind the matching rule parameters with custom rendering.
        ///
        /// Use this when the host hook is not the default [c]reference()[/c] shape - for example a
        /// validated value bound to an ambient [c]value[/c], or a differently named lookup. As with
        /// <see cref="MapReference"/>, only the rule's own parameters are matched.
        /// </summary>
        /// <param name="matches">Tells whether a rule parameter of the given type should bind.</param>
        /// <param name="parameter">
        /// Renders the whole model - the bare parameter. The <see cref="ParameterExpression"/> gives
        /// its type (and name) - for example [c]p => "value"[/c], or
        /// [c]p => "reference('" + p.Type.Name + "')"[/c].
        /// </param>
        /// <param name="parameterAccess">
        /// Renders a field access, given the member/index chain and the root parameter it is rooted
        /// in. Use <see cref="ExpressionCompiler.ParameterAccessPath"/> to turn the chain into a
        /// dotted path - for example
        /// [c](e, p) => "value." + ExpressionCompiler.ParameterAccessPath(e)[/c] turns
        /// [c]m.Address.PostalCode[/c] into [c]value.Address.PostalCode[/c].
        /// </param>
        /// <returns>This registry, for fluent chaining.</returns>
        IJsParameterRegistry Map(Func<Type, bool> matches, Func<ParameterExpression, string> parameter, Func<Expression, ParameterExpression, string> parameterAccess);
    }

    /// <summary>Emits DateTime constants in the configured frame (Local vs UTC).</summary>
    internal sealed class DateTimeConstantTranslator : IConstantTranslator
    {
        public bool TryTranslate(object value, IExpressionEmitContext context, out string js)
        {
            if (value is DateTime dt)
            {
                js = ExpressionCompiler.FormatDateLiteral(dt, context.DateMode);
                return true;
            }
            js = null;
            return false;
        }
    }

    /// <summary>Emits DateTime member reads in the configured frame (getX vs getUTCX).</summary>
    internal sealed class DateTimeMemberTranslator : IMemberTranslator
    {
        public bool TryTranslate(MemberExpression member, IExpressionEmitContext context, out string js)
        {
            js = null;
            if (member.Member.DeclaringType != typeof(DateTime))
                return false;

            bool utc = context.DateMode == DateTimeMode.Utc;

            // Ambient-now accessors stay dynamic (evaluated client-side at validation time).
            if (member.Member.Name == nameof(DateTime.Now) || member.Member.Name == nameof(DateTime.UtcNow))
            {
                js = "new Date()"; // the current instant; component reads on top follow DateMode
                return true;
            }
            if (member.Member.Name == nameof(DateTime.Today))
            {
                js = utc ? "jsv_today(true)" : "jsv_today(false)"; // midnight today in the chosen frame
                return true;
            }

            string obj = context.Emit(member.Expression);

            switch (member.Member.Name)
            {
                case nameof(DateTime.Year): js = utc ? $"{obj}.getUTCFullYear()" : $"{obj}.getFullYear()"; return true;
                case nameof(DateTime.Month): js = utc ? $"({obj}.getUTCMonth() + 1)" : $"({obj}.getMonth() + 1)"; return true;
                case nameof(DateTime.Day): js = utc ? $"{obj}.getUTCDate()" : $"{obj}.getDate()"; return true;
                case nameof(DateTime.Hour): js = utc ? $"{obj}.getUTCHours()" : $"{obj}.getHours()"; return true;
                case nameof(DateTime.Minute): js = utc ? $"{obj}.getUTCMinutes()" : $"{obj}.getMinutes()"; return true;
                case nameof(DateTime.Second): js = utc ? $"{obj}.getUTCSeconds()" : $"{obj}.getSeconds()"; return true;
                case nameof(DateTime.DayOfWeek): js = utc ? $"{obj}.getUTCDay()" : $"{obj}.getDay()"; return true;
            }
            return false;
        }
    }

    /// <summary>A constant translator that emits values of a specific type via a delegate.</summary>
    internal sealed class DelegateConstantTranslator : IConstantTranslator
    {
        private readonly Type mType;
        private readonly Func<object, string> mEmit;

        /// <summary>
        /// Creates a constant translator bound to one type. This is what
        /// <see cref="IJsConstantRegistry.MapConstant{T}"/> builds for you; construct it directly
        /// only when you are assembling translators outside the registry.
        /// </summary>
        /// <param name="type">The type (and its subtypes) whose constant values this translator claims; required.</param>
        /// <param name="emit">Renders a matching value as a JavaScript literal; required, and owns any escaping.</param>
        public DelegateConstantTranslator(Type type, Func<object, string> emit)
        {
            mType = type ?? throw new ArgumentNullException(nameof(type));
            mEmit = emit ?? throw new ArgumentNullException(nameof(emit));
        }

        public bool TryTranslate(object value, IExpressionEmitContext context, out string js)
        {
            if (value != null && mType.IsInstanceOfType(value))
            {
                js = mEmit(value);
                return true;
            }
            js = null;
            return false;
        }
    }

    /// <summary>Data-driven member translator. Template token [c]$obj[/c] = the emitted target.</summary>
    internal sealed class TableMemberTranslator : IMemberTranslator
    {
        private sealed class Entry
        {
            public Type DeclaringType;   // null = match any declaring type (by name only)
            public string Name;
            public string Template;
        }

        private readonly List<Entry> mEntries = new List<Entry>();

        /// <summary>
        /// Adds one property-to-template mapping and returns the same translator so calls chain.
        /// Use it for properties that map straight onto a JavaScript expression of their target.
        /// </summary>
        /// <param name="declaringType">The declaring type to match, or [c]null[/c] to match by name regardless of type (widens the match - use sparingly).</param>
        /// <param name="name">The property name to match, exactly.</param>
        /// <param name="template">The JavaScript template; [c]$obj[/c] is replaced by the emitted target. A template without [c]$obj[/c] emits as-is (for a property whose value doesn't depend on the instance).</param>
        /// <returns>This translator, for fluent chaining of further <see cref="Map"/> calls.</returns>
        public TableMemberTranslator Map(Type declaringType, string name, string template)
        {
            mEntries.Add(new Entry { DeclaringType = declaringType, Name = name, Template = template });
            return this;
        }

        public bool TryTranslate(MemberExpression member, IExpressionEmitContext context, out string js)
        {
            foreach (Entry e in mEntries)
            {
                if (e.DeclaringType != null && member.Member.DeclaringType != e.DeclaringType)
                    continue;
                if (e.Name != member.Member.Name)
                    continue;

                js = member.Expression != null && e.Template.Contains("$obj")
                    ? e.Template.Replace("$obj", context.Emit(member.Expression))
                    : e.Template;
                return true;
            }
            js = null;
            return false;
        }
    }

    /// <summary>Nullable&lt;T&gt;.HasValue / .Value (the declaring type varies per T).</summary>
    internal sealed class NullableMemberTranslator : IMemberTranslator
    {
        public bool TryTranslate(MemberExpression member, IExpressionEmitContext context, out string js)
        {
            js = null;
            Type declaring = member.Member.DeclaringType;
            if (declaring == null || Nullable.GetUnderlyingType(declaring) == null)
                return false;

            if (member.Member.Name == "HasValue")
            {
                js = $"jsv_notequal({context.Emit(member.Expression)}, null)";
                return true;
            }
            if (member.Member.Name == "Value")
            {
                js = $"({context.Emit(member.Expression)})";
                return true;
            }
            return false;
        }
    }

    /// <summary>Terminal fallback: any member chain rooted in a lambda parameter.</summary>
    internal sealed class ParameterAccessMemberTranslator : IMemberTranslator
    {
        public bool TryTranslate(MemberExpression member, IExpressionEmitContext context, out string js)
        {
            if (context.RootsInParameter(member))
            {
                js = context.EmitParameterAccess(member);
                return true;
            }
            js = null;
            return false;
        }
    }
}
