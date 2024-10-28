using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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






    void Start()
    {
        error.text = "";
        db = FirebaseFirestore.DefaultInstance;
        questionsList = new List<Question>();
    }

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
                error.text = "Insufficent Balance  \n Required Balance:- 10";
                error.color = Color.red;
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

        // Assume you have a method to get the current user's ID
        string currentUserId = PlayerPrefs.GetString("uid"); // Implement this method according to your auth system

        // Reference to the user's attemptedQuestions subcollection
        CollectionReference attemptedQuestionsRef = db.Collection("users").Document(currentUserId).Collection("attemptedQuestions");

        // Fetch the attempted questions
        QuerySnapshot attemptedQuestionsSnapshot = await attemptedQuestionsRef.GetSnapshotAsync();
        HashSet<string> attemptedUids = new HashSet<string>();

        // Store the attempted question UIDs
        foreach (DocumentSnapshot doc in attemptedQuestionsSnapshot.Documents)
        {
            if (doc.Exists)
            {
                string uid = doc.GetValue<string>("uid");
                attemptedUids.Add(uid);
            }
        }

        // Now fetch the questions by level
        Query questionQuery = db.Collection("question")
            .WhereEqualTo("difficulty", level);

        // If there are attempted UIDs, add the whereNotIn filter
        if (attemptedUids.Count > 0)
        {
            questionQuery = questionQuery.WhereNotIn(FieldPath.DocumentId, new List<string>(attemptedUids));
        }

        questionQuery = questionQuery.Limit(3); // Add limit after conditionally applying the filter

        QuerySnapshot questionSnapshot = await questionQuery.GetSnapshotAsync();

        foreach (DocumentSnapshot document in questionSnapshot.Documents)
        {
            if (document.Exists)
            {
                // Parse document fields
                Question questionData = new Question
                {
                    question = document.GetValue<string>("question"),
                    option1 = document.GetValue<string>("option1"),
                    option2 = document.GetValue<string>("option2"),
                    option3 = document.GetValue<string>("option3"),
                    option4 = document.GetValue<string>("option4"),
                    answer = document.GetValue<string>("answer"),
                    level = document.GetValue<string>("difficulty"),
                    uid = document.Id // Assuming UID is the document ID in Firestore
                };

                questionsList.Add(questionData);
                Debug.Log($"Level: {questionData.level}, Question: {questionData.question}, Answer: {questionData.answer}");
            }
        }

        if (questionsList.Count >= 3)
        {
            Showquestion();
            countdown.startit();
        }
        else
        {
            Debug.Log("Your question for this level is over");
        }
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
               PlayerPrefs.SetFloat("cash", PlayerPrefs.GetFloat("cash") +amount);
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
        for (int i = 10; i >= 0; i--)
        {

            yield return new WaitForSeconds(1);
            timer.text = i.ToString();
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


        index++;
        Showquestion();
        currentQuestion = "";
        selectedoption = "";
        answerimages[0].GetComponent<Image>().sprite = answersprites[0];
        answerimages[1].GetComponent<Image>().sprite = answersprites[0];
        answerimages[2].GetComponent<Image>().sprite = answersprites[0];
        answerimages[3].GetComponent<Image>().sprite = answersprites[0];
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

}
