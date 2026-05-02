/*
    IDamageDealer.cs
    
    a simple interface that should be implemented by all enemies 
    so that when the player dies, the camera can turn to show
    the enemy
*/
using System.Collections;
using UnityEngine;

public interface IDamageDealer
{
    Transform DamageSourceTransform { get; }
}