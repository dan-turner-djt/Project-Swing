using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerGraphicsController : MonoBehaviour
{
    public Animator animatorController;

    public Vector3 naturalPosition = new Vector3(0, -0.65f, -0.05f);
    public Vector3 idleLowPosition = new Vector3(0, -0.65f, -0.25f);
    public Vector3 runningPosition = new Vector3(0, -0.65f, -0.36f);
    public Vector3 fallingPosition = new Vector3(0, -0.65f, -0.24f);

    Dictionary<AnimationNames, GenericAnimationData> animationData = new Dictionary<AnimationNames, GenericAnimationData>();

    enum AnimationNames
    {
        //these should match the names used in the state machine
        WalkTree, JumpingUp, FallingDown, AirSwinging
    }

    private void Awake()
    {
        if (animatorController == null)
        {
            Debug.Log("No animation controller assigned for player!");
        }
    }

    public void DoStart()
    {
        animatorController.transform.localPosition = naturalPosition;

        //Create animation data for each animation state. The bool and trigger names must exactly match the names used in the state machine

        animationData.Add(AnimationNames.WalkTree, new GenericAnimationData(AnimationNames.WalkTree.ToString(), "useWalkTree", ""));
        animationData.Add(AnimationNames.JumpingUp, new GenericAnimationData(AnimationNames.JumpingUp.ToString(), "startJumpingUp", ""));
        animationData.Add(AnimationNames.FallingDown, new GenericAnimationData(AnimationNames.FallingDown.ToString(), "startFalling", ""));
        animationData.Add(AnimationNames.AirSwinging, new GenericAnimationData(AnimationNames.AirSwinging.ToString(), "airSwinging", ""));

    }

    public void TurnOffAllBools ()
    {
        foreach (KeyValuePair<AnimationNames, GenericAnimationData> animData in animationData)
        {
            animatorController.SetBool(animData.Value.boolName, false);
        }
    }

    public void TurnOffAllBoolsExcept (string except)
    {
        foreach (KeyValuePair<AnimationNames, GenericAnimationData> animData in animationData)
        {
            if (string.Equals (animData.Value.boolName, except))
            {
                continue;
            }

            animatorController.SetBool(animData.Value.boolName, false);
        }
    }

    


    public void DoUpdate (float deltaTime, bool gamePaused, Vector3 velocity, Vector3 gravityDir, Vector3 playerUp, bool grounded, Vector3 playerInput, bool isSwinging, bool isJumping, bool startedJumping)
    {
        Vector3 lateralVelocity = Vector3.ProjectOnPlane(velocity, playerUp);
        Vector3 verticalVelocity = velocity - lateralVelocity;

        float animationSpeedPercent = (lateralVelocity.magnitude > 0) ? 1 : 0;

        if (startedJumping)
        {
            GenericAnimationData anim;
            animationData.TryGetValue(AnimationNames.JumpingUp, out anim);

            TurnOffAllBoolsExcept(anim.boolName);
            animatorController.SetBool(anim.boolName, true);

            animatorController.transform.localPosition = fallingPosition;
            animatorController.speed = 1f;
        }
        else
        {
            if (grounded)
            {
                GenericAnimationData anim;
                animationData.TryGetValue(AnimationNames.WalkTree, out anim);

                if (!animatorController.GetBool(anim.boolName))
                {
                    animatorController.SetBool(anim.boolName, true);
                    TurnOffAllBoolsExcept(anim.boolName);
                }

                

                if (animationSpeedPercent == 0)
                {
                    animatorController.transform.localPosition = idleLowPosition;
                    animatorController.speed = 0.6f;
                }
                else
                {
                    if (lateralVelocity.magnitude < 11)
                    {
                        animationSpeedPercent = 0.4f;
                        animatorController.transform.localPosition = runningPosition;
                        animatorController.speed = Mathf.Clamp(lateralVelocity.magnitude * 0.22f, 1.2f, 3);
                    }
                    else
                    {
                        animationSpeedPercent = 1;
                        animatorController.transform.localPosition = runningPosition;
                        animatorController.speed = 1.9f;
                    }
                }

                animatorController.SetFloat("speedPercent", animationSpeedPercent, 0.03f, deltaTime);
            }
            else
            {
                if (isSwinging)
                {
                    animatorController.transform.localPosition = idleLowPosition;
                    animatorController.speed = 0.6f;

                    GenericAnimationData anim;
                    animationData.TryGetValue(AnimationNames.AirSwinging, out anim);

                    animatorController.SetBool(anim.boolName, true);
                    TurnOffAllBoolsExcept(anim.boolName);
                }
                else if (!isJumping || (isJumping && ExtVector3.MagnitudeInDirection(velocity, -gravityDir) <= 0))
                {
                    if (!animatorController.GetCurrentAnimatorStateInfo(0).IsName(AnimationNames.FallingDown.ToString()))
                    {
                        GenericAnimationData anim;
                        animationData.TryGetValue(AnimationNames.FallingDown, out anim);

                        animatorController.SetBool(anim.boolName, true);
                        TurnOffAllBoolsExcept(anim.boolName);

                        animatorController.transform.localPosition = fallingPosition;
                        animatorController.speed = 0.6f;
                    }
                    
                }
               
            }
        }
        

        
    }

    public void PauseAnimator ()
    {
        animatorController.enabled = false;
    }

    public void UnPauseAnimator()
    {
        animatorController.enabled = true;
    }
}
