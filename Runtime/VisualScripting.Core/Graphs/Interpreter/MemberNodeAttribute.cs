using System;

namespace Unity.VisualScripting.Interpreter
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class MemberNodeAttribute : Attribute
    {
        public Type ModelType { get; }
        public string Member { get; }

        public MemberNodeAttribute(Type modelType, string member)
        {
            ModelType = modelType;
            Member = member;
        }

        public static string ToString(Type modelType, Member member)
        {
            return $"[{nameof(MemberNodeAttribute)}(typeof({modelType}), \"{member.ToUniqueString()}\")]";
        }
    }
}
