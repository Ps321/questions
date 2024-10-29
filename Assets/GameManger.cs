using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Firestore;

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManger : MonoBehaviour
{
    public static string level = "";
    public GameObject questionScreen;
    public GameObject Countdownscreen;

    public Countdown countdown;

    public GameObject difficultyscreen;

    private FirebaseFirestore db;
    public List<Question> questionsList = new List<Question>();

    // Structure to represent the question data
    [System.Serializable]
    public class Question
    {
        public string question;
        public string option1;
        public string option2;
        public string option3;
        public string option4;
        public string answer;
        public string level; // Add level to the Question structure
        public string uid;    // Add UID to store the user ID
    }



    public TMP_Text question;
    public TMP_Text option1;
    public TMP_Text option2;
    public TMP_Text option3;
    public TMP_Text option4;
    public TMP_Text timer;

    public Button fifty50;
    public Button plus5;





    int index = 0;

    public GameObject winscreen;
    public GameObject loosescreen;
    public GameObject nextquesscreen;

    public GameObject[] answerimages;

    public Sprite[] answersprites;
    string currentQuestion = "";

    string selectedoption = "";

    public TMP_Text error;
    int deductamount = 0;
    int winamount = 0;

    private int totalTime = 10; // Total countdown time
    private int additionalTime = 0;
    int remainingTime = 0;

    public GameObject loading;






    void Start()
    {
        loading.SetActive(true);
        error.text = "";
        db = FirebaseFirestore.DefaultInstance;
        questionsList = new List<Question>();

        string phone = PlayerPrefs.GetString("pno");
        if (phone == "")
        {

            SceneManager.LoadScene(0);
            return;
        }

        FetchUserByPhoneNumber(phone);
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
                            PlayerPrefs.SetString("uid",uid);
                            PlayerPrefs.SetString("name",userData["fullName"].ToString());
                            PlayerPrefs.SetString("email",userData["email"].ToString());
                            PlayerPrefs.SetString("username",userData["username"].ToString());
                            

                           
                            float fiatWalletValue = Convert.ToSingle(userData["fiatwallet"]);
                           
                            float cashWalletValue = Convert.ToSingle(userData["cashwallet"]);
                            
                            PlayerPrefs.SetFloat("fiat", fiatWalletValue);
                            PlayerPrefs.SetFloat("cash", cashWalletValue);

                            // Fetch the profile picture URL and load it
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
        finally{
            loading.SetActive(false);
        }
    }

    // Coroutine to load the profile picture from a URL


    // Update is called once per frame
    public void Lobby()
    {
        SceneManager.LoadScene(1);
    }


    public void SelectLevel(string level1)
    {
        level = level1;

        float fiat = PlayerPrefs.GetFloat("fiat");
        float cash = PlayerPrefs.GetFloat("cash");
        Debug.Log(fiat + "-" + cash);
        if (level == "Easy")
        {
            if (fiat < 10 && cash < 10)
            {
                ToastNotification.Show("Insufficent Balance", 3, "error");
                return;
            }
            winamount = 30;
            deductamount = 10;

        }
        if (level == "Medium")
        {
            if (fiat < 50 && cash < 50)
            {
                error.text = "Insufficent Balance  \n Required Balance:- 50";
                error.color = Color.red;
                return;
            }
            winamount = 80;
            deductamount = 50;
        }
        if (level == "Hard")
        {
            if (fiat < 100 && cash < 100)
            {
                error.text = "Insufficent Balance \n Required Balance:- 100";
                error.color = Color.red;
                return;
            }

            winamount = 170;
            deductamount = 100;
        }
        difficultyscreen.SetActive(false);
        Countdownscreen.SetActive(true);
        FetchQuestionsByLevel(level);
    }

    private async void FetchQuestionsByLevel(string level)
    {
        Debug.Log("Fetching questions...");

        string currentUserId = PlayerPrefs.GetString("uid");
        CollectionReference attemptedQuestionsRef = db.Collection("users").Document(currentUserId).Collection("attemptedQuestions");

        // Fetch attempted question UIDs
        QuerySnapshot attemptedQuestionsSnapshot = await attemptedQuestionsRef.GetSnapshotAsync();
        HashSet<string> attemptedUids = new HashSet<string>();

        foreach (DocumentSnapshot doc in attemptedQuestionsSnapshot.Documents)
        {
            if (doc.Exists)
            {
                string uid = doc.GetValue<string>("uid");
                attemptedUids.Add(uid);
            }
        }

        // Pagination setup
        int batchSize = 10;
        bool hasMoreQuestions = true;
        List<Question> tempQuestionsList = new List<Question>();

        // Loop through pages until we have the required questions
        while (tempQuestionsList.Count < 3 && hasMoreQuestions)
        {
            // Fetch a random batch of questions by level
            Query questionQuery = db.Collection("question")
                .WhereEqualTo("difficulty", level)
                .OrderBy(FieldPath.DocumentId)
                .Limit(batchSize);

            // Randomly set a start point for pagination
            string randomStartId = GenerateRandomDocumentId(); // Function to generate a random document ID
            questionQuery = questionQuery.StartAt(randomStartId);

            QuerySnapshot questionSnapshot = await questionQuery.GetSnapshotAsync();
            if (questionSnapshot.Documents.Count() == 0)
            {
                hasMoreQuestions = false;
                break;
            }

            // Filter out already attempted questions
            foreach (DocumentSnapshot document in questionSnapshot.Documents)
            {
                if (document.Exists && !attemptedUids.Contains(document.Id))
                {
                    Question questionData = new Question
                    {
                        question = document.GetValue<string>("question"),
                        option1 = document.GetValue<string>("option1"),
                        option2 = document.GetValue<string>("option2"),
                        option3 = document.GetValue<string>("option3"),
                        option4 = document.GetValue<string>("option4"),
                        answer = document.GetValue<string>("answer"),
                        level = document.GetValue<string>("difficulty"),
                        uid = document.Id
                    };

                    tempQuestionsList.Add(questionData);
                    Debug.Log($"Level: {questionData.level}, Question: {questionData.question}, Answer: {questionData.answer}");

                    if (tempQuestionsList.Count >= 3) break; // Stop if 3 questions have been found
                }
            }
        }

        if (tempQuestionsList.Count >= 3)
        {
            questionsList.AddRange(tempQuestionsList.Take(3));
            Showquestion();
            countdown.startit();
        }
        else
        {
            Debug.Log("Your question for this level is over");
        }
    }

    // Helper function to generate a random Document ID for pagination starting point
    private string GenerateRandomDocumentId()
    {
        // Customize this function to generate a random Document ID within your expected range
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        return new string(Enumerable.Repeat(chars, 20).Select(s => s[random.Next(s.Length)]).ToArray());
    }
    public void Showquestion()
    {
        question.text = questionsList[index].question;
        option1.text = questionsList[index].option1;
        option2.text = questionsList[index].option2;
        option3.text = questionsList[index].option3;
        option4.text = questionsList[index].option4;

        Debug.Log("Answer is " + questionsList[index].answer);

        StoreAttemptedQuestion(questionsList[index].uid);
        DeductMoneyFromWallet(PlayerPrefs.GetString("uid"), deductamount);
    }
    private void DeductMoneyFromWallet(string userId, float amount)
    {
        DocumentReference userDocRef = db.Collection("users").Document(userId);



        if (PlayerPrefs.GetFloat("cash") > amount)
        {
            userDocRef.UpdateAsync("cashwallet", FieldValue.Increment(-amount)).ContinueWith(task =>
           {
               if (task.IsFaulted)
               {
                   Debug.LogError("Deduction failed: " + task.Exception);
               }
               else
               {
                   PlayerPrefs.SetFloat("cash", PlayerPrefs.GetFloat("cash") - amount);
                   Debug.Log($"Deducted {amount} from user's wallet.");
               }
           });
        }
        else
        {
            userDocRef.UpdateAsync("fiatwallet", FieldValue.Increment(-amount)).ContinueWith(task =>
           {
               if (task.IsFaulted)
               {
                   Debug.LogError("Deduction failed: " + task.Exception);
               }
               else
               {
                   PlayerPrefs.SetFloat("fiat", PlayerPrefs.GetFloat("fiat") - amount);
                   Debug.Log($"Deducted {amount} from user's wallet.");
               }
           });
        }

        // Use FieldValue to subtract from the balance

    }
    private void AddMoneyFromWallet(string userId, float amount)
    {
        DocumentReference userDocRef = db.Collection("users").Document(userId);




        userDocRef.UpdateAsync("cashwallet", FieldValue.Increment(amount)).ContinueWith(task =>
       {
           if (task.IsFaulted)
           {
               Debug.LogError("Deduction failed: " + task.Exception);
           }
           else
           {
               Debug.Log("aaya");
               PlayerPrefs.SetFloat("cash", PlayerPrefs.GetFloat("cash") + amount);
               Debug.Log($"Added {amount} from user's wallet.");
           }
       });

        // Use FieldValue to subtract from the balance

    }




    private async void StoreAttemptedQuestion(string questionUid)
    {
        // Assume you have a method to get the current user's ID
        string currentUserId = PlayerPrefs.GetString("uid"); // Implement this method according to your auth system

        // Reference to the user's document in the user collection
        DocumentReference userDocRef = db.Collection("users").Document(currentUserId);

        // Reference to the attemptedQuestions subcollection
        CollectionReference attemptedQuestionsRef = userDocRef.Collection("attemptedQuestions");

        // Create a new document in the attemptedQuestions subcollection
        DocumentReference newQuestionRef = attemptedQuestionsRef.Document(questionUid);

        // Prepare the data to store
        var questionData = new
        {
            uid = questionUid,
            difficulty = questionsList[index].level,
            question = questionsList[index].question,
            timestamp = Timestamp.GetCurrentTimestamp() // Optional: to store when the question was attempted
        };

        try
        {
            // Set the data in Firestore
            await newQuestionRef.SetAsync(questionData);
            Debug.Log($"Successfully stored attempted question: {questionUid} with difficulty: ");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error storing attempted question: {e.Message}");
        }
    }


    public void StartGame()
    {
        StartCoroutine(Starttimer());
        questionScreen.SetActive(true);
    }
   

    IEnumerator Starttimer()
    {
        remainingTime = totalTime + additionalTime; // Calculate remaining time including additional time

        while (remainingTime > 0) // Use a while loop to allow for dynamic time extension
        {
            timer.text = remainingTime.ToString(); // Display the remaining time
            yield return new WaitForSeconds(1); // Wait for 1 second
            remainingTime--; // Decrease the remaining time
        }




        if (questionsList[index].answer.Substring(3).Equals(currentQuestion, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("Correct answer!");



            if (selectedoption == "option1")
            {
                answerimages[0].GetComponent<Image>().sprite = answersprites[2];


            }
            if (selectedoption == "option2")
            {

                answerimages[1].GetComponent<Image>().sprite = answersprites[2];

            }
            if (selectedoption == "option3")
            {

                answerimages[2].GetComponent<Image>().sprite = answersprites[2];

            }
            if (selectedoption == "option4")
            {

                answerimages[3].GetComponent<Image>().sprite = answersprites[3];

            }

            yield return new WaitForSeconds(2.0f);
            if (index + 1 < 3)
            {
                nextquesscreen.SetActive(true);
            }
            else
            {
                winscreen.SetActive(true);
                AddMoneyFromWallet(PlayerPrefs.GetString("uid"), winamount);

            }


        }
        else
        {
            Debug.Log("Incorrect answer. The correct answer is: " + currentQuestion + "-" + questionsList[index].answer.Substring(3));

            if (selectedoption == "option1")
            {
                answerimages[0].GetComponent<Image>().sprite = answersprites[3];


            }
            if (selectedoption == "option2")
            {

                answerimages[1].GetComponent<Image>().sprite = answersprites[3];

            }
            if (selectedoption == "option3")
            {

                answerimages[2].GetComponent<Image>().sprite = answersprites[3];

            }
            if (selectedoption == "option4")
            {

                answerimages[3].GetComponent<Image>().sprite = answersprites[3];

            }

            yield return new WaitForSeconds(2.0f);

            loosescreen.SetActive(true);
        }



    }


    public void shownextquesscreen()
    {

        additionalTime=0;
        index++;
        Showquestion();
        currentQuestion = "";
        selectedoption = "";
        answerimages[0].GetComponent<Image>().sprite = answersprites[0];
        answerimages[1].GetComponent<Image>().sprite = answersprites[0];
        answerimages[2].GetComponent<Image>().sprite = answersprites[0];
        answerimages[3].GetComponent<Image>().sprite = answersprites[0];
        plus5.interactable=true;
        fifty50.interactable=true;
        option1.gameObject.SetActive(true);
        option2.gameObject.SetActive(true);
        option3.gameObject.SetActive(true);
        option4.gameObject.SetActive(true);
        nextquesscreen.SetActive(false);
        StartCoroutine(Starttimer());

    }


    public void HandleAnswer(String option)
    {


        if (option == "option1")
        {
            answerimages[0].GetComponent<Image>().sprite = answersprites[1];
            answerimages[1].GetComponent<Image>().sprite = answersprites[0];
            answerimages[2].GetComponent<Image>().sprite = answersprites[0];
            answerimages[3].GetComponent<Image>().sprite = answersprites[0];
            currentQuestion = questionsList[index].option1;
            selectedoption = option;
        }
        if (option == "option2")
        {
            selectedoption = option;
            answerimages[0].GetComponent<Image>().sprite = answersprites[0];
            answerimages[1].GetComponent<Image>().sprite = answersprites[1];
            answerimages[2].GetComponent<Image>().sprite = answersprites[0];
            answerimages[3].GetComponent<Image>().sprite = answersprites[0];
            currentQuestion = questionsList[index].option2;
        }
        if (option == "option3")
        {
            selectedoption = option;
            answerimages[0].GetComponent<Image>().sprite = answersprites[0];
            answerimages[1].GetComponent<Image>().sprite = answersprites[0];
            answerimages[2].GetComponent<Image>().sprite = answersprites[1];
            answerimages[3].GetComponent<Image>().sprite = answersprites[0];
            currentQuestion = questionsList[index].option3;
        }
        if (option == "option4")
        {
            selectedoption = option;
            answerimages[0].GetComponent<Image>().sprite = answersprites[0];
            answerimages[1].GetComponent<Image>().sprite = answersprites[0];
            answerimages[2].GetComponent<Image>().sprite = answersprites[0];
            answerimages[3].GetComponent<Image>().sprite = answersprites[1];
            currentQuestion = questionsList[index].option4;
        }




    }


    public void ShowTwoOptions()
    {
       
        fifty50.interactable=false;
        DeductMoneyFromWallet(PlayerPrefs.GetString("uid"), 20);
        // Get the correct answer and its index
        string correctAnswer = questionsList[index].answer.Substring(3);
        int correctIndex = -1;

        // Determine the index of the correct answer
        if (questionsList[index].option1 == correctAnswer) correctIndex = 0;
        else if (questionsList[index].option2 == correctAnswer) correctIndex = 1;
        else if (questionsList[index].option3 == correctAnswer) correctIndex = 2;
        else if (questionsList[index].option4 == correctAnswer) correctIndex = 3;

        // Create a list of incorrect options
        List<string> incorrectOptions = new List<string>();

        if (correctIndex != 0) incorrectOptions.Add(questionsList[index].option1);
        if (correctIndex != 1) incorrectOptions.Add(questionsList[index].option2);
        if (correctIndex != 2) incorrectOptions.Add(questionsList[index].option3);
        if (correctIndex != 3) incorrectOptions.Add(questionsList[index].option4);

        // Randomly select one incorrect option from the remaining options
        string randomIncorrectOption = incorrectOptions[UnityEngine.Random.Range(0, incorrectOptions.Count)];

        // Hide all options initially
        option1.gameObject.SetActive(false);
        option2.gameObject.SetActive(false);
        option3.gameObject.SetActive(false);
        option4.gameObject.SetActive(false);

        // Show the correct option in its original position
        if (correctIndex == 0)
        {
            option1.text = correctAnswer;
            option1.gameObject.SetActive(true); // Show option1
        }
        else if (correctIndex == 1)
        {
            option2.text = correctAnswer;
            option2.gameObject.SetActive(true); // Show option2
        }
        else if (correctIndex == 2)
        {
            option3.text = correctAnswer;
            option3.gameObject.SetActive(true); // Show option3
        }
        else if (correctIndex == 3)
        {
            option4.text = correctAnswer;
            option4.gameObject.SetActive(true); // Show option4
        }

        // Show the random incorrect option in its original position
        if (questionsList[index].option1 == randomIncorrectOption)
        {
            option1.text = randomIncorrectOption;
            option1.gameObject.SetActive(true); // Show option1
        }
        else if (questionsList[index].option2 == randomIncorrectOption)
        {
            option2.text = randomIncorrectOption;
            option2.gameObject.SetActive(true); // Show option2
        }
        else if (questionsList[index].option3 == randomIncorrectOption)
        {
            option3.text = randomIncorrectOption;
            option3.gameObject.SetActive(true); // Show option3
        }
        else if (questionsList[index].option4 == randomIncorrectOption)
        {
            option4.text = randomIncorrectOption;
            option4.gameObject.SetActive(true); // Show option4
        }

        Debug.Log("Showing options: Correct answer at index " + correctIndex + " and incorrect answer: " + randomIncorrectOption);
    }



     public void OnAddTimeButtonClicked()
    {

        plus5.interactable=false;
        DeductMoneyFromWallet(PlayerPrefs.GetString("uid"), 10);
        additionalTime += 5;
        Debug.Log($"Added 5 seconds! Total additional time: {additionalTime} seconds.");

        UpdateTimerDisplay();

        // Restart the timer coroutine if it's already running

        StartCoroutine("StartTimer");
    }

    private void UpdateTimerDisplay()
    {
        StopCoroutine("StartTimer");
        // Calculate remaining time including additional time
        remainingTime = remainingTime + additionalTime;
        timer.text = remainingTime.ToString(); // Update UI text
    }



}
