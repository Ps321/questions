using System;
using System.Collections;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField loginpno;
    public TMP_InputField otpvalue;

    public GameObject login;
    public GameObject otp;
    public GameObject signup;

    public Button resendOtpButton;
    public TMP_Text timerText;
    public Text Signuperrortext;
    public Text loginerrortext;
    public Text otperrortext;

    private string apiKey = "ZAPHPYGTJR1Fj8GxmmFfngNKkBgY9VH5w9IPe3KGHofLjrEW1uoQSiBn8jVC";
    private string senderId = "KHELJN";
    private string messageId = "170747";
    private string url = "https://www.fast2sms.com/dev/bulkV2";
    private FirebaseFirestore firestore;
    private string generatedOTP;
    private float countdownTime = 60f;

    private bool canResend = false;

    public TMP_InputField emailInput;
    public TMP_InputField fullNameInput;
    public TMP_InputField usernameInput;
    public TMP_InputField phoneNumberInput;

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

        resendOtpButton.onClick.AddListener(OnResendOTPClicked);
        resendOtpButton.interactable = false;
        timerText.text = "";
    }


    public void Login()
    {
        string phoneNumber = loginpno.text;

        if (string.IsNullOrEmpty(phoneNumber))
        {
            ToastNotification.Show("Phone number is required!", 3.0f, "error");
            return;
        }

        CheckIfPhoneNumberExists(phoneNumber);
    }

    private void CheckIfPhoneNumberExists(string phoneNumber)
    {
        CollectionReference usersRef = firestore.Collection("users");

        Query query = usersRef.WhereEqualTo("phoneNumber", phoneNumber);

        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                var documents = task.Result;

                if (documents.Count > 0)
                {
                    string otp1 = GenerateOTP();
                    StartCoroutine(SendOTP(phoneNumber, otp1));
                    generatedOTP = otp1;
                    ToastNotification.Show("OTP sent to your phone!", 3.0f, "success");
                    login.SetActive(false);
                    otp.SetActive(true);

                    StartCoroutine(StartOtpCountdown());
                }
                else
                {
                    ToastNotification.Show("Phone number does not exist!", 3.0f, "error");
                }
            }
            else
            {
                ToastNotification.Show("Error checking phone number!", 3.0f, "error");
            }
        });
    }

    private string GenerateOTP()
    {
        return UnityEngine.Random.Range(100000, 999999).ToString();
    }

    public IEnumerator SendOTP(string phone, string otp)
    {
        string jsonData = JsonUtility.ToJson(new Fast2SMSRequest
        {
            route = "dlt",
            sender_id = senderId,
            message = messageId,
            variables_values = otp,
            flash = 0,
            numbers = phone
        });

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("authorization", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
            ToastNotification.Show("Failed to send OTP!", 3.0f, "error");
        }
        else
        {
            Debug.Log("Response: " + request.downloadHandler.text);
        }
    }

    private IEnumerator StartOtpCountdown()
    {
        resendOtpButton.interactable = false;

        float remainingTime = countdownTime;

        while (remainingTime > 0)
        {
            timerText.text = $"Resend OTP in {Mathf.CeilToInt(remainingTime)} seconds";
            yield return new WaitForSeconds(1.0f);
            remainingTime -= 1.0f;
        }

        timerText.text = "";
        resendOtpButton.interactable = true;
        canResend = true;
    }

    public void Google()
    {
        ToastNotification.Show("Api Keys Expired", 3, "error");
    }

    private void OnResendOTPClicked()
    {
        if (canResend)
        {
            string phoneNumber = loginpno.text;

            if (!string.IsNullOrEmpty(phoneNumber))
            {
                string otp = GenerateOTP();
                StartCoroutine(SendOTP(phoneNumber, otp));
                generatedOTP = otp;
                ToastNotification.Show("OTP resent!", 3.0f, "success");

                StartCoroutine(StartOtpCountdown());
            }
            else
            {
                ToastNotification.Show("Enter your phone number!", 3.0f, "error");
            }
        }
    }

    public void VerifyOTP()
    {
        string enteredOTP = otpvalue.text;

        if (string.IsNullOrEmpty(enteredOTP))
        {
            ToastNotification.Show("Please enter the OTP!", 3.0f, "error");
            return;
        }

        if (enteredOTP == generatedOTP)
        {
            ToastNotification.Show("OTP verified successfully!", 3.0f, "success");
            PlayerPrefs.SetString("pno", loginpno.text);
            StartCoroutine(nextloadscreen());
        }
        else
        {
            ToastNotification.Show("Incorrect OTP!", 3.0f, "error");
        }
    }

    IEnumerator nextloadscreen()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(1);

    }

    public void OnSignupButtonClicked()
    {
        string email = emailInput.text;
        string fullName = fullNameInput.text;
        string username = usernameInput.text;
        string phoneNumber = phoneNumberInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(phoneNumber))
        {
            ToastNotification.Show("All fields are required!", 3.0f, "error");
            return;
        }

        CheckIfUserExists(email, username, phoneNumber, fullName);
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
                                SaveUserToFirestore(email, fullName, username, phoneNumber);
                            }
                        });
                    }
                });
            }
        });
    }


    private void SaveUserToFirestore(string email, string fullName, string username, string phoneNumber)
    {
        CollectionReference usersCollectionRef = firestore.Collection("users");

        var userData = new
        {
            email = email,
            fullName = fullName,
            username = username,
            phoneNumber = phoneNumber,
            cashwallet = 0.0,
            fiatwallet = 10.0
        };

        usersCollectionRef.AddAsync(userData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                ToastNotification.Show("User signed up successfully!", 3.0f, "success");
            }
            else
            {
                ToastNotification.Show("Error saving user data!", 3.0f, "error");
            }
        });
    }

    [System.Serializable]
    public class Fast2SMSRequest
    {
        public string route;
        public string sender_id;
        public string message;
        public string variables_values;
        public int flash;
        public string numbers;
    }
}
