// Handles the server-side game state and works with the PlayerNetwork script to manage player readiness.
// Currently also manages UI stuff... Should probably be put into a separate class... Oh well...

using System;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Transports;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class GameManager : NetworkBehaviour
{

    public GameObject ReadyToPlayObject;

    [SerializeField] private GameObject ScoreBoardUI;
    [SerializeField] private GameObject FirstPlaceUI;
    [SerializeField] private GameObject SecondPlaceUI;
    [SerializeField] private GameObject ThirdPlaceUI;
    [SerializeField] private GameObject FourthPlaceUI;

    [SerializeField] private GameObject IntroUI;
    [SerializeField] private GameObject ReadyArea;
    [SerializeField] private GameObject ReadyText;
    [SerializeField] private GameObject QuestionTimerText;
    [SerializeField] private GameObject TitleText;
    [SerializeField] private GameObject BottomText; // xD
    [SerializeField] private GameObject AnswerMinText;
    [SerializeField] private GameObject AnswerMaxText;

    [SerializeField] private GameObject P1;
    [SerializeField] private GameObject P2;
    [SerializeField] private GameObject P3;
    [SerializeField] private GameObject P4;

    [SerializeField] private GameObject WinningPlayer;

    public int Player1;
    public int Player2;
    public int Player3;
    public int Player4;


    public GameObject CorrectPlacer;
    public GameObject CorrectPlacerText;
    public GameObject CorrectPlacerImageBlue;
    public GameObject CorrectPlacerImageRed;
    public GameObject CorrectPlacerImageGreen;
    public GameObject CorrectPlacerImageYellow;
    public GameObject MinArea;
    public GameObject MaxArea;

    public AudioSource gameSFX;

    public int PlayerReadyCount;
    public int PlayerCount;
    public bool RoundOver;
    public int maxRounds = 2;
    public bool finishedRanking = false;

    public bool isScoreboardSetUp = false;

    public GameObject Ruler;
    public GameObject BackgroundImage;
    
    private SyncVar<int> _roundNumber = new SyncVar<int>(0);
    public int TurnNumber;

    // The target answer position is the position on the x axis where the correct answer is located. It is calculated based on the question's answer value and the min and max answer values.
    [SerializeField] private SyncVar<int> targetAnswerPosition = new SyncVar<int>(0);
    [SerializeField] private SyncVar<int> awayPosition = new SyncVar<int>(-5);

    private int[] _PlayerBallPosition = new int[0];

    // The countdown is triggered when all players are ready (in the "Ready" area).
    private const float _COUNTDOWN_TIME = 3f;
    private SyncVar<float> _countdownTimer = new SyncVar<float>(_COUNTDOWN_TIME);

    // The countdown is for each player during their turn. The turn will change when the timer has elapsed.
    [SerializeField] private const float _QUESTIONCOUNTDOWN_TIME = 15f;
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
        _roundNumber.value = 0;

        CorrectPlacerText.SetActive(true);
        //CorrectPlacerImage.SetActive(true);

        // This is a lambda expression.
        // Whenever the _currentQuestionIndex SyncVar changes, the UpdateQuestionValues method is called to update the question values on all clients.
        // The _ means that we don't want to pass any parameters to the UpdateQuestionValues method. We just want to call it whenever the _currentQuestionIndex changes.
        // I had to do this because there was an issue with the SyncVar and UpdateQuestionValues() method being called in the wrong order (network race hazard).
        // This way ensures that the question values are updated only after the _currentQuestionIndex changes, and not before.
        _currentQuestionIndex.onChanged += _ => UpdateQuestionValues();
    }

    protected override void OnDespawned()
    {
        base.OnDespawned();
        _currentQuestionIndex.onChanged -= _ => UpdateQuestionValues();
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
        ClearText();
        QuestionTimerText.SetActive(true);
        ResetCooldown();

    }


    public void FixedUpdate()
    {
        if (_isQuestionPicked == false && _roundNumber.value <= 0)
        {
            UpdateText();
            GetPlayerBallPositions();
            ;
        }


        if (_roundNumber.value >= 1 && RoundOver == false)
        {
            //Debug.Log("Question Counting down!");
            Debug.Log("Round Number: " + _roundNumber.value);
            Debug.Log("Max Rounds: " + maxRounds);
            if (_roundNumber.value >= maxRounds)
            {
                Debug.Log("Max rounds reached!");
            }
            SetQuestionCountdownText();
            UpdateQuestionTimer();

            if (_questionCountdownTimer.value <= 0)
            {
                QuestionTimerText.GetComponent<TMP_Text>().SetText("Time Up!");
                //CorrectPlacerImage.SetActive(true);
                CorrectPlacerText.SetActive(true);
                if (isServer)
                {
                    ChangeServerCorrectPlacer(targetAnswerPosition.value, -1.8f, 0);
                    _GetTargetPosition();
                    _CalculatePlayerProximityToTarget();
                    if (!finishedRanking)
                    {
                        _RankClosestPlayers();
                    }
                    finishedRanking = true;
                    RoundOver = true;
                }
            }

        }

        if (RoundOver == true && _roundNumber.value < maxRounds)
        {
            Debug.Log("Round Over! Preparing for next round...");
            NextRoundPrep();
        }

        if (isServer &&RoundOver == true && _roundNumber.value >= maxRounds && isScoreboardSetUp == false)
        {
            Debug.Log("Game Over! Prepping scoreboard...");
            //ResetPlayerScores();
            UpdateCooldownTimer();

            if (_cooldownTimer.value <= 0)
            SetUpScoreboard();
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
                ChangeServerCorrectPlacer(targetAnswerPosition.value, awayPosition.value, 0);
                //CorrectPlacer.transform.position = new Vector3(targetAnswerPosition, -3f, 0);
            }

            if (_cooldownTimer.value <= 0 && RoundOver == true)
            {
                if (isServer)
                {
                    _roundNumber.value++;
                }

                _PickRandomQuestion();
                GetQuestionValues();
                SetQuestionText();
                _GetTargetPosition();
                _CalculatePlayerProximityToTarget();
                ResetQuestionCountdown();
                RoundOver = false;
                finishedRanking = false;
                return;
            }
        }

    }

    [ObserversRpc]
    public void scoreboardFinished(bool set)
    {
        isScoreboardSetUp = set;
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
                GetPlayerIDs();
                //Ruler.SetActive(true);
                //BackgroundImage.SetActive(true);
                _PickRandomQuestion();
                GetQuestionValues();
                if (isServer)
                {
                    if (_roundNumber.value == 0)
                    {
                        if (isServer)
                        {
                            _roundNumber.value = 1;
                        }
                        
                        SetQuestionText();
                        _GetTargetPosition();
                        _CalculatePlayerProximityToTarget();
                        return;
                    }
                    else if (_roundNumber.value >= 1)
                    {
                        if (isServer)
                        {
                            _roundNumber.value++;
                            maxRounds++;
                        }
                        SetQuestionText();
                        _GetTargetPosition();
                        _CalculatePlayerProximityToTarget();
                    }

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
        _questionCountdownTimer.value -= (Time.deltaTime/2);
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
            if (_questionCountdownTimer.value <= 0 && isServer)
            {
                _questionCountdownTimer.value = 0;
            }
            else if (_questionCountdownTimer.value >= 0.1f)
            {
                // QuestionTimerText.GetComponent<TMP_Text>().text = $"{Mathf.CeilToInt(_questionCountdownTimer.value)}...";
                QuestionTimerText.GetComponent<TMP_Text>().text = $"{_questionCountdownTimer.value.ToString("F0")}...";
            }
                
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
        targetAnswerPosition.value = Mathf.RoundToInt(targetPosition);
    
    }

    //[ObserversRpc]
    //public void ChangeCorrectPlacer(float x, float y, float z)
    //{
    //    CorrectPlacer.transform.position = new Vector3(x, y, z);
    //    ChangeServerCorrectPlacer(x, y, z);
    //}

    [ServerRpc]
    public void ChangeServerCorrectPlacer(float x, float y, float z)
    {
        CorrectPlacer.transform.position = new Vector3(x, y, z);
        //ChangeCorrectPlacer(x, y, z);

    }

    public void ChangeCorrectText(string text)
    {
        CorrectPlacerText.GetComponent<TMP_Text>().SetText($"{text}");
    
    }

    [ObserversRpc(Channel.Unreliable)]
    private void GetPlayerIDs()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            int playerId = player.GetComponent<PlayerPhysics>().PlayerNumber;

            if (playerId == 1)
            {
                P1 = player;
                P1.name = "P1 (Blue)";
            }
            if (playerId == 2)
            {
                P2 = player;
                P2.name = "P2 (Red)";
            }
            if (playerId == 3)
            {
                P3 = player;
                P3.name = "P3 (Green)";
            }
            if (playerId == 4)
            {
                P4 = player;
                P4.name = "P4 (Yellow)";
            }
        }

    }

    // Calculates the proximity of each player's ball position to the target answer position and stores it in the answerProximity variable of each player.
    [ServerRpc(Channel.Unreliable)]
    private void _CalculatePlayerProximityToTarget()
    {
        foreach (var player in PlayerNetwork.allPlayers)
        {
            var playerBallPosition = player.Value.ballPosition;
            var proximity = Mathf.Abs(playerBallPosition - targetAnswerPosition.value);
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

        rankedPlayers[0].score.value++;
        WinningPlayer = rankedPlayers[0].gameObject;
        if (WinningPlayer.GetComponent<PlayerPhysics>().PlayerNumber == 1)
        {
            CorrectImageSetter(1);
        }
        if (WinningPlayer.GetComponent<PlayerPhysics>().PlayerNumber == 2)
        {
            CorrectImageSetter(2);
        }
        if (WinningPlayer.GetComponent<PlayerPhysics>().PlayerNumber == 3)
        {
            CorrectImageSetter(3);
        }
        if (WinningPlayer.GetComponent<PlayerPhysics>().PlayerNumber == 4)
        {
            CorrectImageSetter(4);
        }
        Debug.Log("Player " + rankedPlayers[0].id + " scored a point! Total score: " + rankedPlayers[0].score.value);

        


    }

    [ObserversRpc]
    public void CorrectImageSetter(int colour)
    {
        CorrectPlacerImageBlue.SetActive(false);
        CorrectPlacerImageRed.SetActive(false);
        CorrectPlacerImageGreen.SetActive(false);
        CorrectPlacerImageYellow.SetActive(false);

        if (colour == 1)
        {
            CorrectPlacerImageBlue.SetActive(true);
        }
        
        if (colour == 2)
        {
            CorrectPlacerImageRed.SetActive(true);
        }
        if (colour == 3)
        {
            CorrectPlacerImageGreen.SetActive(true);
        }
        if (colour == 4)
        {
            CorrectPlacerImageYellow.SetActive(true);
        }
    }

    [ObserversRpc]
    public void SetUpScoreboard()
    {
        if (isScoreboardSetUp == false)
        {
            var rankedPlayers = new List<PlayerNetwork>(PlayerNetwork.allPlayers.Values);

            rankedPlayers.Sort((first, second) =>
                second.score.value.CompareTo(first.score.value));

            if (rankedPlayers.Count == 0)
            {
                Debug.LogWarning("Cannot populate scoreboard: no players found.");
                return;
            }

            SetScoreboardText(FirstPlaceUI, "1st", rankedPlayers[0]);

            if (rankedPlayers.Count > 1)
                SetScoreboardText(SecondPlaceUI, "2nd", rankedPlayers[1]);

            if (rankedPlayers.Count > 2)
                SetScoreboardText(ThirdPlaceUI, "3rd", rankedPlayers[2]);

            if (rankedPlayers.Count > 3)
                SetScoreboardText(FourthPlaceUI, "4th", rankedPlayers[3]);

            IntroUI.SetActive(false);
            //CorrectPlacerImage.SetActive(false);
            //CorrectPlacerText.SetActive(false);
            CorrectPlacer.SetActive(false);
            //ChangeServerCorrectPlacer(0, -5, 0);
            
            ScoreBoardUI.SetActive(true);
            //isScoreboardSetUp = true;

            gameSFX.Play();
            scoreboardFinished(true);

        }

    }

    private void SetScoreboardText(
        GameObject scoreboardEntry,
        string rank,
        PlayerNetwork player)
    {
        var text = scoreboardEntry.GetComponent<TMP_Text>();

        text.SetText($"{rank}: {player.name} - {player.score.value}");
    }

    [ServerRpc(Channel.Unreliable)]
    public void ResetPlayerScores()
    {
        if (_roundNumber.value == maxRounds)
        {
            foreach (var player in PlayerNetwork.allPlayers)
            {
                player.Value.score.value = 0;
            }
        }
    
    }

}