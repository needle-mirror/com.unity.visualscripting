using System;

namespace Unity.VisualScripting.Interpreter
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class ConstantNodeAttribute : Attribute
    {
        public Type Type { get; }

        public ConstantNodeAttribute(Type type)
        {
            Type = type;
        }

        public static string ToString(Type type)
        {
            return $"[{nameof(ConstantNodeAttribute)}(typeof({type}))]";
        }
    }
}
