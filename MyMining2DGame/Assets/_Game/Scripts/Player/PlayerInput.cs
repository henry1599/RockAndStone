using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DinoMining
{
    public class PlayerInput : MonoBehaviour
    {
        public PlayerInputConfig InputConfig;
        public FrameInput FrameInput { get; private set; }
        private void Update() => FrameInput = Gather();
        private FrameInput Gather() 
        {
            return new FrameInput 
            {
                Interact = Input.GetKey(InputConfig.InteractKey),
                Move = new Vector2(Input.GetAxisRaw(InputConfig.HorizontalInputKey), Input.GetAxisRaw(InputConfig.VerticalInputKey)),
                Mine = Input.GetMouseButton(InputConfig.MineInputKey),
                Shoot = Input.GetMouseButton(InputConfig.ShootInputKey),
                Sprint = Input.GetKey(InputConfig.SprintKey)
            };
        }
    }
    public struct FrameInput
    {
        public Vector2 Move;
        public bool Sprint;
        public bool Interact;
        public bool Mine;
        public bool Shoot;
    }
}
