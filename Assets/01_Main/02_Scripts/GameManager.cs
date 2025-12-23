using HM.CodeBase;
using UnityEngine;

public class GameManager : ASingletone<GameManager>
{
    [Header("ÂüÁ¶")]
    [SerializeField] private GameObject _aiRootObj;
    [SerializeField] private PlayerZoneStayReporter _playerZoneStayReporter;
    [SerializeField] private MasterAI_Provider _masterAIController;
    //[SerializeField] private ChaseAIController _chaseAIController;

    public override void Awake()
    {
        base.Awake();

        _playerZoneStayReporter.Initialize(_masterAIController);
    }
}
