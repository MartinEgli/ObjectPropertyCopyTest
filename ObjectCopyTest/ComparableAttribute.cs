using System;

namespace ObjectCopyTest
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ComparableAttribute : Attribute
    {
    }
}