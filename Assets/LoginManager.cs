using System.Collections;
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

    public Button resendOtpButton;  // Button to resend OTP
    public TMP_Text timerText;          // Text to show countdown timer
    public Text Signuperrortext;  
    public Text loginerrortext;    // Error or success message
    public Text otperrortext;    // Error or success message
    
    private string apiKey = "YOUR_API_KEY";  // Replace with your Fast2SMS API key
    private string senderId = "KHELJN";      // Your sender ID
    private string messageId = "170747";     // Template/message ID
    private string url = "https://www.fast2sms.com/dev/bulkV2";
    private FirebaseFirestore firestore;
    private string generatedOTP; // Store the generated OTP here
    private float countdownTime = 60f; // 60 seconds countdown

    private bool canResend = false; // Track whether OTP can be resent

    void Start()
    {
        firestore = FirebaseFirestore.DefaultInstance;
        resendOtpButton.onClick.AddListener(OnResendOTPClicked);
        resendOtpButton.interactable = false;  
        timerText.text = "";                   
    }

    public void Login()
    {
        string phoneNumber = loginpno.text;

        if (string.IsNullOrEmpty(phoneNumber))
        {
            Signuperrortext.text = "Phone number is required!";
            Signuperrortext.color = Color.red;
            return;
        }

        CheckIfPhoneNumberExists(phoneNumber);
    }

    private void CheckIfPhoneNumberExists(string phoneNumber)
    {
        CollectionReference usersRef = firestore.Collection("users");

        // Query Firestore for existing phone number
        Query query = usersRef.WhereEqualTo("phoneNumber", phoneNumber);

        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                var documents = task.Result;

                if (documents.Count > 0)
                {
                    // Phone number exists, generate and send OTP
                    string otp1 = GenerateOTP();
                    // StartCoroutine(SendOTP(phoneNumber, otp1));
                    Debug.Log(otp1);
                    generatedOTP = otp1; // Store the OTP for later verification
                    otperrortext.text = "OTP sent to your phone!";
                    otperrortext.color = Color.green;
                    login.SetActive(false);
                    otp.SetActive(true);

                    // Start the countdown for Resend OTP
                    StartCoroutine(StartOtpCountdown());
                }
                else
                {
                    loginerrortext.text = "Phone number does not exist!";
                    loginerrortext.color = Color.red;
                }
            }
            else
            {
                loginerrortext.text = "Error checking phone number!";
                loginerrortext.color = Color.red;
            }
        });
    }

    private string GenerateOTP()
    {
        // Generate a random 6-digit OTP
        return Random.Range(100000, 999999).ToString();
    }

    public IEnumerator SendOTP(string phone, string otp)
    {
        // Prepare JSON payload
        string jsonData = JsonUtility.ToJson(new Fast2SMSRequest
        {
            route = "dlt",
            sender_id = senderId,
            message = messageId,
            variables_values = otp,
            flash = 0,
            numbers = phone
        });

        // Create request
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        // Set headers and body
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("authorization", apiKey);

        // Send request and wait for response
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
            Signuperrortext.text = "Failed to send OTP!";
            Signuperrortext.color = Color.red;
        }
        else
        {
            Debug.Log("Response: " + request.downloadHandler.text);
        }
    }

    // Countdown Timer for Resend OTP
    private IEnumerator StartOtpCountdown()
    {
        resendOtpButton.interactable = false;  // Disable Resend OTP button

        float remainingTime = countdownTime;

        while (remainingTime > 0)
        {
            timerText.text = $"Resend OTP in {Mathf.CeilToInt(remainingTime)} seconds";
            yield return new WaitForSeconds(1.0f);
            remainingTime -= 1.0f;
        }

        timerText.text = "";                    // Clear the timer text when finished
        resendOtpButton.interactable = true;    // Enable Resend OTP button
        canResend = true;                       // Allow the user to resend OTP
    }

    // Handle Resend OTP click event
    private void OnResendOTPClicked()
    {
        if (canResend)
        {
            string phoneNumber = loginpno.text;

            if (!string.IsNullOrEmpty(phoneNumber))
            {
                string otp = GenerateOTP();    // Generate new OTP
                StartCoroutine(SendOTP(phoneNumber, otp));
                generatedOTP = otp;            // Update the generated OTP
                otperrortext.text = "OTP resent!";
                otperrortext.color = Color.green;

                // Start the countdown again
                StartCoroutine(StartOtpCountdown());
            }
            else
            {
                otperrortext.text = "Enter your phone number!";
                otperrortext.color = Color.red;
            }
        }
    }

    public void VerifyOTP()
    {
        string enteredOTP = otpvalue.text;

        if (string.IsNullOrEmpty(enteredOTP))
        {
            otperrortext.text = "Please enter the OTP!";
            otperrortext.color = Color.red;
            return;
        }

        if (enteredOTP == generatedOTP)
        {
            otperrortext.text = "OTP verified successfully!";
            otperrortext.color = Color.green;
            // Proceed to the next step (e.g., logging in or navigating to another scene)
            PlayerPrefs.SetString("pno",loginpno.text);
            
            SceneManager.LoadScene(1);
             // Load the next scene
        }
        else
        {
            otperrortext.text = "Incorrect OTP!";
            otperrortext.color = Color.red;
        }
    }

    // Structure for JSON request
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
