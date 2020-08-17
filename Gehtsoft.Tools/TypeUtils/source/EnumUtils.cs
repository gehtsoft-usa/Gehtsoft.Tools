using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gehtsoft.Tools.TypeUtils
{
    public class EnumValue<TE, TV> 
    {
        public TV Value { get; internal set; }
        public TE EnumerationValue { get; internal set; }
        internal Enum RawEnumerationValue { get; set; }
        public FieldInfo FieldInfo { get; set; }
        public TA GetCustomAttribute<TA>() where TA : System.Attribute => FieldInfo.GetCustomAttribute<TA>();
        public string Description => RawEnumerationValue.Description();
    }

    public class EnumDescriptionAttribute : System.Attribute
    {
        public string Description { get; set; }

        public EnumDescriptionAttribute()
        {

        }

        public EnumDescriptionAttribute(string description)
        {
            Description = description;
        }
    }

    public static class EnumUtils
    {
        public static T GetCustomAttribute<T>(this Enum enumVal) where T : System.Attribute => (T) GetCustomAttribute(enumVal, typeof(T));

        public static string Description(this Enum enumVal) => enumVal.GetCustomAttribute<EnumDescriptionAttribute>()?.Description;

        public static System.Attribute GetCustomAttribute(this Enum enumVal, Type attributeType) 
        {
            Type type = enumVal.GetType();
            MemberInfo[] memInfo = type.GetMember(enumVal.ToString());
            object[] attributes = memInfo[0].GetCustomAttributes(attributeType, false);
            if (attributes == null || attributes.Length < 1)
                return null;
            return (System.Attribute)attributes[0];
        }

        public static EnumValue<TE, TV>[] GetEnumValues<TE, TV>()
        {
            Type typeEnum = typeof(TE);
            EnumValue<TE, TV>[] values;
            TypeInfo typeInfo = typeEnum.GetTypeInfo();

            if (!typeInfo.IsEnum)
                throw new ArgumentException(nameof(typeEnum), "Type is not an enumeration");


            FieldInfo[] fields = typeInfo.GetFields(BindingFlags.Static | BindingFlags.Public);
            values = new EnumValue<TE, TV>[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                TE rawValue = (TE)fields[i].GetRawConstantValue();
                TV value;
                value = (TV)Convert.ChangeType(rawValue, typeof(TV));
                values[i] = new EnumValue<TE, TV>() {Value = value, EnumerationValue = rawValue, RawEnumerationValue = (Enum) (object)rawValue, FieldInfo = fields[i]};
            }
            return values;
        }
    }
}
