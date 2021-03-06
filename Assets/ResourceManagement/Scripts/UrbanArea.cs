﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class UrbanArea : MonoBehaviour
{
    public string sceneName;
    public DataHolder dataHolder = null;
    public GameObject EmergencyObject;
    public GameObject DisasterEmergencyObject;
    public GameObject MedicalEmergencyObject;
    public GameObject BothEmergencyObject;
    //public int numBananas;
    public float range;
    public float TimeBetween;
    public ERCAgent MyERC;
    public int atualEmergencies = 0;
    public int maxEmergencies = 15;
    public int maxFailures = 20;
    public int nRestarts;
    public int allPeople = 0;
    public int peopleSaved = 0;
    public int peopleNotSaved = 0;
    public int failures = 0;
    public Text restartText;
    private bool restart;
    public Text gameOverText;
    //private bool gameOver;

    public List<float> responseTimes = new List<float>();
    public List<int> peopleSavedList = new List<int>();
    public List<int> peopleNotSavedList = new List<int>();
    public List<int> allPeopleList = new List<int>();
    public List<float> burnedRatio = new List<float>();
    public float runningTime = 0f;
    public float maxResponseTime = -1f;
    public int maxPeopleSaved = 0;
    public int maxPeopleNotSaved = 0;
    public int ImpossibleEmergencies = 0;

    enum E_Type { Medical, Disaster, Both}

    //Key: position Value: emergency
    private Dictionary<Vector3, GameObject> MyEmergencies;
    private IEnumerator MakeEmergenciesOccur(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);
            
            //if (atualEmergencies == maxEmergencies)
            //{
            //    gameOver = true;
            //}

            if (atualEmergencies < maxEmergencies)
            {
                var em_prob = Random.Range(0f, 1f);
                if (em_prob <= 0.05)
                {
                    atualEmergencies += 1;
                    CreateEmergency();
                }
            }
        }
    }

    // Use this for initialization
    void Start()
    {
        restart = false;
        restartText.text = "";
        gameOverText.text = "";
        //gameOver = false;
        MyEmergencies = new Dictionary<Vector3, GameObject>();
        var coroutine = MakeEmergenciesOccur(TimeBetween);
        StartCoroutine(coroutine);
        runningTime = Time.time;
        //dataHolder 
        //nRestarts = dataHolder.nRestarts;
    }

    // Update is called once per frame
    void Update()
    {
        if (restart)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        if ((MyERC.getGameOver() || (failures > maxFailures)) && DataHolder.instance.nRestarts >= 1)
        {
            peopleSavedList.Add(peopleSaved);
            peopleNotSavedList.Add(allPeople - peopleSaved);
            allPeopleList.Add(allPeople);

            maxPeopleSaved = Mathf.Max(maxPeopleSaved, peopleSaved);
            maxPeopleNotSaved = Mathf.Max(maxPeopleNotSaved, allPeople - peopleSaved);

            float rTimes = 0;
            for (var i = 0; i < responseTimes.Count; i++)
            {
                rTimes += responseTimes[i];
            }
            rTimes = rTimes / responseTimes.Count;

            float bRatios = 0;
            for (var i = 0; i < burnedRatio.Count; i++)
            {
                bRatios += burnedRatio[i];
            }
            bRatios = bRatios / burnedRatio.Count;

            DataHolder.instance.SendData(rTimes, peopleSavedList, peopleNotSavedList, allPeopleList, maxResponseTime, maxPeopleSaved, maxPeopleNotSaved, Time.time - runningTime, bRatios, ImpossibleEmergencies, MyERC.wastedAmbulances, MyERC.wastedFiretrucks);


            if (DataHolder.instance.nRestarts == 1)
            {
                DataHolder.instance.WriteFile();
                restart = true;
                gameOverText.text = "Simulation Failed!";
                restartText.text = "Press 'R' for Restart";
                Time.timeScale = 0;
                DataHolder.instance.nRestarts--;
            }
            else
            {
                DataHolder.instance.nRestarts--;
                RestartAll();
            }
        }

        //else if ((MyERC.getGameOver() || (failures > maxFailures)) && DataHolder.instance.nRestarts == 1)
        //{
        //    peopleSavedList.Add(peopleSaved);
        //    peopleNotSavedList.Add(allPeople - peopleSaved);
        //    allPeopleList.Add(allPeople);

        //    maxPeopleSaved = Mathf.Max(maxPeopleSaved, peopleSaved);
        //    maxPeopleNotSaved = Mathf.Max(maxPeopleNotSaved, allPeople - peopleSaved);

        //    float rTimes = 0;
        //    for (var i = 0; i < responseTimes.Count; i++)
        //    {
        //        rTimes += responseTimes[i];
        //    }
        //    rTimes = rTimes / responseTimes.Count;

        //    float bRatios = 0;
        //    for (var i = 0; i < burnedRatio.Count; i++)
        //    {
        //        bRatios += burnedRatio[i];
        //    }
        //    bRatios = bRatios / burnedRatio.Count;

        //    DataHolder.instance.SendData(rTimes, peopleSavedList, peopleNotSavedList, allPeopleList, maxResponseTime, maxPeopleSaved, maxPeopleNotSaved, Time.time - runningTime, bRatios, ImpossibleEmergencies, MyERC.wastedAmbulances, MyERC.wastedFiretrucks);
        //    DataHolder.instance.WriteFile();
        //    restart = true;
        //    gameOverText.text = "Simulation Failed!";
        //    restartText.text = "Press 'R' for Restart";
        //    Time.timeScale = 0;
        //    DataHolder.instance.nRestarts--;
        //}
    }

    void CreateEmergency()
    {
        float type_probability = Random.Range(0f, 1f);
        Emergency.E_Severity Eserv;
        int people_involved = -1;
        E_Type Etype;

        if (type_probability <= 0.6f)
        {
            float x = (float)Random.Range((int)1, (int)21) / 20;         //max(21) is exclusive
            x = Mathf.Pow(-x + 1, 3);                                   //chance distribution. less people is more probable

            people_involved = (int)Mathf.Max(x * 20, 1);

            Etype = E_Type.Medical;
        }
        else if (type_probability <= 0.8f)
        {
            Etype = E_Type.Disaster;
        }
        else
        {
            float x = (float)Random.Range((int)1, (int)21) / 20;         //max(21) is exclusive
            x = Mathf.Pow(-x + 1, 3);                                   //chance distribution. less people is more probable

            people_involved = (int)Mathf.Max(x * 20, 1);

            Etype = E_Type.Both;
        }

        float creation_probability = Random.Range(0.0f, 1.0f);
        if (creation_probability <= 0.6f)
        {
            Eserv = Emergency.E_Severity.Light;
        }
        else if (creation_probability <= 0.9f)
        {
            Eserv = Emergency.E_Severity.Medium;
        }
        else
        {
            Eserv = Emergency.E_Severity.Severe;
        }

        Vector3 pos = new Vector3(Random.Range(-range, range), 1f, Random.Range(-range, range)) + transform.position;
        while (this.MyEmergencies.ContainsKey(pos) || TooCloseToCentral(pos))
        {
            pos = new Vector3(Random.Range(-range, range), 1f, Random.Range(-range, range)) + transform.position;
        }
        

        if (Etype == E_Type.Medical)
        {
            GameObject em = Instantiate(MedicalEmergencyObject, pos, Quaternion.Euler(new Vector3(0f, 0f, 0f)));
            MedicalEmergency e = em.GetComponent<MedicalEmergency>();
            e.InitEmergency(Eserv, people_involved, this);
            MyERC.EmergencyCall(e);
            //Notify Area that emergency exists
            MyEmergencies.Add(em.gameObject.transform.position, em);
        }

        else if (Etype == E_Type.Disaster)
        {
            GameObject em = Instantiate(DisasterEmergencyObject, pos, Quaternion.Euler(new Vector3(0f, 0f, 0f)));
            DisasterEmergency e = em.GetComponent<DisasterEmergency>();
            e.InitEmergency(Eserv, people_involved, this);
            MyERC.EmergencyCall(e);
            //Notify Area that emergency exists
            MyEmergencies.Add(em.gameObject.transform.position, em);
        }

        else if (Etype == E_Type.Both)
        {
            GameObject em = Instantiate(BothEmergencyObject, pos, Quaternion.Euler(new Vector3(0f, 0f, 0f)));
            BothEmergency e = em.GetComponent<BothEmergency>();
            e.InitEmergency(Eserv, people_involved, this);
            MyERC.EmergencyCall(e);
            //Notify Area that emergency exists
            MyEmergencies.Add(em.gameObject.transform.position, em);
        }


        //Notify ERC that an emergency occured -> simulating the "calls"

    }

    private bool TooCloseToCentral(Vector3 pos) {
        if (Vector3.Distance(pos, MyERC.transform.position) < 5)
            return true;
        return false;
    }
    public void RemoveEmergency(Emergency em)
    {
        MyEmergencies.Remove(em.transform.position);
        MyERC.EmergencyEnded(em);
    }

    public void AddEmergencyStatistics(Emergency em, float successRate)
    {
        if (successRate < 0.5)
        {
            failures +=1;
        }
        //MyEmergencies.Remove(em.transform.position);
        //MyERC.EmergencyEnded(em);
    }

    public void Saved()
    {
        peopleSaved += 1;
        allPeople += 1;
    }

    public void NotSaved()
    {
        peopleNotSaved += 1;
        allPeople += 1;
    }

    public void Ratio(float r)
    {
        burnedRatio.Add(r);
    }

    public void ReOpenEmergency(DisasterEmergency em)
    {
        MyERC.EmergencyReOpen(em);
    }

    public void ReOpenEmergency(MedicalEmergency em)
    {
        MyERC.EmergencyReOpen(em);
    }

    public void ReOpenMedicalEmergency(BothEmergency em)
    {
        MyERC.ReOpenMedEmergency(em);
    }

    public void ReOpenDisasterEmergency(BothEmergency em)
    {
        MyERC.ReOpenDisEmergency(em);
    }


    public void ResetUrbanArea()
    {
        MyEmergencies.Clear();
    }

    public void RestartAll()
    {
        //nRestarts -= 1;
        //atualEmergencies = 0;
        //allPeople = 0;
        //peopleSaved = 0;
        //peopleNotSaved = 0;
        //failures = 0;
        //foreach (Vector3 e in MyEmergencies.Keys)
        //{
        //    if (MyEmergencies.ContainsKey(e))
        //    {
        //        Debug.Log(e);
        //        Destroy(MyEmergencies[e].gameObject);
        //    }
        //}
        //MyEmergencies.Clear();
        //MyERC.RestartERC();

        SceneManager.LoadScene(sceneName);
    }

}
