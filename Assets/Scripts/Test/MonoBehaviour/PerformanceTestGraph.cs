using Base.MonoBehaviour;
using Test.Component;
using Unity.Animation.Hybrid;
using Unity.Entities;
using UnityEngine;
using UnityTemplateProjects.Test.System;

namespace Test.MonoBehaviour
{
    public class PerformanceTestGraph : AnimationGraphBase
    {
        public AnimationClip[] Clips;

        public override void AddGraphSetupComponent(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
            if (Clips == null || Clips.Length == 0)
            {
                UnityEngine.Debug.LogError("No clips specified for performance test!");
                return;
            }

            var clipBuffer = dstManager.AddBuffer<PerformanceSetupAsset>(entity);
            for (int i = 0; i < Clips.Length; ++i)
                clipBuffer.Add(new PerformanceSetupAsset {Clip = Clips[i].ToDenseClip()});

            dstManager.AddComponent<PerformanceSetupComponent>(entity);
        }
    }
}