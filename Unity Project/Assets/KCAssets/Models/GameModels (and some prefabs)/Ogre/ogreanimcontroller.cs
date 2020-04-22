using UnityEngine;
using System.Collections;
using System;

public class ogreanimcontroller : MonoBehaviour
{


    public Animator anim;
    

    // Update is called once per frame

    Vector3 syncToLoc;
    Quaternion syncToRot;
    void StartSyncTo(Vector3 syncPos, Quaternion syncRot, float time)
    {
        syncToLoc = syncPos;
        syncToRot = syncRot;
        totalSyncTime = time;
        syncTime = time;
    }

    internal void TransitionToAttacking(Vector3 syncLoc, Quaternion syncRot, float time)
    {
        anim.CrossFade("mutant_swiping", 0.1f);
        anim.applyRootMotion = true;
        StartSyncTo(syncLoc, syncRot, time);
    }

    internal void TransitionToDying()
    {
        anim.CrossFade("mutant_dying", 0.1f);
    }

    internal void TransitionToWalking()
    {
        anim.applyRootMotion = true;
        anim.CrossFade("IdleWalk", 0.1f);
      
    }
   

    float syncTime = -1;
    float totalSyncTime = 1.0f;
    void Update()
    {
        if (syncTime >= 0)
        {
            syncTime -= Time.deltaTime;
            float T = Mathf.Clamp01(1 - syncTime / totalSyncTime);
            transform.position = Vector3.Lerp(transform.position, syncToLoc, T);
            transform.rotation = Quaternion.Slerp(transform.rotation, syncToRot, T);
        }

        float animWalkSpeed = fwdVel * 8.0f;
        anim.SetFloat("vSpeed", animWalkSpeed);
    }
    internal float fwdVel;
    public float velSpeedScale = 8.0f;


}
