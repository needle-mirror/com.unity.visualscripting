using System;

namespace Unity.VisualScripting.Interpreter
{
    interface INodeCodeGenerator
    {
        Type UnitType { get; }

        bool GenerateCode(IUnit unit, out string fileName, out string code);
        bool ShouldGenerateCode(IUnit unit, TranslationOptions options);
    }

    public abstract class NodeCodeGenerator<T> : INodeCodeGenerator where T : IUnit
    {
        public Type UnitType => typeof(T);

        public bool GenerateCode(IUnit unit, out string fileName, out string code)
        {
            return GenerateCode((T)unit, out fileName, out code);
        }

        public bool ShouldGenerateCode(IUnit unit, TranslationOptions options)
        {
            return ShouldGenerateCode((T)unit, options);
        }

        protected abstract bool GenerateCode(T unit, out string fileName, out string code);

        protected abstract bool ShouldGenerateCode(T unit, TranslationOptions options);

        protected static string FormatFileName(string name)
        {
            return name.Filter(true, whitespace: false, symbols: false, punctuation: false);
        }
    }
}
