using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogData", menuName = "Dialog/Boss Dialog Data")]
public class DialogData : ScriptableObject
{
    [Header("연출에니메이션")]
    [SerializeField] GameObject animeEffect;
    [SerializeField] GameObject bossName;
    [SerializeField] AudioClip bossBgm;

    public GameObject AnimeEffect => animeEffect;
    public GameObject BossName => bossName;
    public AudioClip BossBgm => bossBgm;

    [Header("대화 내용")]
    [TextArea(3, 10)]
    public string[] dialogueLines;
}
