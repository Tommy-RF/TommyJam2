using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "QuestionScriptableObject", menuName = "Scriptable Objects/QuestionScriptableObject")]
public class QuestionScriptableObject : ScriptableObject
{
    public int id;
    public string questionText;
    public int answer;
    public Vector2 answerRange;
}
