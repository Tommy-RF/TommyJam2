// Handles the server-side game state and works with the PlayerNetwork script to manage player readiness.
// Currently also manages UI stuff... Should probably be put into a separate class... Oh well...

using System;
using PurrNet;
using PurrNet.Transports;
using TMPro;
using UnityEngine;

public class GameManager : NetworkBehaviour
{

    public GameObject ExampleObject;
    public GameObject ReadyToPlayObject;

    [SerializeField] private GameObject ReadyArea;
    [SerializeField] private GameObject ReadyText;
    [SerializeField] private GameObject QuestionTimerText;
    [SerializeField] private GameObject TitleText;
    [SerializeField] private GameObject BottomText; // xD
    [SerializeField] private GameObject AnswerMinText;
    [SerializeField] private GameObject AnswerMaxText;
    public GameObject CorrectPlacer;
    public GameObject CorrectPlacerText;
    public GameObject CorrectPlacerImage;
    public GameObject MinArea;
    public GameObject MaxArea;

    public int PlayerReadyCount;
    public int PlayerCount;
    public bool RoundOver;

    public GameObject Ruler;
    public GameObject BackgroundImage;
    
    public int RoundNumber;
    public int TurnNumber;

    // The target answer position is the position on the x axis where the correct answer is located. It is calculated based on the question's answer value and the min and max answer values.
    public int targetAnswerPosition;

    private int[] _PlayerBallPosition = new int[0];

    // The countdown is triggered when all players are ready (in the "Ready" area).
    private const float _COUNTDOWN_TIME = 3f;
    private SyncVar<float> _countdownTimer = new SyncVar<float>(_COUNTDOWN_TIME);

    // The countdown is for each player during their turn. The turn will change when the timer has elapsed.
    [SerializeField] private const float _QUESTIONCOUNTDOWN_TIME = 10f;
    private SyncVar<float> _questionCountdownTimer = new SyncVar<float>(_QUESTIONCOUNTDOWN_TIME);

    // The countdown is for time between rounds.
    [SerializeField] private const float _COOLDOWN_TIME = 5f;
    private SyncVar<float> _cooldownTimer = new SyncVar<float>(_COOLDOWN_TIME);

    // Questions are stored in a ScriptableObject array, which is loaded from the Resources folder. The questions are picked randomly and sent to all clients.
    private QuestionScriptableObject[] _allQuestions = new QuestionScriptableObject[0];
    private SyncVar<int> _currentQuestionIndex = new SyncVar<int>(0);
    private SyncVar<string> _currentQuestionText = new SyncVar<string>("");
    private SyncVar<int> _currentQuestionAnswer = new SyncVar<int>(0);
    private SyncVar<Vector2> _currentQuestionAnswerRange = new SyncVar<Vector2>(new Vector2(0, 0));
    //private SyncTextureAsset _currentQuestionImage = new SyncTextureAsset(true);

    private bool _isQuestionPicked = false;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        _LoadQuestions();
        QuestionTimerText.SetActive(false);
        RoundNumber = 0;

        CorrectPlacerText.SetActive(true);
        CorrectPlacerImage.SetActive(true);

