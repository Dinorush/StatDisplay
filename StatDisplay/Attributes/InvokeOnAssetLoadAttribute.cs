using System;

namespace StatDisplay.Attributes
{
    // Shamelessly stolen from Flow because I like this
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class InvokeOnAssetLoadAttribute : Attribute
    {
    }
}
