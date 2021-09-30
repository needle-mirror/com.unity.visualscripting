using System;

namespace Unity.VisualScripting.Interpreter
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PortDescriptionAttribute : System.Attribute
    {
        public readonly string AuthoringPortName;

        public PortDescriptionAttribute(string authoringPortName)
        {
            AuthoringPortName = authoringPortName;
        }
    }
}
