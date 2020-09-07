using Base.Component;
using Unity.Animation;
using Unity.DataFlowGraph;

namespace Test.Component
{
    public struct PerformanceDataComponent : IDataComponent
    {
        public NodeHandle<DeltaTimeNode> DeltaTimeNode;
        public NodeHandle<MixerNode>     MixerNode;
        public NodeHandle<NMixerNode>    NMixerNode;
        public NodeHandle<ComponentNode> EntityNode;
    }
}