

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System;
using TMPro;
using UnityEngine.UI;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections;

public class ApiManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField aadhaarNumber;
    public GameObject verificationPanel;
    public TMP_InputField otpInputField;
    public TMP_Text statusMessageText;
    public Button submitAadhaarBtn;
    public Button submitOtpBtn;
    public TMP_InputField phoneNumberField;
    public TMP_Text kycStatusText;
    private Color processingColor = new Color(0.7f, 0.7f, 0.0f); // Yellow
    private Color errorColor = new Color(0.9f, 0.2f, 0.2f);      // Red
    private Color successColor = new Color(0.2f, 0.8f, 0.2f);    // Greenusing System.Collections;

    [Header("API Configuration")]
    private string authorizationToken = "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJmcmVzaCI6ZmFsc2UsImlhdCI6MTc0NTY3MjU0NiwianRpIjoiY2ViYjUzMjAtMWNhZS00ZjVhLWE4MGUtNGRmMWFlYmY4ZDExIiwidHlwZSI6ImFjY2VzcyIsImlkZW50aXR5IjoiZGV2LmtoZWxqdW5jdGlvbjFAc3VyZXBhc3MuaW8iLCJuYmYiOjE3NDU2NzI1NDYsImV4cCI6MjM3NjM5MjU0NiwiZW1haWwiOiJraGVsanVuY3Rpb24xQHN1cmVwYXNzLmlvIiwidGVuYW50X2lkIjoibWFpbiIsInVzZXJfY2xhaW1zIjp7InNjb3BlcyI6WyJ1c2VyIl19fQ.NwVWMeC2kiKVGwPSfoO1aWcGv9ww8u_PryaabnGNNSc";

    private string userId; // The current user's ID

    private string client_id = "";
    private bool isProcessing = false;
    private FirebaseFirestore db;


    private async void Start()
    {
        userId = PlayerPrefs.GetString("uid");
        // Initialize UI
        verificationPanel.SetActive(false);
        statusMessageText.gameObject.SetActive(false);

        // Add button listeners
        submitAadhaarBtn.onClick.AddListener(OnSubmitAadhaarClicked);
        submitOtpBtn.onClick.AddListener(OnSubmitOtpClicked);

        // Initialize Firebase
        await InitializeFirebase();

        // Check if user is already KYC verified
        if (!string.IsNullOrEmpty(userId))
        {
            CheckKycStatus();
        }
        else
        {
            Debug.LogWarning("User ID is not set. Cannot check KYC status.");
        }
    }
    private void UpdateKycStatusInFirestore(string fullName)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Cannot update KYC status: User ID is not set");
            return;
        }

        if (db == null)
        {
            Debug.LogError("Firebase Firestore is not initialized");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(userId);
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "isKycVerified", true },
            { "kycVerifiedAt", FieldValue.ServerTimestamp },
            { "aadhaarName", fullName }
        };

        userRef.UpdateAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error updating KYC status: " + task.Exception);
            }
            else
            {
                Debug.Log("KYC status updated successfully in Firestore");
            }
        });
    }    // Message display colors

    private async Task InitializeFirebase()
    {
        // Initialize Firebase
        await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Create the Firestore instance
                db = FirebaseFirestore.DefaultInstance;
                Debug.Log("Firebase initialized successfully");
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }
    public Button AdharButton;

    private void CheckKycStatus()
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error getting user document: " + task.Exception);
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists)
            {
                Dictionary<string, object> userData = snapshot.ToDictionary();
                if (userData.TryGetValue("isKycVerified", out object isVerified) && isVerified is bool isVerifiedBool && isVerifiedBool)
                {
                    // User is already KYC verified
                    kycStatusText.text = "Verified";
                    kycStatusText.color = successColor;
                    // Optionally disable the KYC verification UI if already verified
                    // aadhaarNumber.interactable = false;
                    AdharButton.interactable = false;
                    submitAadhaarBtn.interactable = false;
                }
                else
                {
                    // User exists but is not KYC verified
                    kycStatusText.text = "Not Verified";
                    kycStatusText.color = errorColor;
                }
            }
            else
            {
                // User document doesn't exist
                Debug.Log("User document does not exist. Creating new user document.");
                Dictionary<string, object> initialData = new Dictionary<string, object>
                {
                    { "isKycVerified", false },
                    { "createdAt", FieldValue.ServerTimestamp }
                };

                userRef.SetAsync(initialData).ContinueWithOnMainThread(setTask =>
                {
                    if (setTask.IsFaulted)
                    {
                        Debug.LogError("Error creating user document: " + setTask.Exception);
                    }
                    else
                    {
                        Debug.Log("Created new user document");
                    }
                });

                kycStatusText.text = "Not Verified";
                kycStatusText.color = errorColor;
            }
        });
    }

    public void OnSubmitAadhaarClicked()
    {
        if (isProcessing)
            return;

        if (string.IsNullOrEmpty(aadhaarNumber.text) || aadhaarNumber.text.Length != 12)
        {
            ShowMessage("Please enter a valid 12-digit Aadhaar number", MessageType.Error);
            return;
        }

        // Show processing message
        ShowMessage("Sending OTP...", MessageType.Processing);
        ToastNotification.Show("Sending OTP...", 3, "success");

        isProcessing = true;

        // Call API
        CallGenerateOtpApi(aadhaarNumber.text);
    }

    public void OnSubmitOtpClicked()
    {
        if (isProcessing)
            return;

        if (string.IsNullOrEmpty(otpInputField.text) || otpInputField.text.Length < 4)
        {
            ShowMessage("Please enter a valid OTP", MessageType.Error);
            ToastNotification.Show("Please enter a valid OTP", 3, "error");

            return;
        }

        if (string.IsNullOrEmpty(phoneNumberField.text) || phoneNumberField.text.Length != 10)
        {
            ShowMessage("Please enter a valid 10-digit phone number", MessageType.Error);
            ToastNotification.Show("Please enter a valid 10-digit phone number", 3, "error");

            return;
        }

        // Show processing message
        ShowMessage("Verifying OTP...", MessageType.Processing);
        ToastNotification.Show("Verifying OTP...", 3, "success");

        isProcessing = true;

        // Call API - we pass empty string for expectedName as it's handled in response now
        StartAadhaarVerification(otpInputField.text, "", phoneNumberField.text);
    }

    public void CallGenerateOtpApi(string aadhaarNumber)
    {
        StartCoroutine(GenerateOtpCoroutine(aadhaarNumber));
    }

    private IEnumerator GenerateOtpCoroutine(string aadhaarNumber)
    {
        string url = "https://kyc-api.surepass.io/api/v1/aadhaar-v2/generate-otp";

        // Create the JSON payload using anonymous type for proper JSON formatting
        string jsonPayload = JsonUtility.ToJson(new RequestPayload { id_number = aadhaarNumber });

        // Create the request
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", authorizationToken);

        // Send the request and wait for the response
        yield return request.SendWebRequest();

        isProcessing = false;

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Parse the JSON response
            Debug.Log("Response: " + request.downloadHandler.text);
            ResponseData responseJson = JsonUtility.FromJson<ResponseData>(request.downloadHandler.text);

            if (responseJson != null && responseJson.success)
            {
                client_id = responseJson.data.client_id;
                Debug.Log("Client ID stored: " + client_id);

                // Show OTP input panel
                verificationPanel.SetActive(true);
                otpInputField.text = "";
                otpInputField.Select();

                // Show instruction message
                ShowMessage("OTP sent! Please enter the OTP and your phone number", MessageType.Success);
            }
            else
            {
                string errorMsg = responseJson != null ? responseJson.message : "Unknown error occurred";
                ShowMessage("API Error: " + errorMsg, MessageType.Error);
                Debug.LogError("API responded with an error: " + errorMsg);
            }
        }
        else
        {
            ShowMessage("API Request Failed: " + request.error, MessageType.Error);
            Debug.LogError("API request failed: " + request.error);
        }
    }

    public void StartAadhaarVerification(string otp, string expectedName, string phoneNumber)
    {
        StartCoroutine(SubmitOtpCoroutine(otp, expectedName, phoneNumber));
    }

    private IEnumerator SubmitOtpCoroutine(string otp, string expectedName, string phoneNumber)
    {
        string url = "https://kyc-api.surepass.io/api/v1/aadhaar-v2/submit-otp";

        // Check if client_id is available
        if (string.IsNullOrEmpty(client_id))
        {
            ShowMessage("No client ID available. Please restart the verification process.", MessageType.Error);
            isProcessing = false;
            yield break;
        }

        // Construct the request body using custom class
        OtpRequestBody requestBody = new OtpRequestBody
        {
            client_id = client_id,
            otp = otp,
            mobile_number = phoneNumber
        };

        string jsonData = JsonUtility.ToJson(requestBody);

        // Create the UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", authorizationToken);

        // Send the request and wait for response
        yield return request.SendWebRequest();

        isProcessing = false;

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Request successful! Response: " + request.downloadHandler.text);

            // Parse the response
            AadhaarResponseWrapper response = JsonUtility.FromJson<AadhaarResponseWrapper>(request.downloadHandler.text);

            if (response != null && response.data != null)
            {
                if (response.data.mobile_verified)
                {
                    ShowMessage("KYC Verification Successful!\nName: " + response.data.full_name, MessageType.Success);
                    Debug.Log("KYC Successful for: " + response.data.full_name);

        ToastNotification.Show("KYC Successful!", 3, "success");
                    // Update Firestore to save KYC verification status
                    UpdateKycStatusInFirestore(response.data.full_name);
                    verificationPanel.SetActive(false);


                    // Update the KYC status text
                    kycStatusText.text = "Verified";
                    kycStatusText.color = successColor;
                }
                else
                {
                    ShowMessage("Mobile number verification failed.", MessageType.Error);
                    Debug.LogWarning("Mobile verification failed for: " + response.data.full_name);
                            ToastNotification.Show("Mobile verification failed!", 3, "error");

                }
            }
            else
            {
                ShowMessage("Failed to parse response data", MessageType.Error);
                Debug.LogError("Failed to parse response.");
            }
        }
        else
        {
            ShowMessage("Request failed: " + request.error, MessageType.Error);
                    ToastNotification.Show("Request failed!", 3, "error");

            Debug.LogError("Request failed: " + request.error);
        }
    }

    // Enum for message types
    private enum MessageType
    {
        Processing,
        Error,
        Success
    }

    private void ShowMessage(string message, MessageType type)
    {
        statusMessageText.text = message;

        // Set color based on message type
        switch (type)
        {
            case MessageType.Processing:
                statusMessageText.color = processingColor;
                break;
            case MessageType.Error:
                statusMessageText.color = errorColor;
                break;
            case MessageType.Success:
                statusMessageText.color = successColor;
                break;
        }

        statusMessageText.gameObject.SetActive(true);
    }

    // Helper classes for serialization/deserialization

    [Serializable]
    private class RequestPayload
    {
        public string id_number;
    }

    [Serializable]
    private class OtpRequestBody
    {
        public string client_id;
        public string otp;
        public string mobile_number;
    }

    [Serializable]
    public class ResponseData
    {
        public ResponseDataDetails data;
        public int status_code;
        public string message_code;
        public string message;
        public bool success;
    }

    [Serializable]
    public class ResponseDataDetails
    {
        public string client_id;
        public bool otp_sent;
        public bool if_number;
        public bool valid_aadhaar;
        public string status;
    }

    [Serializable]
    public class AadhaarResponseWrapper
    {
        public AadhaarResponseData data;
        public bool success;
        public string message;
    }

    [Serializable]
    public class AadhaarResponseData
    {
        public string client_id;
        public string full_name;
        public string aadhaar_number;
        public string dob;
        public string gender;
        public AadhaarAddress address;
        public bool face_status;
        public int face_score;
        public string zip;
        public string profile_image;
        public bool mobile_verified;
    }

    [Serializable]
    public class AadhaarAddress
    {
        public string country;
        public string dist;
        public string state;
        public string po;
        public string loc;
        public string vtc;
        public string subdist;
        public string street;
        public string house;
        public string landmark;
    }
}