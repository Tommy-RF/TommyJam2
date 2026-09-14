// Handles the server-side game state and works with the PlayerNetwork script to manage player readiness.

using PurrNet;
using PurrNet.Transports;
using UnityEngine;

public class GameManager : NetworkBehaviour
{

    public GameObject ExampleObject;
    public GameObject ReadyToPlayObject;
    [SerializeField] private GameObject ReadyArea;
    [SerializeField] private GameObject ReadyText;
    [SerializeField] private GameObject TitleText;
    [SerializeField] private GameObject BottomText; // xD

    public int PlayerReadyCount;
    public int PlayerCount;

    // The countdown is triggered when all players are ready (in the "Ready" area).
    private const float _COUNTDOWN_TIME = 3f;
    private SyncVar<float> _countdownTimer = new SyncVar<float>(_COUNTDOWN_TIME);

    // Questions are stored in a ScriptableObject array, which is loaded from the Resources folder. The questions are picked randomly and sent to all clients.
    private QuestionScriptableObject[] _allQuestions = new QuestionScriptableObject[0];
    private SyncVar<int> _currentQuestionIndex = new SyncVar<int>(0);
    private SyncVar<string> _currentQuestionText = new SyncVar<string>("");
    private SyncVar<int> _currentQuestionAnswer = new SyncVar<int>(0);
    private SyncVar<Vector2> _currentQuestionAnswerRange = new SyncVar<Vector2>(new Vector2(0, 0));

    private bool _isQuestionPicked = false;

    protected override void OnSpawned()
    {
        base.OnSpawned();
            _LoadQuestions();
    }

    public void CheckPlayers()
    {
        foreach(var player in PlayerNetwork.allPlayers)
        {
            Debug.Log($"Player added: {player.Value.id}.");
        }
    }

    // Loads all questions from the Resources folder into the _allQuestions array. This is called on the server when the GameManager is spawned.
    [ServerRpc(Channel.Unreliable)]
    private void _LoadQuestions()
    {
        _allQuestions = Resources.LoadAll<QuestionScriptableObject>("Questions");
    }

    // Picks a random question from the _allQuestions array and sets the current question values.
    [ServerRpc(Channel.Unreliable)]
    private void _PickRandomQuestion()
    {
        int randomIndex = Random.Range(0, _allQuestions.Length);
        _currentQuestionIndex.value = randomIndex;
    }

    // Gets the current question values from the _allQuestions array and sets the current question values. This is called on the server when a question is picked.
    public void GetQuestionValues()
    {
        _currentQuestionText.value = _allQuestions[_currentQuestionIndex.value].questionText;
        _currentQuestionAnswer.value = _allQuestions[_currentQuestionIndex.value].answer;
        _currentQuestionAnswerRange.value = _allQuestions[_currentQuestionIndex.value].answerRange;
        Debug.Log($"Current Question Index: {_currentQuestionIndex.value}");
        Debug.Log($"Current Question Text: {_allQuestions[_currentQuestionIndex.value].questionText}");
        Debug.Log($"Current Question Answer: {_allQuestions[_currentQuestionIndex.value].answer}");
        Debug.Log($"Current Question Answer Range: {_allQuestions[_currentQuestionIndex.value].answerRange}");
    }

    [ObserversRpc]
    public void SetQuestionText()
    {
        BottomText.GetComponent<TMPro.TMP_Text>().text = _currentQuestionText.value;
        _isQuestionPicked = true;
    }

    public void FixedUpdate()
    {
        if (_isQuestionPicked == false)
        {
            UpdateText();
        }
        Debug.Log("Player ready count: " + PlayerReadyCount);
        Debug.Log("Player Count: " + PlayerNetwork.allPlayers.Count);
    }

    [ServerRpc(Channel.Unreliable)]
    public void UpdateText()
    {
         // Check if all players are ready. If they are, start the countdown. Otherwise, reset the countdown.
        var playerReadyCount = 0;
        foreach(var player in PlayerNetwork.allPlayers)
        {
            if(player.Value.isReady)
                {
                    playerReadyCount++;
                }

            else
                {
                 PlayerReadyCount = playerReadyCount;
                }
        }
            
        if(playerReadyCount == PlayerNetwork.allPlayers.Count)
        {
            SetCountdownText();
            //Debug.Log("It's a match!");
            
            UpdateTimer();

            if (_countdownTimer.value <= 0)
            {
                // Debug.Log("Countdown finished!");
                ClearText();
                _PickRandomQuestion();
                GetQuestionValues();
                SetQuestionText();
            }
            //Debug.Log("Countdown: " + _countdownTimer.value);
        }

        else if(playerReadyCount != PlayerNetwork.allPlayers.Count)
        {
            //Debug.Log("Resetting!");
            ResetCountdown();
            ShowText();
        }
    }

    [ServerRpc(Channel.Unreliable)]
    public void UpdateTimer()
    {
        _countdownTimer.value -= Time.deltaTime;
    }

    [ServerRpc(Channel.Unreliable)]
    public void ResetCountdown()
    {
        _countdownTimer.value = _COUNTDOWN_TIME;
    }

    [ObserversRpc]
    public void SetCountdownText()
    {
        ReadyText.SetActive(true);
        TitleText.SetActive(true);
        // Checks if the ReadyText is active, and if it is, sets the text to the countdown timer. If it is not active, it does nothing.
        if (ReadyText.activeSelf)
        {
            ReadyText.GetComponent<TMPro.TMP_Text>().text = $"{Mathf.CeilToInt(_countdownTimer.value)}...";
        }
    }

    [ObserversRpc]
    public void ClearText()
    {
        ReadyArea.SetActive(false);
        ReadyText.SetActive(false);
        TitleText.SetActive(false);
    }
    [ObserversRpc]
    public void ShowText()
    {
        ReadyArea.SetActive(true);
        ReadyText.SetActive(true);
        TitleText.SetActive(true);
    }

    // We will need to use Target RPCs to tell clients that their turn is here or not... I think.
    /*
    [ObserversRpc]
    public void change_state(string state, int TurnPlayer)
    {
        if (state == "hide")
        {
            ExampleObject.SetActive(false);
        }

        if (state == "show")
        {
            ExampleObject.SetActive(true);
        }
    }

    public void CallStateShow()
    {
        change_state("show", 1);
    }

    public void CallStateHide()
    {
        change_state("hide", 1);
    }
    */
}
