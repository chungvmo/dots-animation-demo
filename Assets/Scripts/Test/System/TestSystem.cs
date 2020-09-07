using System;
using Test.Component;
using Unity.Animation;
using Unity.Collections;
using Unity.DataFlowGraph;
using Unity.Entities;
using UnityEngine.Assertions;
using UnityTemplateProjects.Base.System;
using Random = Unity.Mathematics.Random;

namespace UnityTemplateProjects.Test.System
{
    public class TestSystem : BaseSystem<PerformanceSetupComponent, PerformanceDataComponent,
        PreAnimationGraphSystem.Tag, PreAnimationGraphSystem>
    {
        static readonly Unity.Mathematics.Random _Random = new Random(0x12345678);

        protected override PerformanceDataComponent CreateGraph(Entity e, ref Rig rig,
            PreAnimationGraphSystem animationGraph,
            ref PerformanceSetupComponent setupComponent)
        {
            if (!EntityManager.HasComponent<PerformanceSetupAsset>(e))
                throw new InvalidOperationException(
                    "Entity is missing a PerformanceSetupAsset IBufferElementData");

            var set = animationGraph.Set;
            var data = new PerformanceDataComponent();
            data.DeltaTimeNode = set.Create<DeltaTimeNode>();
            data.EntityNode = set.CreateComponentNode(e);

            var clipBuffer = EntityManager.GetBuffer<PerformanceSetupAsset>(e);
            Assert.AreNotEqual(clipBuffer.Length, 0);

            var clipPlayerNodes = new NativeArray<NodeHandle<ClipPlayerNode>>(clipBuffer.Length, Allocator.Temp);
            if (clipBuffer.Length == 1)
            {
                // Clip to output (no mixers)
                clipPlayerNodes[0] = set.Create<ClipPlayerNode>();
                set.SetData(clipPlayerNodes[0], ClipPlayerNode.KernelPorts.Speed, _Random.NextFloat(0.1f, 1f));

                set.Connect(data.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, clipPlayerNodes[0],
                    ClipPlayerNode.KernelPorts.DeltaTime);
                set.Connect(clipPlayerNodes[0], ClipPlayerNode.KernelPorts.Output, data.EntityNode);

                set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Configuration,
                    new ClipConfiguration {Mask = ClipConfigurationMask.LoopTime});
                set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Rig, rig);
                set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Clip, clipBuffer[0].Clip);
            }
            else if (clipBuffer.Length == 2)
            {
                // Clips to binary mixer
                data.MixerNode = set.Create<MixerNode>();
                clipPlayerNodes[0] = set.Create<ClipPlayerNode>();
                clipPlayerNodes[1] = set.Create<ClipPlayerNode>();

                set.SetData(clipPlayerNodes[0], ClipPlayerNode.KernelPorts.Speed, _Random.NextFloat(0.1f, 1f));
                set.SetData(clipPlayerNodes[1], ClipPlayerNode.KernelPorts.Speed, _Random.NextFloat(0.1f, 1f));

                set.Connect(data.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, clipPlayerNodes[0],
                    ClipPlayerNode.KernelPorts.DeltaTime);
                set.Connect(data.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, clipPlayerNodes[1],
                    ClipPlayerNode.KernelPorts.DeltaTime);
                set.Connect(clipPlayerNodes[0], ClipPlayerNode.KernelPorts.Output, data.MixerNode,
                    MixerNode.KernelPorts.Input0);
                set.Connect(clipPlayerNodes[1], ClipPlayerNode.KernelPorts.Output, data.MixerNode,
                    MixerNode.KernelPorts.Input1);
                set.Connect(data.MixerNode, MixerNode.KernelPorts.Output, data.EntityNode);

                set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Configuration,
                    new ClipConfiguration {Mask = ClipConfigurationMask.LoopTime});
                set.SendMessage(clipPlayerNodes[1], ClipPlayerNode.SimulationPorts.Configuration,
                    new ClipConfiguration {Mask = ClipConfigurationMask.LoopTime});
                set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Rig, rig);
                set.SendMessage(clipPlayerNodes[1], ClipPlayerNode.SimulationPorts.Rig, rig);
                set.SendMessage(clipPlayerNodes[0], ClipPlayerNode.SimulationPorts.Clip, clipBuffer[0].Clip);
                set.SendMessage(clipPlayerNodes[1], ClipPlayerNode.SimulationPorts.Clip, clipBuffer[1].Clip);
                set.SendMessage(data.MixerNode, MixerNode.SimulationPorts.Rig, rig);
                set.SetData(data.MixerNode, MixerNode.KernelPorts.Weight, _Random.NextFloat(0f, 1f));
            }
            else
            {
                // Clips to n-mixer
                data.NMixerNode = set.Create<NMixerNode>();

                set.SendMessage(data.NMixerNode, NMixerNode.SimulationPorts.Rig, rig);
                set.SetPortArraySize(data.NMixerNode, NMixerNode.KernelPorts.Inputs, clipBuffer.Length);
                set.SetPortArraySize(data.NMixerNode, NMixerNode.KernelPorts.Weights, clipBuffer.Length);

                var clipWeights = new NativeArray<float>(clipBuffer.Length, Allocator.Temp);
                var wSum = 0f;
                for (int i = 0; i < clipBuffer.Length; ++i)
                {
                    clipPlayerNodes[i] = set.Create<ClipPlayerNode>();

                    set.SendMessage(clipPlayerNodes[i], ClipPlayerNode.SimulationPorts.Configuration,
                        new ClipConfiguration {Mask = ClipConfigurationMask.LoopTime});
                    set.SendMessage(clipPlayerNodes[i], ClipPlayerNode.SimulationPorts.Rig, rig);
                    set.SendMessage(clipPlayerNodes[i], ClipPlayerNode.SimulationPorts.Clip, clipBuffer[i].Clip);
                    set.SetData(clipPlayerNodes[i], ClipPlayerNode.KernelPorts.Speed, _Random.NextFloat(0.1f, 1f));

                    float w = _Random.NextFloat(0.1f, 1f);
                    wSum += w;
                    clipWeights[i] = w;

                    set.Connect(data.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, clipPlayerNodes[i],
                        ClipPlayerNode.KernelPorts.DeltaTime);
                    set.Connect(clipPlayerNodes[i], ClipPlayerNode.KernelPorts.Output, data.NMixerNode,
                        NMixerNode.KernelPorts.Inputs, i);
                }

                // Set normalized clip weights on NMixer
                float wFactor = 1f / wSum;
                for (int i = 0; i < clipBuffer.Length; ++i)
                    set.SetData(data.NMixerNode, NMixerNode.KernelPorts.Weights, i, clipWeights[i] * wFactor);

                set.Connect(data.NMixerNode, NMixerNode.KernelPorts.Output, data.EntityNode);
            }

            PostUpdateCommands.AddComponent(e, animationGraph.TagComponent);
            var clipNodeBuffer = PostUpdateCommands.AddBuffer<PerformanceDataAsset>(e);
            for (int i = 0; i < clipPlayerNodes.Length; ++i)
                clipNodeBuffer.Add(new PerformanceDataAsset {ClipNode = clipPlayerNodes[i]});

            return data;
        }

        protected override void DestroyGraph(Entity e, PreAnimationGraphSystem animationGraph,
            ref PerformanceDataComponent dataComponent)
        {
            if (!EntityManager.HasComponent<PerformanceDataAsset>(e))
                throw new InvalidOperationException("Entity is missing a PerformanceDataAsset ISystemStateBufferElementData");

            var set = animationGraph.Set;
            var clipNodeBuffer = EntityManager.GetBuffer<PerformanceDataAsset>(e);
            for (int i = 0; i < clipNodeBuffer.Length; ++i)
                set.Destroy(clipNodeBuffer[i].ClipNode);

            EntityManager.RemoveComponent<PerformanceDataAsset>(e);

            set.Destroy(dataComponent.DeltaTimeNode);
            set.Destroy(dataComponent.EntityNode);

            if (dataComponent.MixerNode != default)
                set.Destroy(dataComponent.MixerNode);

            if (dataComponent.NMixerNode != default)
                set.Destroy(dataComponent.NMixerNode);
        }
    }
    public struct PerformanceSetupAsset : IBufferElementData
    {
        public BlobAssetReference<Clip> Clip;
    }
    public struct PerformanceDataAsset : ISystemStateBufferElementData
    {
        public NodeHandle<ClipPlayerNode> ClipNode;
    }
}