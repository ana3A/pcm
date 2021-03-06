﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Firetruck : Resource
{
    public float speed;
    public float maxSpeed;
    public ERCAgent myERC;
    public Boundary boundary;
    public Rigidbody rb;
    public float maxWaitTime = 5;
    private Emergency myEmergency;
    private float epsilon = 3f;
    private float waitTime = 0;
    static public int damage = 3;
    static public float waterDeposit = 30;
    private float curWaterDeposit = waterDeposit;
    public float preparationTime = 2;
    //States
    public bool goingToEmergency;
    public bool onEmergency;
    public bool goingToERC;
    public bool returnedToERC;
    public bool free;
    private float timePassedAfterComeBack;

    public bool Decentralized { get; set; }

    public void SendFiretruck(Emergency em)
    {
        myEmergency = em;
        goingToEmergency = true;
        returnedToERC = false;
        free = false;
    }

    void Start()
    {
        curWaterDeposit = waterDeposit;
        returnedToERC = true;
        onEmergency = false;
        goingToEmergency = false;
        goingToERC = false;
        free = true;
    }

    void Update()
    {
        if (!free)
        {
            DealWithEmergency();
        }

        else if (Decentralized)
        {
            if (myERC.TheresDisasterEmergency())
            {
                AssumeEmergency();
            }
        }
    }

    public void AssumeEmergency()
    {
        SendFiretruck(myERC.WorstDisasterEmergency());
        myERC.SendFiretruck(this);
        myEmergency.SendResources(0, 1);

        if (myEmergency.NeededFiretrucks() < 1 || myEmergency.GetEmergencyDisasterLife() > 250)
        {
            myERC.DisasterEmergencyControlled(myEmergency);
        }

    }


    private void DealWithEmergency()
    {
        if (goingToEmergency)
        {
            Move(myEmergency.gameObject);
        }

        else if (onEmergency)
        {
            rb.velocity = Vector3.zero;
            TreatEmergency();
        }

        else if (goingToERC)
        {
            Move(myERC.gameObject);
        }

        else if (returnedToERC)
        {
            if (timePassedAfterComeBack >= preparationTime)
            {
                ReturnFiretruck();
                timePassedAfterComeBack = 0;
            }

            else
            {
                timePassedAfterComeBack += Time.deltaTime;
            }
        }
    }

    private void ReturnFiretruck()
    {
        myERC.ReturnFiretruck(this);
        free = true;
        returnedToERC = false;
        if (curWaterDeposit == waterDeposit)
        {
            myERC.wastedFiretrucks += 1;
        }
        curWaterDeposit = waterDeposit;
    }

    private void TreatEmergency()
    {

        if (Decentralized && myEmergency.NFiretrucks == 1 && curWaterDeposit <=0 && myEmergency.GetEmergencyDisasterLife() > 0)
        {
            onEmergency = false;
            goingToERC = true;
            myEmergency.NFiretrucks -= 1;
            if (!myERC.DisasterEmergenciesWaiting.Contains(myEmergency))
            {
                myERC.DisasterEmergenciesWaiting.Add(myEmergency);
            }
            if (myEmergency.NAmbulances <= 0)
            {
                myERC.EmergenciesBeingTreated.Remove(myEmergency);
            }
            return;
        }

        if (myEmergency.GetEmergencyDisasterLife() <= 0)
        {
            onEmergency = false;
            goingToERC = true;
            myEmergency.NFiretrucks -= 1;
            if (Decentralized && myEmergency.NAmbulances <= 0 && myEmergency.NFiretrucks <= 0 && myEmergency.GetEmergencyPeopleEnvolved() < 1)
            {
                myERC.MedicalEmergenciesWaiting.Remove(myEmergency);
                myERC.DisasterEmergenciesWaiting.Remove(myEmergency);
                myERC.EmergencyEnded(myEmergency);
            }

            else if (Decentralized && myEmergency.NAmbulances <= 0 && myEmergency.NFiretrucks <= 0)
            {
                myERC.EmergenciesBeingTreated.Remove(myEmergency);
            }
            return;
        }

        if (myEmergency.GetEmergencyDisasterLife() >= 250)
        {
            onEmergency = false;
            goingToERC = true;
            myEmergency.NFiretrucks -= 1;
            if (Decentralized && myEmergency.NAmbulances <= 0 && myEmergency.NFiretrucks <= 0 && myEmergency.GetEmergencyPeopleEnvolved() < 1)
            {
                myERC.MedicalEmergenciesWaiting.Remove(myEmergency);
                myERC.DisasterEmergenciesWaiting.Remove(myEmergency);
                myERC.EmergencyImpossible(myEmergency);
            }
            else if (Decentralized && myEmergency.NFiretrucks <= 0)
            {
                myERC.DisasterEmergenciesWaiting.Add(myEmergency);
                if(myEmergency.NAmbulances <= 0)
                {
                    myERC.EmergenciesBeingTreated.Remove(myEmergency);
                }
            }
            return;
        }

        if (curWaterDeposit <= 0)
        {
            onEmergency = false;
            goingToERC = true;
            myEmergency.NFiretrucks -= 1;
            return;
        }

        if (waitTime >= myEmergency.WaitTime)
        {
            if (myEmergency.TreatEmergency(this))
            {
                curWaterDeposit -= damage;
            }
            waitTime = 0;
        }

        else
        {
            waitTime += Time.deltaTime;
        }

    }

    private void Move(GameObject target)
    {
        if (Vector3.Distance(target.transform.position, gameObject.transform.position) < epsilon)
        {
            if (goingToEmergency)
            {
                goingToEmergency = false;
                onEmergency = true;
            }
            else
            {
                goingToERC = false;
                returnedToERC = true;
            }
            return;
        }

        Vector3 movement = target.transform.position - gameObject.transform.position;

        rb.velocity = movement * speed;

        //rb.position = new Vector3
        //(
        //    Mathf.Clamp(rb.position.x, boundary.xMin, boundary.xMax),
        //    1.0f,
        //    Mathf.Clamp(rb.position.z, boundary.zMin, boundary.zMax)
        //);
    }

    internal void RestartFiretruck()
    {
        curWaterDeposit = waterDeposit;
        gameObject.transform.position = myERC.gameObject.transform.position;
        speed = 0;
        returnedToERC = true;
        onEmergency = false;
        goingToEmergency = false;
        goingToERC = false;
        free = true;
    }
}
