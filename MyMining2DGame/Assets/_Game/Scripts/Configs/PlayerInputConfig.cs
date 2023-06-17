using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace DinoMining
{
    [CreateAssetMenu(menuName = "Scriptable Objects/PlayerInputConfig", fileName = "Player Input Config")]
    public class PlayerInputConfig : ScriptableObject
    {
        [Foldout("String input")] public string HorizontalInputKey;
        [Foldout("String input")] public string VerticalInputKey;
        
        [Foldout("Integer input")] public int ShootInputKey;
        [Foldout("Integer input")] public int MineInputKey;
        
        [Foldout("KeyCode input")] public KeyCode InteractKey;
        [Foldout("KeyCode input")] public KeyCode SprintKey;
    }
}
