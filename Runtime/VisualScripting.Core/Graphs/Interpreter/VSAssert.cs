using System.Diagnostics;
using UnityEngine.Assertions;

namespace Unity.VisualScripting.Interpreter
{
    static class VSAssert
    {
        [Conditional("VS_ASSERT")]
        public static void AreEqual<T>(T expected, T actual, string message = null)
        {
            Assert.AreEqual(expected, actual, message);
        }
    }
}
