using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Countdown : MonoBehaviour
{
    public GameObject gametime;
    public GameObject c3;

    public GameObject c2;

    public GameObject c1;
    public GameObject start;

    public GameObject Countdowngameobject;
    public GameManger gameManger;


    void Start()
    {

    }

    public void startit()
    {
        StartCoroutine(Countdown1());
    }

    public IEnumerator Countdown1()
    {
        yield return new WaitForSeconds(4);
        gametime.SetActive(false);
        c3.SetActive(true);
        yield return new WaitForSeconds(1);
        c3.SetActive(false);
        c2.SetActive(true);

        c2.SetActive(true);
        yield return new WaitForSeconds(1);
        c2.SetActive(false);
        c1.SetActive(true);


        c1.SetActive(true);
        yield return new WaitForSeconds(1);
        c1.SetActive(false);
        start.SetActive(true);
        yield return new WaitForSeconds(1);
        Countdowngameobject.SetActive(false);
        gameManger.StartGame();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
