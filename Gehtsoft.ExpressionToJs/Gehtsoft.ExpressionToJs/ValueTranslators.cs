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
        bool TryTranslate(object value, IExpressionEmitContext context, out string js);
    }

    /// <summary>Additive registry for custom constant types (Guid, enums, value structs, ...).</summary>
    public interface IJsConstantRegistry
    {
        IJsConstantRegistry AddTranslator(IConstantTranslator translator);

        /// <summary>Emit values of type <typeparamref name="T"/> with the given function.</summary>
        IJsConstantRegistry MapConstant<T>(Func<T, string> emit);
    }

    /// <summary>Translates a member/property access into JavaScript.</summary>
    public interface IMemberTranslator
    {
        bool TryTranslate(MemberExpression member, IExpressionEmitContext context, out string js);
    }

    /// <summary>Additive registry for custom member/property access.</summary>
    public interface IJsMemberRegistry
    {
        IJsMemberRegistry AddTranslator(IMemberTranslator translator);

        /// <summary>Map a property (type + name) to a JS template ($obj = the emitted target).</summary>
        IJsMemberRegistry MapMember(Type declaringType, string name, string template);
    }

    /// <summary>Emits DateTime constants in the configured frame (Local vs UTC).</summary>
    public sealed class DateTimeConstantTranslator : IConstantTranslator
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
    public sealed class DateTimeMemberTranslator : IMemberTranslator
    {
        public bool TryTranslate(MemberExpression member, IExpressionEmitContext context, out string js)
        {
            js = null;
            if (member.Member.DeclaringType != typeof(DateTime))
                return false;

            if (member.Member.Name == nameof(DateTime.Now))
            {
                js = "new Date()"; // an instant; identical in both modes
                return true;
            }

            bool utc = context.DateMode == DateTimeMode.Utc;
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
    public sealed class DelegateConstantTranslator : IConstantTranslator
    {
        private readonly Type mType;
        private readonly Func<object, string> mEmit;

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

    /// <summary>Data-driven member translator. Template token <c>$obj</c> = the emitted target.</summary>
    public sealed class TableMemberTranslator : IMemberTranslator
    {
        private sealed class Entry
        {
            public Type DeclaringType;   // null = match any declaring type (by name only)
            public string Name;
            public string Template;
        }

        private readonly List<Entry> mEntries = new List<Entry>();

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
    public sealed class NullableMemberTranslator : IMemberTranslator
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
    public sealed class ParameterAccessMemberTranslator : IMemberTranslator
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
