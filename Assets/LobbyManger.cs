using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManger : MonoBehaviour
{
    //    public TMP_Text Name;
    public TMP_Text Email;

    public TMP_Text username;
    //    public TMP_Text pno;
    public TMP_Text fiatbalance;
    public TMP_Text cashbalance;
    private FirebaseFirestore db;
    public Image profileImage;

    public TMP_InputField depositcash;
    public TMP_InputField withdrawcash;
    public TMP_InputField depositcredit;

    public TMP_Text availablebalance1;

    public TMP_Text availablebalance2;





    void Start()
    {

        db = FirebaseFirestore.DefaultInstance;
        string phone = PlayerPrefs.GetString("pno");
        if (phone == "")
        {

            SceneManager.LoadScene(0);
            return;
        }

        FetchUserByPhoneNumber(phone);
    }

    // Update is called once per frame
    public void GameScreen()
    {
        SceneManager.LoadScene(2);
    }

    private void FetchUserByPhoneNumber(string phoneNumber)
    {
        try
        {
            // Reference to the "users" collection
            CollectionReference usersCollectionRef = db.Collection("users");

            // Query Firestore for the document where the phone number matches
            Query query = usersCollectionRef.WhereEqualTo("phoneNumber", phoneNumber);

            // Execute the query
            query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    QuerySnapshot snapshot = task.Result;

                    if (snapshot.Count > 0)
                    {
                        // If a matching document is found, display the user details
                        foreach (DocumentSnapshot document in snapshot.Documents)
                        {
                            var userData = document.ToDictionary();

                            // Get the UID of the user
                            string uid = document.Id;
                            Debug.Log($"User UID: {uid}");
                            PlayerPrefs.SetString("uid", uid);
                            PlayerPrefs.SetString("name", userData["fullName"].ToString());
                            PlayerPrefs.SetString("email", userData["email"].ToString());
                            PlayerPrefs.SetString("username", userData["username"].ToString());


                            // Fetch the user details
                            Email.text = userData["email"].ToString();
                            username.text = userData["username"].ToString();
                            float fiatWalletValue = Convert.ToSingle(userData["fiatwallet"]);
                            fiatbalance.text = fiatWalletValue.ToString("F2");
                            float cashWalletValue = Convert.ToSingle(userData["cashwallet"]);
                            cashbalance.text = cashWalletValue.ToString("F2");
                            PlayerPrefs.SetFloat("fiat", fiatWalletValue);
                            PlayerPrefs.SetFloat("cash", cashWalletValue);
                            availablebalance1.text=cashWalletValue.ToString("F2");
                            availablebalance2.text=fiatWalletValue.ToString("F2");


                            // Fetch the profile picture URL and load it
                            if (userData.ContainsKey("profilePictureUrl"))
                            {
                                string profilePictureUrl = userData["profilePictureUrl"].ToString();
                                PlayerPrefs.SetString("profilepic", profilePictureUrl);
                                StartCoroutine(LoadProfilePicture(profilePictureUrl));
                            }
                            else
                            {
                                Debug.Log("No profile picture found!");
                            }

                            // Log or display fetched data
                            Debug.Log($"User Details:\nEmail: {Email.text}\nFiat Balance: {fiatbalance.text}\nCash Balance: {cashbalance.text}");
                        }
                    }
                    else
                    {
                        Debug.Log("No user found with this phone number!");
                        // Signuperrortext.text = "No user found with this phone number!";
                    }
                }
                else
                {
                    Debug.LogError("Error fetching user details!");
                    // Signuperrortext.text = "Error fetching user details!";
                }
            });
        }
        catch (Exception ex)
        {
            // Handle any exception that occurs
            Debug.LogError($"Exception: {ex.Message}");
            // Signuperrortext.text = $"Error occurred: {ex.Message}";
        }
    }

    // Coroutine to load the profile picture from a URL
    private IEnumerator LoadProfilePicture(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        Debug.Log("aaya");
        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            profileImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            Debug.Log("Profile picture loaded successfully.");
        }
        else
        {
            Debug.LogError("Failed to load profile picture: " + request.error);
        }
    }

    public void Reload()
    {
        SceneManager.LoadScene(1);
    }


    public void Logout()
    {
        PlayerPrefs.SetString("pno", "");
        SceneManager.LoadScene(0);
    }


    public void DepositCash()
    {
        if (float.TryParse(depositcash.text, out float depositAmount) && depositAmount > 0)
        {
            Deposit(PlayerPrefs.GetString("uid"), depositAmount);
        }
        else
        {
            Debug.LogError("Invalid input. Please enter a number greater than zero.");
            ToastNotification.Show("Invalid input.\n Please enter a number greater than zero.", 3, "error");
        }
    }

    public void WithdrawCash()
    {
        if (float.TryParse(withdrawcash.text, out float depositAmount) && depositAmount > 0)
        {
            Withdraw(PlayerPrefs.GetString("uid"), depositAmount);
        }
        else
        {
            Debug.LogError("Invalid input. Please enter a number greater than zero.");
            ToastNotification.Show("Invalid input.\n Please enter a number greater than zero.", 3, "error");
        }
    }
    public void DepositFiat()
    {
         if (float.TryParse(depositcredit.text, out float depositAmount) && depositAmount > 0)
        {
            DepositCredit(PlayerPrefs.GetString("uid"), depositAmount);
        }
        else
        {
            Debug.LogError("Invalid input. Please enter a number greater than zero.");
            ToastNotification.Show("Invalid input.\n Please enter a number greater than zero.", 3, "error");
        }
    }


    void Deposit(string userId, float amount)
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                float currentBalance = task.Result.GetValue<float>("cashwallet");
                float newBalance = currentBalance + amount;

                userRef.UpdateAsync("cashwallet", newBalance).ContinueWithOnMainThread(updateTask =>
                {
                    if (updateTask.IsCompleted)
                    {
                        Debug.Log("Deposit successful! New cashwallet balance: " + newBalance);
                      
                            availablebalance1.text=(PlayerPrefs.GetFloat("cash")+amount).ToString();
                            LogTransaction(amount,"Deposit","Cash");
                    }
                    else
                    {
                        Debug.LogError("Failed to deposit: " + updateTask.Exception);
                        ToastNotification.Show("Error submitting Deposit", 3, "error");
                    }
                });
            }
            else
            {
                Debug.LogError("User not found or error fetching data: " + task.Exception);
                ToastNotification.Show("Error submitting Deposit", 3, "error");
            }
        });
    }

    void Withdraw(string userId, float amount)
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                float currentBalance = task.Result.GetValue<float>("cashwallet");

                if (currentBalance >= amount) // Check if balance is sufficient
                {
                    float newBalance = currentBalance - amount;

                    userRef.UpdateAsync("cashwallet", newBalance).ContinueWithOnMainThread(updateTask =>
                    {
                        if (updateTask.IsCompleted)
                        {
                            Debug.Log("Withdrawal successful! New cashwallet balance: " + newBalance);
                           
                            availablebalance1.text=(PlayerPrefs.GetFloat("cash")-amount).ToString();
                            LogTransaction(amount,"Withdraw","Cash");
                        }
                        else
                        {
                            Debug.LogError("Failed to withdraw: " + updateTask.Exception);
                        }
                    });
                }
                else
                {
                    Debug.LogError("Insufficient funds for withdrawal.");
                    ToastNotification.Show("Insufficient funds for withdrawal.", 3, "error");
                }
            }
            else
            {
                Debug.LogError("User not found or error fetching data: " + task.Exception);
                ToastNotification.Show("error while submitting withdrawal.", 3, "error");

            }
        });
    }

    void DepositCredit(string userId, float amount)
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                float currentFiatBalance = task.Result.GetValue<float>("fiatwallet");
                float newFiatBalance = currentFiatBalance + amount;

                userRef.UpdateAsync("fiatwallet", newFiatBalance).ContinueWithOnMainThread(updateTask =>
                {
                    if (updateTask.IsCompleted)
                    {
                        Debug.Log("Deposit to fiatwallet successful! New balance: " + newFiatBalance);
                      
                        availablebalance2.text=(PlayerPrefs.GetFloat("fiat")+amount).ToString();
                         LogTransaction(amount,"Deposit","Fiat");
                    }
                    else
                    {
                        Debug.LogError("Failed to deposit credit: " + updateTask.Exception);
                        ToastNotification.Show("Error submitting Deposit", 3, "error");
                    }
                });
            }
            else
            {
                Debug.LogError("User not found or error fetching data: " + task.Exception);
                ToastNotification.Show("Error submitting Deposit", 3, "error");
            }
        });
    }

    void LogTransaction( float amount, string transactionType, string walletType)
{
    // Get a reference to the transactions collection
    DocumentReference transactionRef = db.Collection("transactions").Document();

    // Prepare transaction data
    var transactionData = new Dictionary<string, object>
    {
        { "userId", PlayerPrefs.GetString("uid") },
        { "name", PlayerPrefs.GetString("name") },
        { "pno", PlayerPrefs.GetString("pno") },
        { "email", PlayerPrefs.GetString("email") },
        { "amount", amount },
        { "transactionType", transactionType },  // "deposit" or "withdraw"
        { "walletType", walletType },           // "cashwallet" or "fiatwallet"
        { "timestamp", FieldValue.ServerTimestamp }  // Server-generated timestamp
    };

    // Add transaction document to Firestore
    transactionRef.SetAsync(transactionData).ContinueWithOnMainThread(task =>
    {
        if (task.IsCompleted)
        {
            Debug.Log("Transaction logged successfully!");
              ToastNotification.Show("Transaction Successfull \n Taking you to Home Page", 3, "success");
                        
                        Invoke("Reload", 3.0f);
        }
        else
        {
            Debug.LogError("Failed to log transaction: " + task.Exception);
        }
    });
}


}
