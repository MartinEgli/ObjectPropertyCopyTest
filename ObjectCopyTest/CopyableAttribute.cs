using System;

namespace ObjectCopyTest
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CopyableAttribute : Attribute
    {
        public string Category { get; set; }
    }
}