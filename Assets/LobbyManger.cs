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
    private FirebaseFirestore firestore;
     public Image profileImage; 



    void Start()
    {

        firestore = FirebaseFirestore.DefaultInstance;
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
            CollectionReference usersCollectionRef = firestore.Collection("users");

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
                            PlayerPrefs.SetString("uid",uid);
                            PlayerPrefs.SetString("name",userData["fullName"].ToString());
                            PlayerPrefs.SetString("email",userData["email"].ToString());
                            PlayerPrefs.SetString("username",userData["username"].ToString());
                            

                            // Fetch the user details
                            Email.text = userData["email"].ToString();
                            username.text = userData["username"].ToString();
                            float fiatWalletValue = Convert.ToSingle(userData["fiatwallet"]);
                            fiatbalance.text = fiatWalletValue.ToString("F2");
                            float cashWalletValue = Convert.ToSingle(userData["cashwallet"]);
                            cashbalance.text = cashWalletValue.ToString("F2");
                            PlayerPrefs.SetFloat("fiat", fiatWalletValue);
                            PlayerPrefs.SetFloat("cash", cashWalletValue);

                            // Fetch the profile picture URL and load it
                            if (userData.ContainsKey("profilePictureUrl"))
                            {
                                string profilePictureUrl = userData["profilePictureUrl"].ToString();
                                 PlayerPrefs.SetString("profilepic",profilePictureUrl);
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

    public void Reload(){
        SceneManager.LoadScene(1);
    }
    
}