        // This is a lambda expression.
        // Whenever the _currentQuestionIndex SyncVar changes, the UpdateQuestionValues method is called to update the question values on all clients.
        // The _ means that we don't want to pass any parameters to the UpdateQuestionValues method. We just want to call it whenever the _currentQuestionIndex changes.
        // I had to do this because there was an issue with the SyncVar and UpdateQuestionValues() method being called in the wrong order (network race hazard).
        // This way ensures that the question values are updated only after the _currentQuestionIndex changes, and not before.
        _currentQuestionIndex.onChanged += _ => UpdateQuestionValues();
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
        int randomIndex = UnityEngine.Random.Range(0, _allQuestions.Length);
        _currentQuestionIndex.value = randomIndex;
    }


    // Gets the current question values from the _allQuestions array and sets the current question values. This is called on the server when a question is picked.
    [ServerRpc(Channel.Unreliable)]
    public void GetQuestionValues()
    {
        _currentQuestionText.value = _allQuestions[_currentQuestionIndex.value].questionText;
        _currentQuestionAnswer.value = _allQuestions[_currentQuestionIndex.value].answer;
        _currentQuestionAnswerRange.value = _allQuestions[_currentQuestionIndex.value].answerRange;
        //_currentQuestionImage = _allQuestions[_currentQuestionIndex.value].image;

        string answerText = _currentQuestionAnswer.value.ToString();

        ChangeCorrectText(answerText);
    }


    // This updates the question values on all clients. It is called whenever the _currentQuestionIndex SyncVar changes, via a lambda expression subscription.
    [ObserversRpc]
    public void UpdateQuestionValues()
    {

        BottomText.GetComponent<TMPro.TMP_Text>().text = _currentQuestionText.value;
        AnswerMinText.GetComponent<TMPro.TMP_Text>().text = _currentQuestionAnswerRange.value.x.ToString();
        AnswerMaxText.GetComponent<TMPro.TMP_Text>().text = _currentQuestionAnswerRange.value.y.ToString();
        AnswerMinText.SetActive(true);
        AnswerMaxText.SetActive(true);
    }


    [ObserversRpc]
    public void SetQuestionText()
    {
        //BackgroundImage.GetComponent<SpriteRenderer>().sprite = _currentQuestionImage;
        BottomText.GetComponent<TMPro.TMP_Text>().text = _currentQuestionText.value;
        AnswerMinText.GetComponent<TMPro.TMP_Text>().text = _currentQuestionAnswerRange.value.x.ToString();
        AnswerMaxText.GetComponent<TMPro.TMP_Text>().text = _currentQuestionAnswerRange.value.y.ToString();
        _isQuestionPicked = true;
        QuestionTimerText.SetActive(true);

        ClearText();
        ResetCooldown();
        RoundNumber++;
        return;

    }


    public void FixedUpdate()
    {
        if (_isQuestionPicked == false && RoundNumber <= 0)
        {
            UpdateText();
            GetPlayerBallPositions();
        }


        if (RoundNumber >= 1 && RoundOver == false)
        {
            Debug.Log("Question Counting down!");
            SetQuestionCountdownText();
            UpdateQuestionTimer();

            if (_questionCountdownTimer.value <= 0 && RoundOver == false)
            {
                QuestionTimerText.GetComponent<TMP_Text>().SetText("Time Up!");
                if (isServer)
                {
                    CorrectPlacerImage.SetActive(true);
                    CorrectPlacerText.SetActive(true);
                    ChangeCorrectPlacer();
                    ChangeServerCorrectPlacer();
                    _GetTargetPosition();
                    _CalculatePlayerProximityToTarget();
                    _RankClosestPlayers();
                }
                RoundOver = true;
            }

        }

        if (RoundOver == true)
        {
            NextRoundPrep();
        }
    }

    [ServerRpc(Channel.Unreliable)]
    public void NextRoundPrep()
    {
        if (RoundOver == true)
        {

            UpdateCooldownTimer();

            if (_cooldownTimer.value <= 1)
            {
                CorrectPlacer.transform.position = new Vector3(targetAnswerPosition, -3f, 0);
            }

            if (_cooldownTimer.value <= 0 && RoundOver == true)
            {
                _PickRandomQuestion();
                GetQuestionValues();
                SetQuestionText();
                _GetTargetPosition();
                _CalculatePlayerProximityToTarget();
                _RankClosestPlayers();
                ResetQuestionCountdown();
                RoundOver = false;
                
                return;
            }
        }

    }
    [ObserversRpc(Channel.Unreliable)]
    public void RoundOverSetter(bool isRoundOver)
    {
        RoundOver = isRoundOver;
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
        
        // If all players are ready, start the countdown. Otherwise, reset the countdown.
        if(playerReadyCount == PlayerNetwork.allPlayers.Count)
        {
            SetCountdownText();
            
            UpdateTimer();

            if (_countdownTimer.value <= 0 && _isQuestionPicked == false)
            {
                ClearText();
                Ruler.SetActive(true);
                BackgroundImage.SetActive(true);
                _PickRandomQuestion();
                GetQuestionValues();
                if (isServer)
                {
                    SetQuestionText();
                    _GetTargetPosition();
                    _CalculatePlayerProximityToTarget();
                    _RankClosestPlayers();
                }
            }
        }

        else if(playerReadyCount != PlayerNetwork.allPlayers.Count)
        {
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


    [ServerRpc(Channel.Unreliable)]
    public void UpdateQuestionTimer()
    {
        _questionCountdownTimer.value -= (Time.deltaTime);
    }


    [ServerRpc(Channel.Unreliable)]
    public void ResetQuestionCountdown()
    {
        _questionCountdownTimer.value = _QUESTIONCOUNTDOWN_TIME;
    }

    [ServerRpc(Channel.Unreliable)]
    public void UpdateCooldownTimer()
    {
        _cooldownTimer.value -= Time.deltaTime;
    }

    [ServerRpc(Channel.Unreliable)]
    public void ResetCooldown()
    {
        _cooldownTimer.value = _COOLDOWN_TIME;
    }

    [ObserversRpc]
    public void SetCountdownText()
    {
        ReadyText.SetActive(true);
        TitleText.SetActive(true);
        // Checks if the ReadyText is active, and if it is, sets the text to the countdown timer. If it is not active, it does nothing.
        if (ReadyText.activeSelf)
        {
            ReadyText.GetComponent<TMP_Text>().text = $"{Mathf.CeilToInt(_countdownTimer.value)}...";
        }
    }

    [ObserversRpc]
    public void SetQuestionCountdownText()
    {
        QuestionTimerText.SetActive(true);;
        // Checks if the ReadyText is active, and if it is, sets the text to the countdown timer. If it is not active, it does nothing.
        if (QuestionTimerText.activeSelf && RoundOver == false)
        {
            if (_questionCountdownTimer.value <= 0)
            {
                _questionCountdownTimer.value = 0;
            }
            //QuestionTimerText.GetComponent<TMP_Text>().text = $"{Mathf.CeilToInt(_questionCountdownTimer.value)}...";
            QuestionTimerText.GetComponent<TMP_Text>().text = $"{_questionCountdownTimer.value.ToString("F0")}...";
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
        AnswerMinText.SetActive(false);
        AnswerMaxText.SetActive(false);
    }


    // Gets the ball positions of all players and stores them in the _PlayerBallPosition array.
    [ServerRpc(Channel.Unreliable)]
    private void GetPlayerBallPositions()
    {
        var playerIndex = 0;

        _PlayerBallPosition = new int[PlayerNetwork.allPlayers.Count];

        foreach (var player in PlayerNetwork.allPlayers)
            {
                _PlayerBallPosition[playerIndex] = player.Value.ballPosition;

                playerIndex++;
            }
    }


    // Grabs the question's answer and positions it on a scale beetween the min and the max answer position.
    // This position is then used to determine the "target anser position" for the players to aim for with their balls.
    // The target position is set on an x axis between the min and max answer positions, and is calculated based on the question's answer value, linearly interpolated between the min and max answer values.
    [ServerRpc(Channel.Unreliable)]
    private void _GetTargetPosition()
    {
        var minAnswerPosition = MinArea.transform.position.x;
        var maxAnswerPosition = MaxArea.transform.position.x;

        var minAnswerValue = _currentQuestionAnswerRange.value.x;
        var maxAnswerValue = _currentQuestionAnswerRange.value.y;

        var targetPosition = Mathf.Lerp(minAnswerPosition, maxAnswerPosition, (float) (_currentQuestionAnswer.value - minAnswerValue) / (maxAnswerValue - minAnswerValue));
        targetAnswerPosition = Mathf.RoundToInt(targetPosition);

    }

    [ObserversRpc]
    public void ChangeCorrectPlacer()
    {
        CorrectPlacer.transform.position = new Vector3(targetAnswerPosition, -1.8f, 0);
    }

    [ServerRpc]
    public void ChangeServerCorrectPlacer()
    {
        CorrectPlacer.transform.position = new Vector3(targetAnswerPosition, -1.8f, 0);
    }

    public void ChangeCorrectText(string text)
    {
        CorrectPlacerText.GetComponent<TMP_Text>().SetText($"{text}");
    }

    // Calculates the proximity of each player's ball position to the target answer position and stores it in the answerProximity variable of each player.
    [ServerRpc(Channel.Unreliable)]
    private void _CalculatePlayerProximityToTarget()
    {
        foreach (var player in PlayerNetwork.allPlayers)
        {
            var playerBallPosition = player.Value.ballPosition;
            var proximity = Mathf.Abs(playerBallPosition - targetAnswerPosition);
            player.Value.answerProximity = (int) proximity;

            // Debug.Log($"Player {player.Value.id} ball position: {playerBallPosition}, target position: {targetAnswerPosition}, proximity: {proximity}");
        }
    }


    // Rank the players based on their respective ball positions' proximity to the target answer position.
    // The players are ranked from closest first to the farthest last.
    [ServerRpc(Channel.Unreliable)]
    private void _RankClosestPlayers()
    {
        var rankedPlayers = new PlayerNetwork[PlayerNetwork.allPlayers.Count];
        var playerIndex = 0;
        foreach (var player in PlayerNetwork.allPlayers)
        {
            rankedPlayers[playerIndex] = player.Value;
            playerIndex++;
        }
        //Debug.Log(playerIndex);

        // Sort the players by their proximity to the target answer position.
        // Uses a lambda expression with a custom comparison to sort the players based on their answerProximity value.
        Array.Sort(rankedPlayers, (a, b) => a.answerProximity.CompareTo(b.answerProximity));

        Debug.Log("Ranked players:");
        var rank = 1;
        foreach (var player in rankedPlayers)
        {
            Debug.Log($"Player {player.id} proximity: {player.answerProximity}, rank: {rank}.");   
            rank++;
        }
    }


}