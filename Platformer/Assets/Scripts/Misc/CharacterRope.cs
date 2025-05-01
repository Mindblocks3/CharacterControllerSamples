using System;
using Unity.Entities;

[Serializable]
public struct CharacterRope : IComponentData
{
    public Entity OwningCharacterEntity;
}