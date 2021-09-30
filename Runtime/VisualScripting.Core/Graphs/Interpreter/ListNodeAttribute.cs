using System;

namespace Unity.VisualScripting.Interpreter
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class ListNodeAttribute : Attribute
    {
        public Type ElementType { get; }

        public ListNodeAttribute(Type elementType)
        {
            ElementType = elementType;
        }

        public static string ToString(Type elementType)
        {
            return $"[{nameof(ListNodeAttribute)}(typeof({elementType}))]";
        }
    }
}
