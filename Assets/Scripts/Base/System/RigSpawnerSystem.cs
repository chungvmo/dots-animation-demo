using Base.Component;
using Base.MonoBehaviour;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using NotImplementedException = System.NotImplementedException;

namespace UnityTemplateProjects.Base.System
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class RigSpawnerSystem : ComponentSystem
    {
        AnimationInputBase m_Input;

        public void RegisterInput(AnimationInputBase input)
        {
            m_Input = input;
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((Entity e, ref RigSpawner spawner) =>
            {
                for (var x = 0; x < spawner.CountX; x++)
                {
                    for (var y = 0; y < spawner.CountY; ++y)
                    {
                        var rigInstance = EntityManager.Instantiate(spawner.RigPrefab);
                        var position = new float3(x * 1.3F, 0, y * 1.3F);
                        EntityManager.SetComponentData(rigInstance, new Translation { Value = position });

                        if (m_Input != null)
                            m_Input.RegisterEntity(rigInstance);
                    }
                }

                EntityManager.DestroyEntity(e);
            });
        }
    }
}