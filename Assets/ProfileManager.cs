using System.Collections;
using System.Collections.Generic;
using System.IO;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Storage;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ProfileManager : MonoBehaviour
{
    public FirebaseStorage storage;
    public FirebaseFirestore firestore;
    public TextMeshProUGUI statusText;
    public Button uploadButton;
    public Image profileImage;

    private string filePath;

    void Start()
    {
        storage = FirebaseStorage.DefaultInstance;
        firestore = FirebaseFirestore.DefaultInstance;

        uploadButton.onClick.AddListener(PickImage);
        string profilepicture=PlayerPrefs.GetString("profilepic");
        
        if(profilepicture!="")StartCoroutine(LoadProfilePicture(profilepicture));
    }

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
    // Open the image picker
    private void PickImage()
    {
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                filePath = path;
                // Load the selected image into the UI
                LoadImage(path);
            }
        }, "Select a Profile Picture", "image/*");

        if (permission != NativeGallery.Permission.Granted)
        {
            statusText.text = "Permission to access gallery was denied!";
        }
    }

    // Display the selected image in the UI
    private void LoadImage(string imagePath)
    {
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageBytes);

        // Display the image on the UI
        profileImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        UploadProfilePicture();
    }

    // Call this method to upload the image to Firebase Storage
    public void UploadProfilePicture()
    {
        if (string.IsNullOrEmpty(filePath))
        {
            statusText.text = "No image selected!";
            return;
        }

        // Create a reference to the storage location
        string userId = PlayerPrefs.GetString("uid"); // Replace with the actual user ID
        StorageReference storageRef = storage.GetReference($"profile_pictures/{userId}.jpg");

        // Upload the file to Firebase Storage
        storageRef.PutFileAsync(filePath).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                // Get the download URL after successful upload
                storageRef.GetDownloadUrlAsync().ContinueWithOnMainThread(urlTask =>
                {
                    if (urlTask.IsCompleted)
                    {
                        string downloadUrl = urlTask.Result.ToString();
                        statusText.text = "Image uploaded successfully!";

                        // Store the download URL in Firestore under user data
                        SaveProfileUrlToFirestore(userId, downloadUrl);
                    }
                });
            }
            else
            {
                statusText.text = "Image upload failed!";
            }
        });
    }

    // Save the image URL to Firestore under user details
    private void SaveProfileUrlToFirestore(string userId, string profileUrl)
    {
        DocumentReference userRef = firestore.Collection("users").Document(userId);

        userRef.UpdateAsync(new Dictionary<string, object>
        {
            { "profilePictureUrl", profileUrl }
        }).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                statusText.text = "Profile picture URL saved successfully!";
            }
            else
            {
                statusText.text = "Failed to save profile URL!";
            }
        });
    }
}
