using System;
using UnityEngine;

namespace Control.Tools
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RequiredAttribute : PropertyAttribute
    {
        public RequiredAttribute()
        {
        }

        public RequiredAttribute(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}


