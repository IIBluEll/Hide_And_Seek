using System;
using System.Collections.Generic;
using HM.CodeBase;
using UnityEngine;

public class MasterAI_Test_Provider : ASingletone<MasterAI_Test_Provider>
{
    //Zone 설정
    private int _zoneCount = 6;
    private List<float> _list_ZoneSuspicion = new();

    // 존 의심도 
    private float _playerStaySuspicionAdd = 10f;
    private float _cctvSuspicionAdd = 40f;
    private float _noiseSuspicionAdd = 20f;

    private float _zoneSuspicionDecrease = 3f;

    private float _maxZoneSuspicion = 100f;

    // 긴장도 설정
    [SerializeField, Range(0f,100f)]
    private float _globalTension = 0f;
    private float _tensionIncrease = 15f;
    private float _tensionDecrease = 8f;

    private float _highTensionThreshold = 85f;
    private float _lowTensionThreshold = 40f;

    private bool _isEnemyChasing = false;
    private bool _isEnemyCloseToPlayer = false;

    // 힌트 주기 / 휴식 
    private float _hintIntervalMax = 10f;
    private float _hintIntervalMin = 4f;
    private float _restDuration = 10f;

    // 힌트 타이머
    private float _hintTimer = 0f;
    private float _currentHintInterval = 3f;

    // 휴식 모드
    private bool _isResting = false;
    private float _restTimer = 0f;

    // 힌트 제공 기준
    private float _suspicionThresholdSend = 50.0f;

    [SerializeField]
    private ChaseAI_Controller _agent;

    public override void Awake()
    {
        base.Awake();

        _list_ZoneSuspicion.Clear();

        for ( int i = 0; i < _zoneCount; i++ )
        {
            _list_ZoneSuspicion.Add(0f);
        }

        _currentHintInterval = ( _hintIntervalMax + _hintIntervalMin ) * 0.5f;
    }

    private void Update()
    {
        float tDeltaTime = Time.deltaTime;

        UpdateTension(tDeltaTime);
        UpdateRestState(tDeltaTime);
        DecreaseSuspicion(tDeltaTime);
        UpdateHintTimer(tDeltaTime);
    }

    #region 긴장도 로직

    private void UpdateTension(float deltaTime)
    {
        bool tIsChasing = _isEnemyChasing;
        bool tIsClose = _isEnemyCloseToPlayer;

        if ( tIsChasing || tIsClose )
        {
            _globalTension += _tensionIncrease * deltaTime;
        }
        else
        {
            _globalTension -= _tensionDecrease * deltaTime;
        }

        _globalTension = Mathf.Clamp(_globalTension , 0f , 100f);

        // 긴장도에 따른 힌트 주기 변경
        float tNormal = Mathf.Clamp01(_globalTension / _highTensionThreshold);

        _currentHintInterval = Mathf.Lerp(_hintIntervalMax , _hintIntervalMin , _globalTension);
    }

    private void UpdateRestState(float deltaTime)
    {
        if ( _isResting )
        {
            _restTimer -= deltaTime;

            if ( _restTimer <= 0f && _globalTension <= _lowTensionThreshold )
            {
                _isResting = false;

                SetEnemyResetMode(false);
            }

            return;
        }

        if ( _globalTension >= _highTensionThreshold )
        {
            _isResting = true;
            _restTimer = _restDuration;

            SetEnemyResetMode(true);
        }
    }

    private void SetEnemyResetMode(bool isRest)
    {
        //TODO : 추격 AI 활성화 / 비활성화
        _agent.SetRestMode(isRest);
    }
    #endregion

    #region 구역 의심도 증감 / 힌트 제공

    private void AddSuspicion(int zoneId , float amount)
    {
        if ( !IsValidZoneId(zoneId) || amount <= 0f )
        {
            return;
        }

        float tValue = _list_ZoneSuspicion[zoneId];
        tValue += amount;
        tValue = Mathf.Clamp(tValue , 0f , _maxZoneSuspicion);

        _list_ZoneSuspicion[ zoneId ] = tValue;
    }

    private void DecreaseSuspicion(float deltaTime)
    {
        for ( int i = 0; i < _list_ZoneSuspicion.Count; i++ )
        {
            float tValue = _list_ZoneSuspicion[i];
            tValue -= _zoneSuspicionDecrease * deltaTime;
            tValue = Mathf.Clamp(tValue , 0f , _maxZoneSuspicion);

            _list_ZoneSuspicion[ i ] = tValue;
        }
    }

    private void UpdateHintTimer(float deltaTimer)
    {
        if ( _isResting )
        {
            return;
        }

        _hintTimer += deltaTimer;

        if ( _hintTimer < _currentHintInterval )
        {
            return;
        }

        _hintTimer = 0f;
        SendZoneHintToEnemy();
    }

    private void SendZoneHintToEnemy()
    {
        int tBestZoneId = GetMostSuspiciousZoneId(out float tSuspicion);

        if(tBestZoneId < 0 || tSuspicion < _suspicionThresholdSend)
        {
            return;
        }

        //TODO : 추격AI에게 힌트 전달
        _agent.SetInvestigationTarget(tBestZoneId);
        Debug.Log($"힌트 전달 : {tBestZoneId}번 구역");
    }

    private int GetMostSuspiciousZoneId( out float SuspicionValue)
    {
        int tBestZoneId = -1;
        float tBestValue = 0f;

        for(int i = 0; i<_list_ZoneSuspicion.Count; i++ )
        {
            float tValue = _list_ZoneSuspicion[i];

            if(tValue > tBestValue)
            {
                tBestValue = tValue;
                tBestZoneId = i;
            }
        }

        SuspicionValue = tBestValue;
        return tBestZoneId;
    }

    #endregion

    #region 외부 API

    public void ReportPlayerStay(int zoneId)
    {
        AddSuspicion(zoneId , _playerStaySuspicionAdd);
    }
    
    public void ReportCCTVDetected(int zoneId)
    {
        AddSuspicion(zoneId , _cctvSuspicionAdd);
    }
    
    public void ReportNoise(int zoneId , float power)
    {
        float tClampedPower = Mathf.Max(0f, power);
        AddSuspicion(zoneId , _noiseSuspicionAdd * tClampedPower);
    }

    public void ReportEnemyCloseToPlayerChanged(bool isClose)
    {
        _isEnemyCloseToPlayer = isClose;
    }

    public void ReportEnemyChaseStateChanged(bool isChasing)
    {
        _isEnemyChasing = isChasing;
    }


    #endregion

    private bool IsValidZoneId(int zoneId)
    {
        return zoneId >= 0 && zoneId < _list_ZoneSuspicion.Count;
    }


}
