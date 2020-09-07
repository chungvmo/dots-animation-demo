using System;
using System.Collections.Generic;
using Base.Component;
using Unity.Animation.Hybrid;
using Unity.Entities;
using UnityEngine;

namespace Base.MonoBehaviour
{
    [RequiresEntityConversion]
    public class Spawner : UnityEngine.MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        public GameObject         RigPrefab;
        public AnimationGraphBase GraphPrefab;

        public int CountX = 100;
        public int CountY = 100;

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(RigPrefab);

            if (GraphPrefab != null)
            {
                GraphPrefab.DeclareReferencedPrefabs(referencedPrefabs);
            }
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var rigPrefab = conversionSystem.TryGetPrimaryEntity(RigPrefab);

            if (rigPrefab == Entity.Null)
                throw new Exception($"Something went wrong while creating an Entity for the rig prefab: {RigPrefab.name}");

            if (GraphPrefab != null)
            {
                var rigComponent = RigPrefab.GetComponent<RigComponent>();
                GraphPrefab.PreProcessData(rigComponent);
                GraphPrefab.AddGraphSetupComponent(rigPrefab, dstManager, conversionSystem);
            }

            dstManager.AddComponentData(entity, new RigSpawner
            {
                RigPrefab = rigPrefab,
                CountX = CountX,
                CountY = CountY,
            });
        }
    }

}