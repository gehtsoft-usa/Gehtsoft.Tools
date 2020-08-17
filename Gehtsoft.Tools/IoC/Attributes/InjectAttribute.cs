using System;

namespace Gehtsoft.Tools.IoC.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class InjectAttribute : Attribute
    {
        public bool Required { get; set; } = true;

        public InjectAttribute()
        {

        }

        public InjectAttribute(bool required)
        {
            Required = required;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class InjectOptionalAttribute : InjectAttribute
    {
        public InjectOptionalAttribute() : base(false)
        {

        }
    }
}