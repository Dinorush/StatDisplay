using System;

namespace StatDisplay.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class InvokeOnBuildStartAttribute : Attribute
    {
        public InvokeOnBuildStartAttribute()
        {
        }
    }
}
