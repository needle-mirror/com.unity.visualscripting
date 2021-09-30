using System;

namespace Unity.VisualScripting
{
    [Flags]
    public enum TranslationOptions
    {
        None = 0,
        TranslateUnusedNodes = 1,
        CodegenApiNodes = 2,
        ForceApiReflectionNodes = 4,
        Default = CodegenApiNodes,
    }
}
