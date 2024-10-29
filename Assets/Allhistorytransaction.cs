using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Allhistorytransaction : MonoBehaviour
{
     public GameObject transactionPrefab;  // Reference to the prefab
     public Transform transactionContainer;

    private FirebaseFirestore db;
    public Sprite[] arrows;
    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        LoadAndDisplayTransactions();

    }
   void LoadAndDisplayTransactions(string transactionType = "all", string walletType = "all", float minAmount = 0f)
{
    try
    {
        // Fetch transactions where userId matches and apply other conditions as necessary
        Query transactionQuery = db.Collection("transactions")
            .WhereEqualTo("userId", PlayerPrefs.GetString("uid"));

        if (transactionType != "all")
            transactionQuery = transactionQuery.WhereEqualTo("transactionType", transactionType);

        if (walletType != "all")
            transactionQuery = transactionQuery.WhereEqualTo("walletType", walletType);

        transactionQuery.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            try
            {
                if (task.IsCompleted)
                {
                    foreach (DocumentSnapshot document in task.Result.Documents)
                    {
                        // Get transaction data and filter by amount if necessary
                        float amount = document.GetValue<float>("amount");
                        if (amount >= minAmount)
                        {
                            // Instantiate the prefab and set it as a child of the container
                            if (document.GetValue<string>("transactionType") == "Deposit")
                            {
                                transactionPrefab.transform.GetChild(0).GetComponent<Image>().sprite = arrows[0];
                                transactionPrefab.transform.GetChild(1).GetComponent<TMP_Text>().text = document.GetValue<string>("walletType") == "Cash"?"Cash Deposit Successfully!":"Credit Deposit Successfully!";
                            }
                            else
                            {
                                transactionPrefab.transform.GetChild(0).GetComponent<Image>().sprite = arrows[1];
                                transactionPrefab.transform.GetChild(1).GetComponent<TMP_Text>().text = "Cash Withdraw Successfully!";
                            }

                            transactionPrefab.transform.GetChild(2).GetComponent<TMP_Text>().text = document.GetValue<Timestamp>("timestamp").ToDateTime().ToString();
                            transactionPrefab.transform.GetChild(3).transform.GetChild(0).GetComponent<TMP_Text>().text = amount.ToString("F2");

                            GameObject transactionInstance = Instantiate(transactionPrefab, transactionContainer);
                        }
                    }
                }
                else
                {
                    Debug.LogError("Failed to load transactions: " + task.Exception);
                }
            }
            catch (Exception  ex)
            {
                Debug.LogError("An error occurred while processing transactions: " + ex.Message);
            }
        });
    }
    catch (Exception ex)
    {
        Debug.LogError("An error occurred in LoadAndDisplayTransactions: " + ex.Message);
    }
}

}
