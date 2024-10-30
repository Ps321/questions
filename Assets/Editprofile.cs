using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;

public class Editprofile : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField fullNameInput;
    public TMP_InputField usernameInput;
    public TMP_InputField phoneNumberInput;
    private FirebaseFirestore firestore;
    void Start()
    {

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                firestore = FirebaseFirestore.DefaultInstance;
                Debug.Log("Firebase initialized successfully");
            }
            else
            {
                Debug.LogError("Firebase initialization failed: " + task.Exception);
                ToastNotification.Show("Firebase initialization failed", 3.0f, "error");
            }
        });
        emailInput.text=PlayerPrefs.GetString("email");
        fullNameInput.text= PlayerPrefs.GetString("name");
        usernameInput.text= PlayerPrefs.GetString("username");
        phoneNumberInput.text=PlayerPrefs.GetString("pno");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Save(){
        CheckIfUserExists(emailInput.text,usernameInput.text,phoneNumberInput.text,fullNameInput.text);
    }

   private void CheckIfUserExists(string email, string username, string phoneNumber, string fullName)
{
    CollectionReference usersRef = firestore.Collection("users");

    // Query for each field independently
    Query emailQuery = usersRef.WhereEqualTo("email", email);
    Query usernameQuery = usersRef.WhereEqualTo("username", username);
    Query phoneNumberQuery = usersRef.WhereEqualTo("phoneNumber", phoneNumber);

    // Check each field one by one
    emailQuery.GetSnapshotAsync().ContinueWithOnMainThread(emailTask =>
    {
        if (emailTask.IsCompleted && emailTask.Result.Count > 0)
        {
            ToastNotification.Show("Email already exists!", 3.0f, "error");
        }
        else
        {
            // Check username
            usernameQuery.GetSnapshotAsync().ContinueWithOnMainThread(usernameTask =>
            {
                if (usernameTask.IsCompleted && usernameTask.Result.Count > 0)
                {
                    ToastNotification.Show("Username already exists!", 3.0f, "error");
                }
                else
                {
                    // Check phone number
                    phoneNumberQuery.GetSnapshotAsync().ContinueWithOnMainThread(phoneTask =>
                    {
                        if (phoneTask.IsCompleted && phoneTask.Result.Count > 0)
                        {
                            ToastNotification.Show("Phone number already exists!", 3.0f, "error");
                        }
                        else
                        {
                            // All checks passed, save the user
                            SaveUserToFirestore(PlayerPrefs.GetString("uid"), email, fullName, username, phoneNumber);
                        }
                    });
                }
            });
        }
    });
}


    private void SaveUserToFirestore(string uid, string email, string fullName, string username, string phoneNumber)
{
    // Reference to the specific document with the uid
    DocumentReference userDocRef = firestore.Collection("users").Document(uid);

    var userData = new
    {
        email = email,
        fullName = fullName,
        username = username,
        phoneNumber = phoneNumber
    };

    // Use SetAsync to update the document, which will create it if it doesn’t exist
    userDocRef.SetAsync(userData).ContinueWithOnMainThread(task =>
    {
        if (task.IsCompleted)
        {
            ToastNotification.Show("User data updated successfully!", 3.0f, "success");
        }
        else
        {
            ToastNotification.Show("Error updating user data!", 3.0f, "error");
        }
    });
}


}
