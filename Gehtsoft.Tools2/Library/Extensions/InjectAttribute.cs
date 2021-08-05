using System;

namespace Gehtsoft.Tools2.Extensions
{
    /// <summary>
    /// The attribute to mark up the properties or attributes to be injected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
    }
}
