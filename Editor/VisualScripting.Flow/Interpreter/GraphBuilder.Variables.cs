using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.VisualScripting.Interpreter
{
    public partial class GraphBuilder
    {

        public VariableHandle GetVariableDataIndex(UnifiedVariableUnit variableModelDeclarationModel)
        {
            var bindingId = m_VariableUnitsToBindingIds[variableModelDeclarationModel];
            if (!m_VariableToDataIndex.TryGetValue(bindingId, out var handle) && variableModelDeclarationModel.kind == VariableKind.Flow)
            {
                handle = BindVariableToDataIndex(bindingId);
            }

            return handle;
        }

        public VariableHandle DeclareVariable(VariableType type, ValueType valueType, BindingId bindingId,
            string name = null, Value? initValue = null)
        {
            var variableHandle = BindVariableToDataIndex(bindingId);
            var dataIndex = variableHandle.DataIndex;
            if (type == VariableType.ObjectReference || type == VariableType.SmartObject)
                Assert.IsFalse(initValue.HasValue);
            else if (initValue.HasValue) AddVariableInitValue(dataIndex, initValue.Value);

            var bindingIdName = new GraphDefinition.VariableDescription(bindingId, name ?? bindingId.ToString(),
                dataIndex, valueType, type);

            int insertIndex;
            var variables = m_Result.GraphDefinition.Variables;
            if (type == VariableType.Input) // inputs come first
            {
                insertIndex =
                    variables.FindLastIndex(x =>
                        x.VariableType == VariableType.Input);
                if (insertIndex == -1)
                    insertIndex = 0;
                variables.Insert(insertIndex, bindingIdName);
            }
            else if (type == VariableType.Output) // then output
            {
                insertIndex =
                    variables.FindLastIndex(x => x.VariableType == VariableType.Output);
                if (insertIndex == -1) // if no existing output, insert after input
                    insertIndex = variables.FindLastIndex(x => x.VariableType == VariableType.Input);
                if (insertIndex == -1)
                    insertIndex = 0;
                variables.Insert(insertIndex, bindingIdName);
            }
            else
                variables.Add(bindingIdName);

            return variableHandle;
        }

        private void AddVariableInitValue(uint dataIndex, Value initValue)
        {
            m_Result.GraphDefinition.VariableInitValues.Add(new GraphDefinition.VariableInitValue
            { DataIndex = dataIndex, Value = initValue });
        }

        public VariableHandle BindVariableToDataIndex(BindingId variableId, uint? dataIndex = null)
        {
            Assert.AreNotEqual(default, variableId);
            if (!dataIndex.HasValue)
                dataIndex = AllocateDataIndex();
            m_VariableToDataIndex.Add(variableId, new VariableHandle(dataIndex.Value));
            return new VariableHandle { DataIndex = dataIndex.Value };
        }

        public void AddVariableUnit(UnifiedVariableUnit unifiedVariableUnit, BindingId bindingId)
        {
            m_VariableUnitsToBindingIds.Add(unifiedVariableUnit, bindingId);
        }
    }
}
