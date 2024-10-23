using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    public void Login()
    {
        SceneManager.LoadScene(1);
    }
}
