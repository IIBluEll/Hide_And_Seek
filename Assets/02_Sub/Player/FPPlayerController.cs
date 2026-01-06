using UnityEngine;
using UnityEngine.InputSystem;

public class FPPlayerController : MonoBehaviour
{
    private Camera _playerCamera;

    [Header("InputAction")]
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _lookAction;
    [SerializeField] private InputActionReference _sprintAction;
    [SerializeField] private InputActionReference _crouchAction;

    [Space(10f), Header("Move")]
    [SerializeField] private float _walkSpeed = 4.0f;
    [SerializeField] private float _runSpeed = 7.0f;
    [SerializeField] private float _crouchSpeed = 2.5f;
    [SerializeField] private float _gravity = -9.81f;

    [Space(10f), Header("Look")]
    [SerializeField] private float _mouseSensitivity = 2.0f;
    [SerializeField] private float _pitchMin = -80.0f;
    [SerializeField] private float _pitchMax = 80.0f;

    [Header("Crouch")]
    [SerializeField] private bool _isCrouchToggle = false;
    [SerializeField] private float _crouchHeight = 1.1f;
    [SerializeField] private LayerMask _standUpObstacleMask = ~0;

    private CharacterController _characterController;
    private float _verticalVelocity;
    private float _cameraPitch;

    private bool _isCrouched;
    private float _standHeight;
    private Vector3 _standCenter;
    private Vector3 _standCameraLocalPos;
    private Vector3 _crouchCameraLocalPos;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerCamera = GetComponentInChildren<Camera>();

        _standHeight = _characterController.height;
        _standCenter = _characterController.center;

        if ( _playerCamera != null )
        {
            _standCameraLocalPos = _playerCamera.transform.localPosition;
        }

        _crouchHeight = Mathf.Clamp(_crouchHeight , 0.5f , _standHeight);

        float tRatio = _crouchHeight / _standHeight;
        _crouchCameraLocalPos = new Vector3
        (
            _standCameraLocalPos.x ,
            _standCameraLocalPos.y * tRatio ,
            _standCameraLocalPos.z
        );
    }

    private void OnEnable()
    {
        _moveAction?.action.Enable();
        _lookAction?.action.Enable();
        _sprintAction?.action.Enable();
        _crouchAction?.action.Enable();
    }

    private void OnDisable()
    {
        _moveAction?.action.Disable();
        _lookAction?.action.Disable();
        _sprintAction?.action.Disable();
        _crouchAction?.action.Disable();
    }

    private void Update()
    {
        HandleLook();
        HandleMove();
        HandleCrouch();

        //Debug
        if(Input.GetKeyUp(KeyCode.F2))
        {
            float tLoud = 1f;

            MasterAI_Provider.Instance.ReportNoise(transform.position , tLoud);
        }
    }

    private void HandleLook()
    {
        if ( _playerCamera == null || _lookAction == null )
        {
            return;
        }

        Vector2 tLook = _lookAction.action.ReadValue<Vector2>();

        float tMouseX = tLook.x * _mouseSensitivity;
        float tMouseY = tLook.y * _mouseSensitivity;

        transform.Rotate(0f , tMouseX , 0f);

        _cameraPitch -= tMouseY;
        _cameraPitch = Mathf.Clamp(_cameraPitch , _pitchMin , _pitchMax);

        _playerCamera.transform.localRotation = Quaternion.Euler(_cameraPitch , 0f , 0f);
    }

    private void HandleMove()
    {
        if ( _moveAction == null )
        {
            return;
        }

        Vector2 tMoveInput = _moveAction.action.ReadValue<Vector2>();
        float tX = tMoveInput.x;
        float tZ = tMoveInput.y;

        Vector3 tMove = (transform.right * tX) + (transform.forward * tZ);
        if ( tMove.sqrMagnitude > 1f )
        {
            tMove.Normalize();
        }

        bool tIsSprinting = (_isCrouched == false) && _sprintAction != null && _sprintAction.action.IsPressed();

        float tSpeed;
        if ( _isCrouched )
        {
            tSpeed = _crouchSpeed;
        }
        else
        {
            tSpeed = ( tIsSprinting && tMove.sqrMagnitude > 0.0001f ) ? _runSpeed : _walkSpeed;
        }

        if ( _characterController.isGrounded && _verticalVelocity < 0f )
        {
            _verticalVelocity = -2f;
        }

        _verticalVelocity += _gravity * Time.deltaTime;

        Vector3 tVelocity = (tMove * tSpeed) + (Vector3.up * _verticalVelocity);
        _characterController.Move(tVelocity * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        if ( _crouchAction == null )
        {
            return;
        }

        if ( _isCrouchToggle )
        {
            if ( _crouchAction.action.WasPressedThisFrame() )
            {
                if ( _isCrouched )
                {
                    TryStandUp();
                }
                else
                {
                    SetCrouch(true);
                }
            }
        }
        else
        {
            bool tWantsCrouch = _crouchAction.action.IsPressed();

            if ( tWantsCrouch && _isCrouched == false )
            {
                SetCrouch(true);
            }
            else if ( tWantsCrouch == false && _isCrouched )
            {
                TryStandUp();
            }
        }
    }

    private void SetCrouch(bool isCrouch)
    {
        _isCrouched = isCrouch;

        if ( isCrouch == false )
        {
            return;
        }

        _characterController.height = _crouchHeight;

        float tStandBottomY = _standCenter.y - (_standHeight * 0.5f);
        float tNewCenterY = tStandBottomY + (_crouchHeight * 0.5f);
        _characterController.center = new Vector3(_standCenter.x , tNewCenterY , _standCenter.z);

        if ( _playerCamera != null )
        {
            _playerCamera.transform.localPosition = _crouchCameraLocalPos;
        }
    }

    private void TryStandUp()
    {
        if ( CanStandUp() == false )
        {
            return;
        }

        _isCrouched = false;

        _characterController.height = _standHeight;
        _characterController.center = _standCenter;

        if ( _playerCamera != null )
        {
            _playerCamera.transform.localPosition = _standCameraLocalPos;
        }
    }

    private bool CanStandUp()
    {
        float tRadius = _characterController.radius;

        float tHalf = _standHeight * 0.5f;
        Vector3 tWorldCenter = transform.TransformPoint(_standCenter);

        Vector3 tBottom = tWorldCenter - (transform.up * (tHalf - tRadius));
        Vector3 tTop = tWorldCenter + (transform.up * (tHalf - tRadius));

        bool tBlocked = Physics.CheckCapsule
        (
            tBottom,
            tTop,
            tRadius,
            _standUpObstacleMask,
            QueryTriggerInteraction.Ignore
        );

        return tBlocked == false;
    }
}
