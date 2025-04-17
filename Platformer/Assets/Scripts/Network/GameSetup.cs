using System;
using Unity.Entities;

[Serializable]
public struct GameSetup : IComponentData
{
    public Entity CharacterSpawnPointEntity;
    public Entity CharacterPrefab;
    public Entity PlayerPrefab;
    public Entity CameraPrefab;
}