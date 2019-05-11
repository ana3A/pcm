﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BothEmergency : Emergency
{
    public int NPeopleInvolved;
    public int totalPeople;
    public int savedPeople;
    public int successRate;
    private float SalvationProb;

    public float DevastationLife;
    public float LightDevastationLife;
    public float MediumDevastationLife;
    public float SevereDevastationLife;

    private float regainEnergyPercentage;
    public float LightRegainEnergyPercentage;
    public float MediumRegainEnergyPercentage;
    public float SevereRegainEnergyPercentage;

    public void InitEmergency(E_Severity severity, int peopleInvolved, UrbanArea area)
    {
        base.InitEmergency(area, severity, this.GetComponent<Renderer>().material);
        this.NPeopleInvolved = peopleInvolved;
        this.totalPeople = peopleInvolved;
        this.successRate = -1;
        this.SalvationProb = 0.99f;
        NewSeverity(severity);
        ChangeWaitTime();
        
    }

    public override void NewSeverity(E_Severity severity)
    {
    
        if (severity == E_Severity.Light)
        {
            SalvationProb = 0.99f;
            SetEmergencySeverity(severity);
            DevastationLife = LightDevastationLife;
            regainEnergyPercentage = LightRegainEnergyPercentage;
            MyMaterial.color = new Color(0f / 255f, 255f / 255f, 0f);
        }

        else if (severity == E_Severity.Medium)
        {
            SalvationProb = 0.70f;
            SetEmergencySeverity(severity);
            DevastationLife = MediumDevastationLife;
            regainEnergyPercentage = MediumRegainEnergyPercentage;
            MyMaterial.color = new Color(130f / 255f, 255f / 255f, 0f);
        }
        else
        {
            SalvationProb = 0.50f;
            SetEmergencySeverity(severity);
            DevastationLife = SevereDevastationLife;
            regainEnergyPercentage = SevereRegainEnergyPercentage;
            MyMaterial.color = new Color(255f / 255f, 255f / 255f, 0f);
        }
    }

    public override void ChangeSeverity(E_Severity severity)
    {

        if (severity == E_Severity.Medium)
        {
            SetEmergencySeverity(severity);
            SalvationProb = 0.70f;
            regainEnergyPercentage = MediumRegainEnergyPercentage;
            DevastationLife += DevastationLife * 0.25f;
            MyMaterial.color = new Color(130f / 255f, 255f / 255f, 0f);
        }
        else
        {
            SetEmergencySeverity(severity);
            SalvationProb = 0.50f;
            regainEnergyPercentage = SevereRegainEnergyPercentage;
            DevastationLife += DevastationLife * 0.5f;
            MyMaterial.color = new Color(255f / 255f, 255f / 255f, 0f);
        }
    }

    public override int GetEmergencyPeopleEnvolved()
    {
        return NPeopleInvolved;
    }

    public override void SendResources(int ambulances, int firetrucks)
    {
        if (firetrucks != 0)
        {
            this.NFiretrucks = firetrucks;
        }

        if (ambulances != 0)
        {
            this.NAmbulances = ambulances;
        }
    }

    public override bool TreatEmergency(Firetruck f)
    {
        DevastationLife -= Firetruck.damage;
        return true;
    }

    public override bool TreatEmergency(Ambulance a)
    {
        float salvationProb = UnityEngine.Random.Range(0f, 1f);
        if (salvationProb <= this.SalvationProb)
        {
            NPeopleInvolved -= 1;
            savedPeople += 1;
            successRate = savedPeople / totalPeople;
            MyArea.Saved();
            return true;
        }
        NPeopleInvolved -= 1;
        successRate = savedPeople / totalPeople;
        MyArea.NotSaved();
        return false;
    }

    public override float GetEmergencyDisasterLife()
    {
        return DevastationLife;
    }

    public override void CheckIfDead()
    {
        float dyingProb = UnityEngine.Random.Range(0f, 1f);
        if (Severity == E_Severity.Medium)
        {
            if (dyingProb <= 0.001)
            {
                Debug.Log("medium dead" + dyingProb);
                NPeopleInvolved -= 1;
                MyArea.NotSaved();
            }
        }
        else if (Severity == E_Severity.Severe)
        {
            if (dyingProb <= 0.005)
            {
                Debug.Log("severe dead" + dyingProb);
                NPeopleInvolved -= 1;
                MyArea.NotSaved();
            }
        }

    }

    // Start is called before the first frame update
    new void Start()
    {

    }

    // Update is called once per frame
    new void Update()

    {
        Duration += Time.deltaTime;

        if (this.NPeopleInvolved <= 0 && this.DevastationLife <= 0)
        {
            //notify area +central
            MyArea.RemoveEmergency(this, successRate);
            Destroy(this.gameObject);
        }

        else
        {
            if (this.NAmbulances == 0 && this.NPeopleInvolved >= 1)
            {
                this.NAmbulances = -1;
                MyArea.ReOpenMedicalEmergency(this);
            }

            if (this.NFiretrucks == 0 && this.DevastationLife >= 1)
            {
                this.NFiretrucks = -1;
                MyArea.ReOpenDisasterEmergency(this);
            }

            if (this.NPeopleInvolved >= 1)
            {
                CheckIfDead();
            }

            if (this.DevastationLife > 0)
            {
                DevastationLife += regainEnergyPercentage * DevastationLife;
            }

            IncreaseSeverity();
        }

    }
}