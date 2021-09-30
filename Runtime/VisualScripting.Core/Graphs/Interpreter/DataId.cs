using System;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    /// <summary>
    /// Represents an index in the graphInstance value array. base 1, so 0 means the data id is invalid or not initialized
    /// </summary>
    [Serializable]
    public struct DataId
    {
        [SerializeField]
        uint m_DataIndex;

        public static DataId Null => default;

        public DataId(uint index)
        {
            m_DataIndex = index + 1;
        }

        public uint GetIndex()
        {
            return m_DataIndex - 1;
        }

        public bool IsValid()
        {
            return m_DataIndex > 0;
        }
    }
}
