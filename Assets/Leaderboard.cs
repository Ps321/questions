using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using TMPro;

public class LeaderboardManager : MonoBehaviour
{
    [Header("Firestore Settings")]
    public FirebaseFirestore db;

    [Header("UI Elements")]
    public GameObject entryPrefab;           // Prefab for leaderboard entry
    public Transform scrollViewContent;      // Content object inside the scroll view

    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance; // Ensure Firebase is initialized
        FetchTop10LeaderboardEntries();
    }

    private void FetchTop10LeaderboardEntries()
    {
        db.Collection("leaderboard")
            .OrderByDescending("score")      // Sort by 'score' in descending order
            .Limit(10)                       // Limit to top 10 entries
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error fetching leaderboard data: " + task.Exception);
                    return;
                }

                QuerySnapshot snapshot = task.Result;
                PopulateLeaderboard(snapshot.Documents);
            });
    }

    private void PopulateLeaderboard(IEnumerable<DocumentSnapshot> documents)
    {
        // Clear existing entries
        foreach (Transform child in scrollViewContent)
        {
            Destroy(child.gameObject);
        }

        int index=1;
        foreach (DocumentSnapshot document in documents)
        {
            if (document.Exists)
            {
                // Instantiate a new entry prefab
                GameObject entry = Instantiate(entryPrefab, scrollViewContent);

                // Retrieve fields from Firestore document
                string playerName = document.GetValue<string>("name");
              
                int score = document.GetValue<int>("score");

                if (document.GetValue<string>("profilePictureUrl") != null && document.GetValue<string>("profilePictureUrl") != "")
                {
                    string profilePictureUrl =document.GetValue<string>("profilePictureUrl");
                    PlayerPrefs.SetString("profilepic", profilePictureUrl);
                    StartCoroutine(LoadProfilePicture(entry.transform.GetChild(0).GetComponent<Image>(),profilePictureUrl));
                }
                else
                {
                    Debug.Log("No profile picture found!");
                }
                entry.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).GetComponent<TMP_Text>().text =index.ToString();
                entry.transform.GetChild(1).GetComponent<TMP_Text>().text =playerName;
                entry.transform.GetChild(2).GetComponent<TMP_Text>().text =score.ToString();
                index++;

            }
        }
    }

     private IEnumerator LoadProfilePicture(Image profileImage,string url)
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
}
