﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ambulance : Resource
{
    public float speed;
    public float maxSpeed;
    public ERCAgent myERC;
    static public int maxPeople = 2;
    public Boundary boundary;
    public Rigidbody rb;
    public float maxWaitTime = 5;
    private Emergency myEmergency;
    private float epsilon = 3f;
    private int peopleToTransport = 0;
    private float waitTime = 0;
    public float preparationTime = 2;
    private float timePassedAfterComeBack = 0;
    //States
    public bool goingToEmergency;
    public bool onEmergency;
    public bool goingToERC;
    public bool returnedToERC;
    public bool free;

    public bool Decentralized { get; set; }

    public void Start()
    {
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
            if (myERC.TheresMedicalEmergency())
            {
                AssumeEmergency();
            }
        }
    }

    public void SendAmbulance(Emergency em)
    {
        myEmergency = em;
        free = false;
        goingToEmergency = true;
    }

    public void AssumeEmergency()
    {
        SendAmbulance(myERC.WorstMedicalEmergency());
        myERC.SendAmbulance(this);
        myEmergency.SendResources(1, 0);

        if (myEmergency.NeededAmbulances() < 1)
        {
            myERC.MedicalEmergencyControlled(myEmergency);
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
                ReturnAmbulance();
                timePassedAfterComeBack = 0;
            }

            else
            {
                timePassedAfterComeBack += Time.deltaTime;
            }

        }
    }

    private void ReturnAmbulance()
    {
        myERC.ReturnAmbulance(this);
        free = true;
        returnedToERC = false;
        if (peopleToTransport == 0)
        {
            myERC.wastedAmbulances += 1;
        }
        peopleToTransport = 0;
    }

    private void TreatEmergency()
    {
        if (Decentralized && myEmergency.NAmbulances == 1 && peopleToTransport == maxPeople && myEmergency.GetEmergencyPeopleEnvolved() > 0)
        {
            onEmergency = false;
            goingToERC = true;
            myEmergency.NAmbulances -= 1;
            if (!myERC.MedicalEmergenciesWaiting.Contains(myEmergency))
            {
                myERC.MedicalEmergenciesWaiting.Add(myEmergency);
            }
            if (myEmergency.NFiretrucks <= 0)
            {
                myERC.EmergenciesBeingTreated.Remove(myEmergency);
            }
            return;
        }

        if (myEmergency.GetEmergencyPeopleEnvolved() < 1)
        {
            onEmergency = false;
            goingToERC = true;
            myEmergency.NAmbulances -= 1;
            if (Decentralized && myEmergency.NAmbulances <= 0 && myEmergency.NFiretrucks <= 0 && myEmergency.GetEmergencyDisasterLife() <= 0)
            {
                myERC.MedicalEmergenciesWaiting.Remove(myEmergency);
                myERC.DisasterEmergenciesWaiting.Remove(myEmergency);
                myERC.EmergencyEnded(myEmergency);
            }

            //else if (Decentralized && myEmergency.NAmbulances <= 0 && myEmergency.NFiretrucks <= 0 && myEmergency.GetEmergencyDisasterLife() >= 250)
            //{
            //    myERC.EmergencyImpossible(myEmergency);
            //}

            else if (Decentralized && myEmergency.NAmbulances <= 0 && myEmergency.NFiretrucks <= 0)
            {
                myERC.EmergenciesBeingTreated.Remove(myEmergency);
            }
            return;
        }

        if (peopleToTransport == maxPeople)
        {
            onEmergency = false;
            goingToERC = true;
            myEmergency.NAmbulances -= 1;
            return;
        }

        if (waitTime >= myEmergency.WaitTime)
        {
            if(myEmergency.TreatEmergency(this))
            {
                peopleToTransport++;
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

    internal void RestartAmbulance()
    {
        peopleToTransport = 0;
        var pos = myERC.gameObject.transform.position;
        pos.y = 1;
        gameObject.transform.position = pos;

        speed = 0;
        returnedToERC = true;
        onEmergency = false;
        goingToEmergency = false;
        goingToERC = false;
        free = true;

    }
}
